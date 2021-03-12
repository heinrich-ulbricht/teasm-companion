using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Akavache;
using Serilog;
using Microsoft.Extensions.Caching.Memory;
using TeasmCompanion.Interfaces;
using TeasmCompanion.Stores.Imap;
using TeasmCompanion.Stores;
using System.Reactive.Subjects;
using TeasmCompanion.Misc;
using System.Collections.Concurrent;
using System.Threading;

#nullable enable

namespace TeasmCompanion
{
    /// <summary>
    /// IMAP-based storage backend implementing user and chat storage. Teams users and chats will be stored as "e-mails".
    /// </summary>
    public class ImapStore : ITeamsUserStore, ITeamsChatStore
    {
        private readonly ImapConnectionFactory imapConnectionFactory;
        private readonly ILogger logger;
        private readonly IMemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());
        private readonly ImapBackedRemoteLock remoteLock;
        private readonly Subject<(TeamsDataContext, TeamsChatIndexEntry)> chatIndexEntriesToSave = new Subject<(TeamsDataContext, TeamsChatIndexEntry)>();
        /// <summary>
        /// Number of seconds to collect chat indexes before actually storing them remotely.
        /// </summary>
        private const int ChatIndexWriteBackSecs = 60;
        /// <summary>
        /// Number of seconds to collect users before actually storing them remotely.
        /// </summary>
        private const int UserIndexWriteBackSecs = 10;
        private const int FolderPathCacheMins = 1;
        private const string ChatMetadataMessagePrefix = "Chat metadata for ";

        public event Func<object, TeamsDataContext, Task>? OnChatIndexChanged;

        public ImapStore(ILogger logger, ImapConnectionFactory imapConnectionFactory, ImapBackedRemoteLock remoteLock)
        {
            this.imapConnectionFactory = imapConnectionFactory;
            this.remoteLock = remoteLock;
            remoteLock.LockTimeOut = TimeSpan.FromSeconds(30);
            this.logger = logger.ForContext<ImapStore>();

            chatIndexEntriesToSave
                .GroupBy(value => value.Item1)
                .Select(group =>
                    group
                        .Buffer(TimeSpan.FromSeconds(ChatIndexWriteBackSecs))
                        .Where(value => value.Count > 0)
                ).ForEachAsync(bucketsOfGroupedData =>
                    bucketsOfGroupedData
                    .Subscribe(async listOfChatIndexEntries =>
                    {
                        var ctx = listOfChatIndexEntries.First().Item1;
                        // all items in the list should be of the same context due to above grouping
                        Debug.Assert(listOfChatIndexEntries.All(v => v.Item1.Tenant.TenantId == ctx.Tenant.TenantId));
                        logger.Debug("[{TenantName}] Saving {Count} chat index entries to remote", ctx.Tenant.TenantName, listOfChatIndexEntries.Count);
                        await UpdateChatIndex(ctx, listOfChatIndexEntries.Select(pair => pair.Item2).ToList());
                    }));
        }

        public void SubscribeOnUserCreatedOrModified(IObservable<(TeamsDataContext, ProcessedTeamsUser)>? onUserCreatedOrModified)
        {
            onUserCreatedOrModified?
                .Buffer(TimeSpan.FromSeconds(UserIndexWriteBackSecs))
                .Where(o => o.Count > 0)
                .Subscribe(async o =>
                {
                    logger.Debug("Handling {0} new or modified users", o.Count());
                    var groupedByTeamsDataContext = o.GroupBy(o2 => o2.Item1);
                    List<Task> listOfTasks = new List<Task>();
                    foreach (var group in groupedByTeamsDataContext)
                    {
                        listOfTasks.Add(PersistUsersAsync(group.Key, group.Select(tuple => tuple.Item2)));
                    }

                    try
                    {
                        await Task.WhenAll(listOfTasks);
                    } catch (Exception e)
                    {
                        logger.Error(e, "Error while persisting users; ignoring but some users might not have been persisted correctly");
                    }
                });
        }

