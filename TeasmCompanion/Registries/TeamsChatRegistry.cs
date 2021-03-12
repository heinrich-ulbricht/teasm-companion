using Akavache;
using System.Reactive.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TeasmCompanion.Interfaces;
using System.Diagnostics;
using System.Linq;
using Serilog;
using TeasmCompanion.Misc;
using TeasmCompanion.TeamsInternal.TeamsInternalApiAccessor;
using TeasmCompanion.TeamsInternal.TeamsInternalApi.api.csa.api.v1.teams.users;
using TeasmCompanion.Registries;
using System.Collections.Concurrent;
using System.Threading;

#nullable enable

namespace TeasmCompanion.Stores
{
    /// <summary>
    /// This chat registry implements chat storage and retrieval. 
    /// 
    /// Chat info will be retrieved from the Teams API, from the (slow) chat store or from a local cache to optimize things.
    /// Chat info will be stored in a given (slow) chat store and in the local cache.
    /// </summary>
    public class TeamsChatRegistry : ITeamsChatRegistry
    {
        private readonly ILogger logger;
        private readonly ITeamsChatStore chatStore;
        private readonly TeamsTenantApiAccessor teamsTenantApiAccessor;
        private const int ChatsAndTeamsCacheLifetimeDays = 1;
        private const int SingleChatCacheLifetimeMins = 10;
        private const int ChatIndexCacheLifetimeMins = 10;

        public TeamsChatRegistry(ILogger logger, ITeamsChatStore chatStore, TeamsTenantApiAccessor teamsTenantApiAccessor)
        {
            this.logger = logger.ForContext<TeamsChatRegistry>();
            this.chatStore = chatStore;
            this.teamsTenantApiAccessor = teamsTenantApiAccessor;

            this.chatStore.OnChatIndexChanged += ChatIndexChangedHandler;
        }

        ~TeamsChatRegistry()
        {
            chatStore.OnChatIndexChanged -= ChatIndexChangedHandler;
        }

        private async Task<ConcurrentDictionary<string, IChatChangeInfo>> GetChatIndexAsync(TeamsDataContext ctx)
        {
            return await BlobCache.UserAccount
                .GetOrFetchObject(
                    $"ts.{ctx.Tenant.TenantId}.chatIndex",
                    () => chatStore.GetChatIndexAsync(ctx),
                    DateTimeOffset.Now + TimeSpan.FromMinutes(ChatIndexCacheLifetimeMins)
                   );
        }

        private async Task ChatIndexChangedHandler(object sender, TeamsDataContext ctx)
        {
            await ClearChatIndexCache(ctx);
        }

        private async Task ClearChatIndexCache(TeamsDataContext ctx)
        {
            logger.Debug("[{TenantName}] Clearing chat index cache", ctx.Tenant.TenantName);
            _ = await BlobCache.UserAccount.InvalidateObject<Dictionary<string, TeamsChatIndexEntry>>($"ts.{ctx.Tenant.TenantId}.chatIndex");
        }

        private async Task UpdateChatIndexCacheEntry(TeamsDataContext ctx, ProcessedChat? chat)
        {
            if (chat == null)
            {
                return;
            }
            var cacheKey = $"ts.{ctx.Tenant.TenantId}.chatIndex";
            var chatIndex = await GetChatIndexAsync(ctx);
            chatIndex.AddOrUpdate(chat.Id, chat, (_, existingValue) =>
            {
                if (chat.Version > existingValue.Version)
                {
                    logger.Debug("[{TenantName}] Updating chat index entry for chat {ChatId}", ctx.Tenant.TenantName, chat.Id.Truncate(Constants.ChatIdLogLength, true));
                    return chat;
                }
                else
                {
                    return existingValue;
                }
            });
            await BlobCache.UserAccount.InsertObject(cacheKey, chatIndex, DateTimeOffset.Now + TimeSpan.FromMinutes(ChatIndexCacheLifetimeMins));
        }