        public async Task VisitMissingUserDisplayNames(TeamsDataContext ctx, Func<IMutableChatMessage, Task<(bool, List<string>)>> visitor, CancellationToken cancellationToken = default)
        {
            try
            {
                using var imapClient = await imapConnectionFactory.GetImapConnectionAsync(cancellationToken);
                try
                {
                    var (_, chatsFolder) = await GetOrCreateTenantAndTenantChatsFolderAsync(imapClient, ctx);
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    await ImapUtils.VisitChildren(logger, chatsFolder, async (childFolder, currentCount, maxCount) =>
                    {
                        await childFolder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);
                        cancellationToken.ThrowIfCancellationRequested();
                        var messagesWithUnresolvedPlaceholder = await childFolder.SearchAsync(SearchQuery.Or(SearchQuery.BodyContains("User {"), SearchQuery.SubjectContains("User {")), cancellationToken);
                        foreach (var id in messagesWithUnresolvedPlaceholder)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            var message = await childFolder.GetMessageAsync(id, cancellationToken);
                            var chatMessage = new MimeMessageWrapper(logger, message);
                            cancellationToken.ThrowIfCancellationRequested();
                            var (replacedSomething, notes) = await visitor(chatMessage);
                            if (replacedSomething)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                await childFolder.OpenAsync(FolderAccess.ReadWrite, cancellationToken);
                                logger.Debug("[{TenantName}] Updating message with changes: {Notes} ", ctx.Tenant.TenantName, notes);
                                cancellationToken.ThrowIfCancellationRequested();
                                await childFolder.ReplaceAsync(id, message, MessageFlags.Seen, cancellationToken);
                            }
                        }
                        if (currentCount % 10 == 0 || currentCount == maxCount)
                        {
                            logger.Debug("[{TenantName}] Visited {CurrentCount} of {MaxCount} chat folders", ctx.Tenant.TenantName, currentCount, maxCount);
                        }

                        return !cancellationToken.IsCancellationRequested;
                    });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                }
                finally
                {
                    await imapClient.DisconnectAsync(true, cancellationToken);
                }
            }
            catch (Exception e) when (e is OperationCanceledException || e is TaskCanceledException)
            {
                logger.Debug("[{TenantName}] Checking for unresolved user IDs has been cancelled", ctx.Tenant.TenantName);
            }
        }

        private async Task<IMailFolder> GetOrCreateMainUserFolderAsync(ImapClient imapClient, TeamsDataContext ctx)
        {
            var rootFolder = await GetRootFolderAsync(imapClient);
            var safeUserFolderName = rootFolder.MakeSafeFolderName(ctx.MainUserId);
            IMailFolder userFolder;
            try
            {
                userFolder = await rootFolder.GetSubfolderAsync(safeUserFolderName);
            }
            catch
            {
                userFolder = await rootFolder.CreateAsync(safeUserFolderName, false);
                await userFolder.SubscribeAsync();
            }
            return userFolder;
        }

        // needs already open connection
        private async Task<IMailFolder> GetOrCreateTenantFolderAsync(ImapClient imapClient, TeamsDataContext ctx)
        {
            logger.Verbose("[{TenantName}] Running GetOrCreateTenantFolderAsync", ctx.Tenant.TenantName);

            var folderPathCacheDuration = TimeSpan.FromMinutes(FolderPathCacheMins);
            var cachedFolderPath = await BlobCache.UserAccount.GetObject<string>($"ts.imap.folderpath.tenant.{ctx.Tenant.TenantId}").Catch(Observable.Return(""));
            if (!string.IsNullOrWhiteSpace(cachedFolderPath))
            {
                try
                {
                    var tenantFolder = await imapClient.GetFolderAsync(cachedFolderPath);
                    logger.Debug("[{TenantName}] Found existing tenant folder", ctx.Tenant.TenantName);
                    return tenantFolder;
                }
                catch
                {
                    logger.Debug("[{TenantName}] Couldn't get tenant folder for cached path {0}, trying the longer route.", ctx.Tenant.TenantName, cachedFolderPath);
                    /* Exception? Ok take the longer route... */
                }
            }

            var userFolder = await GetOrCreateMainUserFolderAsync(imapClient, ctx);
            var safeTenantFolderName = userFolder.MakeSafeFolderName(ctx.Tenant.TenantName);
            try
            {
                var tenantFolder = await userFolder.GetSubfolderAsync(safeTenantFolderName);
                await BlobCache.UserAccount.InsertObject($"ts.imap.folderpath.tenant.{ctx.Tenant.TenantId}", tenantFolder.FullName, folderPathCacheDuration);
                logger.Debug("[{TenantName}] Found existing tenant folder '{TenantFolder}'", ctx.Tenant.TenantName, safeTenantFolderName);
                return tenantFolder;
            }
            catch { };
            logger.Information("[{TenantName}] Creating new tenant folder '{TenantFolder}'", ctx.Tenant.TenantName, safeTenantFolderName);
            var result = await userFolder.CreateAsync(safeTenantFolderName, true);
            await result.SubscribeAsync();
            await BlobCache.UserAccount.InsertObject($"ts.imap.folderpath.tenant.{ctx.Tenant.TenantId}", result.FullName, folderPathCacheDuration);
            await result.CreateAsync(Constants.TenantChatsFolderName, true);
            await result.CreateAsync(Constants.TenantTeamsFolderName, true);
            return result;
        }

        public async Task<IMailFolder> GetRootFolderAsync(ImapClient imapClient)
        {
            return await imapClient.GetFolderAsync(imapClient.PersonalNamespaces[0].Path);
        }

        public async Task EnsureFoldersForTenantsExistAsync(IEnumerable<TeamsDataContext> contexts)
        {
            using var imapClient = await imapConnectionFactory.GetImapConnectionAsync();
            try
            {
                var rootFolder = await GetRootFolderAsync(imapClient);
                foreach (var ctx in contexts)
                {
                    await GetOrCreateTenantFolderAsync(imapClient, ctx);
                }
            }
            finally
            {
                await imapClient.DisconnectAsync(true);
            }
        }

        private async Task<ConcurrentDictionary<string, IChatChangeInfo>> GetChatIndexAsync(TeamsDataContext ctx, IMailFolder tenantChatsFolder)
        {
            logger.Debug("[{TenantName}] Retrieving chat index 2", ctx.Tenant.TenantName);
            var db = new ImapBackedDatabase(logger, imapConnectionFactory, remoteLock);

            var kvStore = await db.GetStoreForReading(tenantChatsFolder, GetUniqueChatIndexName(ctx.Tenant.TenantId));
            var storedChatIndexEntries = kvStore.GetOrCreateEmpty<List<TeamsChatIndexEntry>>("chatindex").AsObject;
            logger.Debug("[{TenantName}] Retrieved {Count} chat index entries", ctx.Tenant.TenantName, storedChatIndexEntries.Count);
            // use dictionary as intermediary storage for easy duplicate prevention, but store as list to save some bytes by not storing the chat id twice
            return storedChatIndexEntries.Aggregate(new ConcurrentDictionary<string, IChatChangeInfo>(), (dict, entry) => { dict[entry.ChatId] = entry; return dict; });
        }

        public async Task<ConcurrentDictionary<string, IChatChangeInfo>> GetChatIndexAsync(TeamsDataContext ctx)
        {
            logger.Debug("[{TenantName}] Retrieving chat index 1", ctx.Tenant.TenantName);
            using var imapClient = await imapConnectionFactory.GetImapConnectionAsync();
            try
            {
                var (_, tenantChatsFolder) = await GetOrCreateTenantAndTenantChatsFolderAsync(imapClient, ctx);
                return await GetChatIndexAsync(ctx, tenantChatsFolder);
            }
            finally
            {
                await imapClient.DisconnectAsync(true);
            }
        }

        private async Task UpdateChatIndex(TeamsDataContext ctx, List<TeamsChatIndexEntry> newChatIndexes)
        {
            logger.Debug("[{TenantName}] Updating chat index for tenant", ctx.Tenant.TenantName);
            var db = new ImapBackedDatabase(logger, imapConnectionFactory, remoteLock);
            var imapClient = await imapConnectionFactory.GetImapConnectionAsync();
            int chatEntriesTouchedCount = 0;
            try
            {
                var (_, tenantChatsFolder) = await GetOrCreateTenantAndTenantChatsFolderAsync(imapClient, ctx);

                var (kvStore, lockResult) = await db.LockStoreForWriting(tenantChatsFolder, GetUniqueChatIndexName(ctx.Tenant.TenantId));
                try
                {
                    var storedChatIndexEntries = kvStore.GetOrCreateEmpty<List<TeamsChatIndexEntry>>("chatindex").AsObject;
                    // use dictionary as intermediary storage for easy duplicate prevention, but store as list to save some bytes by not storing the chat id twice
                    Dictionary<string, TeamsChatIndexEntry> storedChatIndexEntriesLookup = storedChatIndexEntries.Aggregate(new Dictionary<string, TeamsChatIndexEntry>(), (dict, entry) => { dict[entry.ChatId] = entry; return dict; });

                    logger.Debug("[{TenantName}] Retrieved {Count} chat index entries from remote; adding or updating {NewCount}", ctx.Tenant.TenantName, storedChatIndexEntriesLookup.Values.Count, newChatIndexes.Count);
                    foreach (var newChatIndex in newChatIndexes)
                    {
                        logger.Debug("[{TenantName}] Storing new chat index for {ChatId}: {@NewChatIndex}", ctx.Tenant.TenantName, newChatIndex.ChatId.Truncate(Constants.ChatIdLogLength, true), newChatIndex);
                        storedChatIndexEntriesLookup[newChatIndex.ChatId] = newChatIndex;
                        chatEntriesTouchedCount++;
                    }
                    kvStore.Set("chatindex", storedChatIndexEntriesLookup.Values.ToList());
                }
                finally
                {
                    logger.Debug("[{TenantName}] Storing chat index (added or updated {Count} entries)", ctx.Tenant.TenantName, chatEntriesTouchedCount);
                    await db.WriteAndUnlockStore(tenantChatsFolder, kvStore, lockResult);
                }
            }
            finally
            {
                await imapClient.DisconnectAsync(true);
                if (chatEntriesTouchedCount > 0)
                {
                    var handler = OnChatIndexChanged;
                    if (handler != null)
                    {
                        try
                        {
                            await handler(this, ctx);
                        } catch (Exception e)
                        {
                            logger.Error(e, "[{TenantName}] Error while calling event handler(s) after updating chat index", ctx.Tenant.TenantName);
                        }
                    }
                }
            }
        }

        private async Task SaveChatMetadataForFolderAsync(IMailFolder folder, ProcessedChat chat)
        {
            var db = new ImapBackedDatabase(logger, imapConnectionFactory, remoteLock);
            var (kvStore, lockResult) = await db.LockStoreForWriting(folder, GetUniqueChatStoreName(chat.Id), message =>
            {
                message.Headers.Add("heu-chat", "true");
                message.Headers.Add("heu-chat-version", chat.Version.ToString());
                message.Headers.Add("heu-chat-threadVersion", chat.ThreadVersion.ToString());
            });
            try
            {
                _ = kvStore.Set(ImapMultipartIds.ChatMetadata, chat);
            }
            finally
            {
                await db.WriteAndUnlockStore(folder, kvStore, lockResult);
            }
        }

        public async Task<(IMailFolder, IMailFolder)> GetOrCreateTenantAndTenantChatsFolderAsync(ImapClient imapClient, TeamsDataContext ctx)
        {
            var tenantFolder = await GetOrCreateTenantFolderAsync(imapClient, ctx);
            IMailFolder tenantChatsFolder;
            try
            {
                tenantChatsFolder = await tenantFolder.GetSubfolderAsync(Constants.TenantChatsFolderName);
            }
            catch
            {
                tenantChatsFolder = await tenantFolder.CreateAsync(Constants.TenantChatsFolderName, true);
            }

            return (tenantFolder, tenantChatsFolder);            

        }

        private async Task<List<(string, string, uint, UniqueId)>> BuildChatIndexRecoveryCache(TeamsDataContext ctx, IMailFolder chatsFolder)
        {
            logger.Debug("[{TenantName}] Building recovery chat index since there was a chat folder search. This aids further chat folder searches, so hang on a moment, it's probably worth it.", ctx.Tenant.TenantName);
            var foundChatCount = 0;
            var subfolders = await chatsFolder.GetSubfoldersAsync(false);
            List<(string, string, uint, UniqueId)> cache = new List<(string, string, uint, UniqueId)>();
            foreach (var subFolder in subfolders)
            {
                subFolder.Open(FolderAccess.ReadOnly);
                var hit = (await subFolder.SearchAsync(SearchQuery.SubjectContains(ChatMetadataMessagePrefix))).FirstOrDefault(); // sometimes there are more than one if the companion got terminated; ignore this, think later about handling this
                if (hit.IsValid)
                {
                    var msgSummary = (await subFolder.FetchAsync(new List<UniqueId>() { hit }, MessageSummaryItems.Envelope | MessageSummaryItems.UniqueId)).FirstOrDefault();
                    var chatId = msgSummary?.NormalizedSubject.Replace(ChatMetadataMessagePrefix, "").Trim();
                    if (chatId != null)
                    {
                        cache.Add((chatId, subFolder.Name, subFolder.UidValidity, hit));
                        foundChatCount++;
                    }
                }
            }
            logger.Debug("[{TenantName}] DONE: Building recovery chat index, found {Count} chats", ctx.Tenant.TenantName, foundChatCount);
            return cache;
        }

        private async Task<(UniqueId, IMailFolder?)> FindChatFolderAsync(ImapClient imapClient, TeamsDataContext ctx, string? chatId)
        {
            if (chatId == null)
            {
                return (UniqueId.Invalid, null);
            }

            var chatMetadataMessageIdHeaderValue = GetUniqueChatStoreMessageId(chatId);
            var (_, tenantChatsFolder) = await GetOrCreateTenantAndTenantChatsFolderAsync(imapClient, ctx);

            // ####### START: optimizing for chat index recovery; after deleting the chat index message it takes ages to rebuild - aid by caching 
            // ####### info about all chat indexes and locations; note: this is only a temp cache that won't be updated since it should only
            // ####### be needed once - to rebuild the chat index
            var recoveryCache = await memoryCache.GetOrCreateAsync("FindChatFolderAsync.ImapClient.RecoveryChatIndex", async entry => {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);
                return await BuildChatIndexRecoveryCache(ctx, tenantChatsFolder);
            });
            var (cachedChatId, cachedFolderName, cachedUiValidity, cachedUniqueId) = recoveryCache.FindAll(value => value.Item1 == chatId).FirstOrDefault();
            if (cachedChatId != null && cachedFolderName != null && cachedUniqueId.IsValid)
            {
                logger.Debug("[{TenantName}] Found cached chat folder for chat {ChatId}: '{FolderName}'", ctx.Tenant.TenantName, chatId.Truncate(Constants.ChatIdLogLength, true), cachedFolderName);
                try
                {
                    var chatFolder = await tenantChatsFolder.GetSubfolderAsync(cachedFolderName);
                    chatFolder.Open(FolderAccess.ReadOnly);
                    if (chatFolder.UidValidity == cachedUiValidity)
                    {
                        var message = (await chatFolder.FetchAsync(new List<UniqueId> () { cachedUniqueId }, MessageSummaryItems.UniqueId)).FirstOrDefault();
                        if (message?.UniqueId != null && message.UniqueId.IsValid && message?.UniqueId == cachedUniqueId)
                        {
                            logger.Debug("[{TenantName}] Found chat index with the help of the recovery cache", ctx.Tenant.TenantName);
                            return (message.UniqueId, chatFolder);
                        }
                    }
                } catch (Exception e) {
                    logger.Debug(e, "[{TenantName}] Exception while getting folder; ignoring and continuing with search", ctx.Tenant.TenantName);
                    // do nothing and continue with recursive folder search
                }
            }
            // ####### END

            return await tenantChatsFolder.FindIdByMessageIdHeader(
                chatMetadataMessageIdHeaderValue, 
                recursive: true, 
                skipParent: true, 
                onlyImmediateChildren: true);
        }

        private string GetUniqueChatStoreName(string chatId)
        {
            return $"{ChatMetadataMessagePrefix}{chatId}";
        }

        private string GetUniqueChatStoreMessageId(string chatId)
        {
            return GetUniqueChatStoreName(chatId).EnsureContentIdFormat();
        }

        private string GetUniqueChatIndexName(string tenantId)
        {
            return $"Chat index for tenant {tenantId}";
        }

        public async Task<ProcessedChat?> GetChatMetadataAsync(TeamsDataContext ctx, bool forceChatIndexUpdate, string? chatId)
        {
            if (chatId == null)
                return default;

            using var imapClient = await imapConnectionFactory.GetImapConnectionAsync();
            try
            {
                var (uniqueId, chatFolder) = await FindChatFolderAsync(imapClient, ctx, chatId);
                if (uniqueId.IsValid && chatFolder != null)
                {
                    await chatFolder.OpenAsync(FolderAccess.ReadOnly);
                    var db = new ImapBackedDatabase(logger, imapConnectionFactory, remoteLock);
                    var kvStore = await db.GetStoreForReading(chatFolder, GetUniqueChatStoreName(chatId));
                    var metadata = kvStore.GetOrDefault<ProcessedChat>(ImapMultipartIds.ChatMetadata);
                    if (forceChatIndexUpdate && metadata.AsObject != null)
                    {
                        // this is mainly used for restoring a missing chat index, e.g. after manually deleting it; it's when chat info is present
                        // in the respective chat folder - and can be retrieved - but the entry is missing in the index
                        UpdateChatIndexFor(ctx, metadata.AsObject, chatFolder);
                    }
                    return metadata.AsObject; // possible optimization: return metadata.AsString as well to prevent another unnecessary conversion to string
                }
                else
                {
                    return default;
                }
            }
            finally
            {
                await imapClient.DisconnectAsync(true);
            }
        }

        private async Task<IMailFolder> GetOrCreateChatFolderAsync(ImapClient imapClient, TeamsDataContext ctx, string chatTitle)
        {
            var (_, tenantChatsFolder) = await GetOrCreateTenantAndTenantChatsFolderAsync(imapClient, ctx);
            var safeChatFolderName = tenantChatsFolder.MakeSafeFolderName(chatTitle);
            IMailFolder? chatFolder = null;
            try
            {
                chatFolder = await tenantChatsFolder.GetSubfolderAsync(safeChatFolderName);
            }
            catch
            {
                // an exception is raised when the folder doesn't exist
            }
            if (chatFolder == null)
            {
                chatFolder = await tenantChatsFolder.CreateAsync(safeChatFolderName, true);
                await chatFolder.SubscribeAsync();
            }
            return chatFolder;
        }

        private void StoreChatMessage(IMailFolder chatFolder, IChatMessage message, string? previousMessageId, object lockObject)
        {
            logger.Debug("Storing message from {0} with ID {1} in chat folder '{2}'", message.From.Select(f => f.DisplayName), message.Id, chatFolder.Name);
            var email = CreateMimeMessageFromChatMessageAsync(message, previousMessageId).Result;
            //previousMessageId = email.MessageId;
            lock (lockObject)
            {
                chatFolder.Open(FolderAccess.ReadWrite);
                var existingMessage = chatFolder.Search(SearchQuery.HeaderContains("Message-ID", email.MessageId));
                if (existingMessage.Count == 0)
                {
                    chatFolder.Append(email);
                }
                else
                {
                    // this is bound to happen if there are multiple listeners or multiple open notification endpoints - if this is a valid scenario we need to lock message creation as well
                    if (existingMessage.Count > 1)
                    {
                        logger.Warning("There are multiple messages with Message-Id header value {MessageId}; might be a concurrency issues we need to handle (if it happens often)", email.MessageId);
                        Debugger.Break();
                    }
                    var id = existingMessage.First();
                    var msg = chatFolder.GetMessage(id);
                    chatFolder.Replace(id, email);
                }
            }
        }

        public async Task<bool> StoreSingleChatMessageAsync(TeamsDataContext ctx, IChatMessage message)
        {
            logger.Debug("[{TenantName}] Trying to store single chat message with ID {Id}", ctx.Tenant.TenantName, message.Id);
            using var imapClient = await imapConnectionFactory.GetImapConnectionAsync();
            try
            {
                var (uniqueId, chatFolder) = await FindChatFolderAsync(imapClient, ctx, message.ChatId); // TODO: this can be optimized (maybe cache path; watch for renaming...)
                if (uniqueId.IsValid && chatFolder != null)
                {
                    logger.Debug("[{TenantName}] Found chat folder, storing chat message with ID {Id}", ctx.Tenant.TenantName, message.Id);
                    StoreChatMessage(chatFolder, message, null, imapClient.SyncRoot);
                }
                else
                {
                    logger.Information("[{TenantName}] Cannot store notification chat message because corresponding message folder does not yet exist. Retrieve it!", ctx.Tenant.TenantName); // TBD
                    return false;
                }
            }
            finally
            {
                await imapClient.DisconnectAsync(true);
            }
            return true;
        }

        private void UpdateChatIndexFor(TeamsDataContext ctx, ProcessedChat chat, IMailFolder chatFolder)
        {
            var teamsChatIndexEntry = new TeamsChatIndexEntry()
            {
                ChatId = chat.Id,
                Version = chat.Version,
                ThreadVersion = chat.ThreadVersion,
                LastMessageVersion = chat.LastMessageVersion,
                FolderName = chatFolder.Name,
                UidValidity = chatFolder.UidValidity,
                CreatedAt = chat.CreatedAt
            };
            chatIndexEntriesToSave.OnNext((ctx, teamsChatIndexEntry));
        }

        public async Task StoreMailThreadAndUpdateMetadataAsync(TeamsDataContext ctx, string title, ProcessedChat chat, IOrderedEnumerable<IChatMessage>? messages)
        {
            using var imapClient = await imapConnectionFactory.GetImapConnectionAsync();
            try
            {
                var chatFolder = await GetOrCreateChatFolderAsync(imapClient, ctx, title);
                var previousMessageId = "";

                if (messages != null)
                {
                    var messageTasks = new List<Action>();
                    foreach (var m in messages)
                    {
                        messageTasks.Add(() =>
                        {
                            StoreChatMessage(chatFolder, m, previousMessageId, imapClient.SyncRoot);
                        });
                    }
                    var options = new ParallelOptions { MaxDegreeOfParallelism = 5 };
                    Parallel.Invoke(options, messageTasks.ToArray());
                }
                await SaveChatMetadataForFolderAsync(chatFolder, chat);
                UpdateChatIndexFor(ctx, chat, chatFolder);
            }
            finally
            {
                await imapClient.DisconnectAsync(true);
            }
        }

        private async Task<MimeMessage> CreateMimeMessageFromChatMessageAsync(IChatMessage m, string? inReplyToMessageId)
        {
            var messageIdHeaderValue = $"{m.ChatId};messageid={m.Id}".EnsureContentIdFormat();
            var message = new MimeMessage(
                new Header(HeaderId.MessageId, messageIdHeaderValue)
                );

            foreach (ProcessedTeamsUser u in m.From)
            {
                message.From.Add(new MailboxAddress(u.DisplayName, u.EmailAddress));
            }
            foreach (ProcessedTeamsUser u in m.To)
            {
                message.To.Add(new MailboxAddress(u.DisplayName, u.EmailAddress));
            }
            message.Subject = m.MessageSubject;
            message.Headers["heu-chatId"] = m.ChatId;

            var store = new EmailBackedKeyValueStore(logger, message);
            store.SetTextContent(m.TextContent);
            store.SetHtmlContent(m.HtmlContent);
            store.SetJson("heu-originalMessage", m.SerializeOriginalMessageAsJson());

            message.Date = m.OriginalArrivalTime;

            if (!string.IsNullOrWhiteSpace(inReplyToMessageId))
            {
                message.InReplyTo = inReplyToMessageId;
            }

            var attachments = new AttachmentCollection();
            foreach (var cid in m.ContentIds.Keys)
            {
                var imageInfo = m.ContentIds[cid];
                byte[] buffer = await BlobCache.UserAccount.Get(imageInfo.CacheKey).Catch(Observable.Return(new byte[0]));
                if (buffer.Length > 0)
                {
                    var image = attachments.Add("image.png", buffer);
                    image.ContentId = cid;
                }
            }
            store.AddAttachments(attachments);
            return message;
        }

        public async Task PersistUserAsync(TeamsDataContext ctx, ProcessedTeamsUser user)
        {
            await PersistUsersAsync(ctx, new ProcessedTeamsUser[] { user });
        }

        //private async Task<ImapClient> GetCachedImapConnectionAsync(TeamsDataContext ctx)
        //{
        //    // cache imap connection for some seconds sliding window to allow for efficient bulk insert operations
        //    return await memoryCache.GetOrCreate($"StoreUser.ImapClient.{ctx.MainUserId}.{Environment.CurrentManagedThreadId}", async entry =>
        //    {
        //        entry.SetSlidingExpiration(TimeSpan.FromSeconds(15));
        //        entry.RegisterPostEvictionCallback((key, value, reason, state) =>
        //        {
        //            try
        //            {
        //                logger.Debug("Disconnecting from IMAP connection");
        //                (value as ImapClient)?.Disconnect(true);
        //            }
        //            catch { logger.Information("Error while disconnecting from cached IMAP connection."); }
        //        });
        //        logger.Debug("Creating new IMAP connection");
        //        return await imapConnectionFactory.GetImapConnectionAsync();
        //    });
        //}

        /**
         * Store the users for the given context.
         */
        public async Task PersistUsersAsync(TeamsDataContext ctx, IEnumerable<ProcessedTeamsUser> users)
        {
            if (!users.Any())
            {
                return;
            }

            logger.Debug("[{TenantName}] PersistUsersAsync: Handling {0} users...", ctx.Tenant.TenantName, users.Count());
            var client = await imapConnectionFactory.GetImapConnectionAsync();
            try
            {
                var tenantFolder = await GetOrCreateTenantFolderAsync(client, ctx);

                var db = new ImapBackedDatabase(logger, imapConnectionFactory, remoteLock);
                var (kvStore, lockResult) = await db.LockStoreForWriting(tenantFolder, $"User Store for tenant {ctx.Tenant.TenantId}");
                try
                {
                    var userList = kvStore.GetOrCreateEmpty<List<ProcessedTeamsUser>>("Users").AsObject;
                    foreach (var user in users)
                    {
                        var existingUser = userList.FirstOrDefault(u => u.UserId == user.UserId);
                        if (existingUser == null)
                        {
                            userList.Add(user);
                        }
                        else
                        {
                            userList.Remove(existingUser);
                            userList.Add(user);
                        }
                    }
                    kvStore.Set("Users", userList);
                    memoryCache.Set("GetUsers.Result", userList, new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24), SlidingExpiration = TimeSpan.FromHours(1) });
                }
                finally
                {
                    await db.WriteAndUnlockStore(tenantFolder, kvStore, lockResult);
                }
            }
            finally
            {
                await client.DisconnectAsync(true);
            }
        }

        public async Task<List<ProcessedTeamsUser>> RetrieveUsersAsync(TeamsDataContext ctx)
        {
            logger.Debug("[{TenantName}] Running RetrieveUsersAsync", ctx.Tenant.TenantName);
            if (memoryCache.TryGetValue("GetUsers.Result", out List<ProcessedTeamsUser> userList))
            {
                logger.Debug("[{TenantName}] Returning {0} cached users", ctx.Tenant.TenantName, userList.Count);
                return userList;
            }

            var client = await imapConnectionFactory.GetImapConnectionAsync();
            try
            {
                logger.Debug("[{TenantName}] Trying to retrieve remote user list...", ctx.Tenant.TenantName);
                var db = new ImapBackedDatabase(logger, imapConnectionFactory, remoteLock);
                var tenantFolder = await GetOrCreateTenantFolderAsync(client, ctx);
                var kvStore = await db.GetStoreForReading(tenantFolder, $"User Store for tenant {ctx.Tenant.TenantId}");
                logger.Debug("[{TenantName}] Done: Trying to retrieve remote user list", ctx.Tenant.TenantName);
                userList = kvStore.GetOrCreateEmpty<List<ProcessedTeamsUser>>("Users").AsObject;
                logger.Debug("[{TenantName}] Got {0} users from remote storage", ctx.Tenant.TenantName, userList.Count);
                memoryCache.Set("GetUsers.Result", userList, new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24), SlidingExpiration = TimeSpan.FromHours(1) });

                logger.Debug("[{TenantName}] Leaving RetrieveUsersAsync", ctx.Tenant.TenantName);
                return userList;
            }
            finally
            {
                await client.DisconnectAsync(true);
            }
        }

        public async Task<bool> CanAccessStore(CancellationToken cancellationToken)
        {
            try
            {
                var imapClient = await imapConnectionFactory.GetImapConnectionAsync(cancellationToken);
                try
                {
                    var result = imapClient.IsConnected && imapClient.IsAuthenticated;
                    logger.Information("Checking store access; IMAP server is connected and authenticated: {Result}", result);
                    return result;
                }
                finally
                {
                    await imapClient.DisconnectAsync(true, cancellationToken);
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Got exception while checking access to IMAP server store");
                return false;
            }
        }
    }
}