        public async Task<MyChatsAndTeams?> GetAllChatsAndTeamsAsync(TeamsDataContext ctx)
        {
            var cacheKey = $"ts.{ctx.Tenant.UserId}.chatsAndTeams";
            var cachedChats = await BlobCache.UserAccount
                .GetObject<MyChatsAndTeams?>(cacheKey)
                .Catch(Observable.Return<MyChatsAndTeams?>(null));
            // only return cached values if there are any
            if (cachedChats != null && cachedChats.chats.Count > 0)
            {
                logger.Debug("[{TenantName}] Got list of chats and teams from cache", ctx.Tenant.TenantName);
                return cachedChats;
            }
            // otherwise: retrieve
            var chatsAndTeams = await teamsTenantApiAccessor.GetMyChatsAndTeamsAsync(ctx);
            if (chatsAndTeams != null)
            {
                await BlobCache.UserAccount.InsertObject(cacheKey, chatsAndTeams, DateTimeOffset.Now + TimeSpan.FromDays(ChatsAndTeamsCacheLifetimeDays));
            }
            return chatsAndTeams;
        }

        public async Task<MyChatsAndTeams?> GetUpdatedChatsAndTeamsAsync(TeamsDataContext ctx)
        {
            logger.Debug("[{TenantName}] {Method}: Starting to update chats", ctx.Tenant.TenantName, nameof(GetUpdatedChatsAndTeamsAsync));
            var toBeUpdatedChatsAndTeams = await GetAllChatsAndTeamsAsync(ctx);
            if (toBeUpdatedChatsAndTeams == null)
            {
                logger.Debug("[{TenantName}] {Method}: Initial list of chats is null; leaving", ctx.Tenant.TenantName, nameof(GetUpdatedChatsAndTeamsAsync));
                // error while retrieving all data? cannot retrieve update either
                return null;
            }

            if (string.IsNullOrWhiteSpace(toBeUpdatedChatsAndTeams.metadata?.syncToken))
            {
                logger.Debug("[{TenantName}] {Method}: Initial list of chats contains no sync token but we need one to update; leaving", ctx.Tenant.TenantName, nameof(GetUpdatedChatsAndTeamsAsync));
                // cannot get update if there is no sync token
                return toBeUpdatedChatsAndTeams;
            }

            var deltaChatsAndTeams = await teamsTenantApiAccessor.GetMyChatsAndTeamsAsync(ctx, toBeUpdatedChatsAndTeams.metadata.syncToken);
            if (deltaChatsAndTeams == null)
            {
                logger.Debug("[{TenantName}] {Method}: Got null as delta result; leaving", ctx.Tenant.TenantName, nameof(GetUpdatedChatsAndTeamsAsync));
                return toBeUpdatedChatsAndTeams;
            }
            logger.Debug("[{TenantName}] {Method}: {ChatCount} chats are new or updated with old sync token {SyncToken}", ctx.Tenant.TenantName, nameof(GetUpdatedChatsAndTeamsAsync), toBeUpdatedChatsAndTeams.chats.Count, toBeUpdatedChatsAndTeams.metadata.syncToken.FromBase64String());

            var oldChatsToReplace = toBeUpdatedChatsAndTeams.chats.Where(oldValue => deltaChatsAndTeams.chats.FirstOrDefault(updatedValue => updatedValue?.id == oldValue?.id) != default);
            logger.Debug("[{TenantName}] {Method}: {ChatCount} old chats need an update", ctx.Tenant.TenantName, nameof(GetUpdatedChatsAndTeamsAsync), oldChatsToReplace.Count());
            foreach (var oldChat in oldChatsToReplace.ToList())
            {
                toBeUpdatedChatsAndTeams.chats.Remove(oldChat);
            }
            toBeUpdatedChatsAndTeams.chats.AddRange(deltaChatsAndTeams.chats);
            teamsTenantApiAccessor.SortChats(toBeUpdatedChatsAndTeams);

            logger.Debug("[{TenantName}] {Method}: Updating sync token to {SyncToken}", ctx.Tenant.TenantName, nameof(GetUpdatedChatsAndTeamsAsync), deltaChatsAndTeams.metadata.syncToken.FromBase64String());
            toBeUpdatedChatsAndTeams.metadata.syncToken = deltaChatsAndTeams.metadata.syncToken;
            await BlobCache.UserAccount.InsertObject($"ts.{ctx.Tenant.UserId}.chatsAndTeams", toBeUpdatedChatsAndTeams, DateTimeOffset.Now + TimeSpan.FromDays(ChatsAndTeamsCacheLifetimeDays));
            logger.Debug("[{TenantName}] {Method}: Done", ctx.Tenant.TenantName, nameof(GetUpdatedChatsAndTeamsAsync));
            return toBeUpdatedChatsAndTeams;
        }

        private string GetChatMetadataCacheKey(string chatId)
        {
            return "ts.imap.chatmetadata." + chatId.EnsureContentIdFormat();
        }

        public async Task<ProcessedChat?> GetStoredProcessedChatAsync(TeamsDataContext ctx, bool forceChatIndexUpdate, string chatId)
        {
            logger.Debug("[{TenantName}] GetChatMetadataAsync for chat {ID} | {Context}", ctx.Tenant.TenantName, chatId.Truncate(Constants.ChatIdLogLength, true), ctx);
            return await BlobCache.UserAccount
                .GetOrFetchObject(
                    GetChatMetadataCacheKey(chatId),
                    () => chatStore.GetChatMetadataAsync(ctx, forceChatIndexUpdate, chatId),
                    DateTimeOffset.Now + TimeSpan.FromMinutes(SingleChatCacheLifetimeMins)
                   );
        }

        public async Task<(ProcessedChat, long)> RetrieveProcessedChatWithAllMessagesAsync(TeamsDataContext ctx, Chat newChat)
        {
            Debug.Assert(newChat != null);
            var messagesForChat = await teamsTenantApiAccessor.RetrieveAllMessagesForChatAsync(ctx, newChat);
            var processedChat = await teamsTenantApiAccessor.ProcessChatMessagesAsync(ctx, newChat, messagesForChat);
            await teamsTenantApiAccessor.DownloadImagesAsync(ctx, processedChat.OrderedMessages);
            return (processedChat, messagesForChat.Count());
        }
        public async Task<(ProcessedChat, long)> RetrieveProcessedChatWithOnlyNewMessagesAsync(TeamsDataContext ctx, Chat oldChat)
        {
            Debug.Assert(oldChat != null);
            var messagesForChat = await teamsTenantApiAccessor.RetrieveMessagesForChatSinceAsync(ctx, oldChat, oldChat.version - (long)TimeSpan.FromMinutes(1).TotalMilliseconds);
            var processedChat = await teamsTenantApiAccessor.ProcessChatMessagesAsync(ctx, oldChat, messagesForChat);
            await teamsTenantApiAccessor.DownloadImagesAsync(ctx, processedChat.OrderedMessages);
            return (processedChat, messagesForChat.Count());
        }

        public async Task StoreMailThreadAsyncAndUpdateMetadataAsync(TeamsDataContext ctx, string title, ProcessedChat chat, IOrderedEnumerable<IChatMessage> messages)
        {
            await chatStore.StoreMailThreadAndUpdateMetadataAsync(ctx, title, chat, messages);
            await BlobCache.UserAccount.InsertObject(GetChatMetadataCacheKey(chat.Id), chat, DateTimeOffset.Now + TimeSpan.FromMinutes(10));
        }

        public async Task<bool> StoreSingleChatMessageAsync(TeamsDataContext ctx, IChatMessage message)
        {
            return await chatStore.StoreSingleChatMessageAsync(ctx, message);
        }

        public async Task<(ChatRetrievalResult, IChatChangeInfo?, long)> RetrieveChatAndMessagesIfNeededAsync(TeamsDataContext ctx, Chat chat)
        {
            logger.Verbose("[{TenantName}] Entering {Method} for chat {ChatId}...", ctx.Tenant.TenantName, nameof(RetrieveChatAndMessagesIfNeededAsync), chat.id.Truncate(Constants.ChatIdLogLength, true));
            
            // try to get change info from index which is mighty fast once cached
            var chatIndex = await GetChatIndexAsync(ctx);
            IChatChangeInfo? result = null;
            if (chatIndex.TryGetValue(chat.id, out var chatIndexEntry))
            {
                logger.Verbose("[{TenantName}] Found chat {ChatId} in chat index", ctx.Tenant.TenantName, chat.id.Truncate(Constants.ChatIdLogLength, true));
                result = chatIndexEntry;
            } else
            {
                logger.Debug("[{TenantName}] Did not find chat {ChatId} in index, need to retrieve", ctx.Tenant.TenantName, chat.id.Truncate(Constants.ChatIdLogLength, true));
                // if there is no chat index entry yet then we request the "real" chat info
                var storedChat = await GetStoredProcessedChatAsync(ctx, true, chat.id);
                result = storedChat;
                // after retrieving the chat our cache will be invalid; retrieve index again the next time
                // await ClearChatIndexCache(ctx); -> note: don't do this anymore as the store will collect several changes and store them later; clearing _now_ makes no sense
                await UpdateChatIndexCacheEntry(ctx, storedChat);
            }
            if (result == null)
            {
                logger.Debug("[{TenantName}] This is a new chat that needs to be retrieved | {Context}", ctx.Tenant.TenantName, ctx);
                var (chatWithMessages, newMessageCount) = await RetrieveProcessedChatWithAllMessagesAsync(ctx, chat);
                logger.Information("[{TenantName}] Retrieved new chat: {@Chat} | {Context}", ctx.Tenant.TenantName, chatWithMessages.GetDebugLogSummary(), ctx);

                await StoreMailThreadAsyncAndUpdateMetadataAsync(ctx, chatWithMessages.ChatTitle, chatWithMessages, chatWithMessages.OrderedMessages);
                //await ClearChatIndexCache(ctx);  -> note: don't do this anymore as the store will collect several changes and store them later; clearing _now_ makes no sense
                await UpdateChatIndexCacheEntry(ctx, chatWithMessages);
                return (ChatRetrievalResult.SuccessfulFullRetrieval, result = chatWithMessages, newMessageCount);
            }
            else if (result.Version != chat.version) // note: threadVersion seems to be way to current and excludes the latest messages; version ist better but also excludes the last message by a few ms...
            {
                logger.Information("[{TenantName}] Chat {ChatTitleOld}/{ChatTitleNew} has been updated, need to retrieve new messages | {Context}", ctx.Tenant.TenantName, result.TitleOrFolderName, chat.title, ctx);
                var (chatWithMessages, newMessageCount) = await RetrieveProcessedChatWithOnlyNewMessagesAsync(ctx, chat);
                await StoreMailThreadAsyncAndUpdateMetadataAsync(ctx, chatWithMessages.ChatTitle, chatWithMessages, chatWithMessages.OrderedMessages);
                await UpdateChatIndexCacheEntry(ctx, chatWithMessages);
                // await ClearChatIndexCache(ctx);  -> note: don't do this anymore as the store will collect several changes and store them later; clearing _now_ makes no sense
                return (ChatRetrievalResult.SuccessfulUpdate, result = chatWithMessages, newMessageCount);
            }
            else
            {
                logger.Debug("[{TenantName}] Ignoring existing up-to-date chat: {ChatTitle} (Chat ID: {ChatId}) | {Context}", ctx.Tenant.TenantName, result.TitleOrFolderName, chat.id.Truncate(Constants.ChatIdLogLength, true), ctx);
                return (ChatRetrievalResult.IsUpToDate, result, 0);
            }
        }

        public async Task VisitMissingUserDisplayNames(TeamsDataContext ctx, Func<IMutableChatMessage, Task<(bool, List<string>)>> visitor, CancellationToken cancellationToken = default)
        {
            await chatStore.VisitMissingUserDisplayNames(ctx, visitor, cancellationToken);
        }
    }
}
