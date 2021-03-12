using Serilog;
using System;
using System.Reactive.Linq;
using System.Linq;
using System.Threading.Tasks;
using TeasmCompanion.Interfaces;
using TeasmCompanion.TeamsInternal.TeamsInternalApi.api.csa.api.v1.teams.users;
using TeasmCompanion.TeamsInternal.TeamsInternalApiAccessor;
using System.Reactive.Subjects;
using System.Threading;
using TeasmCompanion.Registries;
using Priority_Queue;
using TeasmCompanion.Misc;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using MailKit;

#nullable enable

namespace TeasmCompanion.TeamsMonitors
{
    public class TeamsChatRetriever
    {
        private readonly ILogger logger;
        private readonly TeamsUserTenantsRetriever userTenantsRetriever;
        private readonly ITeamsUserRegistry userRegistry;
        private readonly ITeamsChatRegistry chatRegistry;
        private readonly TeamsLongPollingRegistry longPollingRegistry;
        private readonly Configuration config;

        // note: ConcurrentDictionary and SimplePriorityQueue are thread safe
        // note: one queue per data context
        private ConcurrentDictionary<TeamsDataContext, SimplePriorityQueue<ChatQueueItem, HigherVersionWinsComparerChat>> chatsToRetrieve = new ConcurrentDictionary<TeamsDataContext, SimplePriorityQueue<ChatQueueItem, HigherVersionWinsComparerChat>>();

        private static readonly object timerExecutionLock = new object();
        private static readonly object timerCreationLock = new object();
        private CancellationTokenSource tokenSource = new CancellationTokenSource();
        private readonly Subject<TeamsDataContext> internalChatRetrievalRequests = new Subject<TeamsDataContext>();
        private Dictionary<TeamsDataContext, Thread> threads = new Dictionary<TeamsDataContext, Thread>();
        private object serializeActualChatRetrievalLock = new object();
        private CancellationToken cancellationToken = default;

        public TeamsChatRetriever(ILogger logger, TeamsUserTenantsRetriever userTenantsRetriever, ITeamsUserRegistry userRegistry, ITeamsChatRegistry chatRegistry, TeamsLongPollingRegistry longPollingRegistry, Configuration config)
        {
            this.logger = logger.ForContext<TeamsChatRetriever>();
            this.userTenantsRetriever = userTenantsRetriever;
            this.userRegistry = userRegistry;
            this.chatRegistry = chatRegistry;
            this.longPollingRegistry = longPollingRegistry;
            this.config = config;
        }

        private (ChatQueueItem?, int) GetNextChatToProcess(TeamsDataContext ctx)
        {
            if (chatsToRetrieve.TryGetValue(ctx, out var queue))
            {
                queue.TryDequeue(out ChatQueueItem? result);
                return (result, queue.Count);
            }
            return (null, 0);
        }

        private void ThreadFunc(TeamsDataContext ctx)
        {
            try
            {
                var lastIdleLogTime = DateTime.UtcNow;
                var heartbeatMinutes = 5;
                var forceHeartbeatMessage = false;
                try
                {
                    // give it a minute before trying to resolve
                    var lastUserResolveTime = DateTime.UtcNow.AddMinutes(-1 * config.ResolveUnknownUserIdsJobIntervalMin + 1);
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var (chatQueueItem, chatsLeftCount) = GetNextChatToProcess(ctx);
                        if (chatQueueItem != null)
                        {
                            ProcessChatFromQueue(chatQueueItem, chatsLeftCount);
                            // make sure to log immediately when idle after retrieving chat
                            lastIdleLogTime = DateTime.UtcNow.AddMinutes(-1 * heartbeatMinutes);
                        }
                        else
                        {
                            if (DateTime.UtcNow - lastIdleLogTime > TimeSpan.FromMinutes(heartbeatMinutes) || forceHeartbeatMessage)
                            {
                                forceHeartbeatMessage = false;
                                lastIdleLogTime = DateTime.UtcNow;
                                logger.Debug("[{TenantName}] Nothing to do in chat retrieval thread, sleeping. Next heartbeat message in {Min} minutes at the latest.", ctx.Tenant.TenantName, heartbeatMinutes);
                            }
                            else
                            {
                                logger.Verbose("[{TenantName}] Nothing to do in chat retrieval thread, sleeping...", ctx.Tenant.TenantName);
                            }
                            // TODO: optimize this
                            if (cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(15)))
                                return;
                        }
                        // from time to time scan and resolve user ids that, so far, were unknown
                        if (DateTime.UtcNow - lastUserResolveTime > TimeSpan.FromMinutes(config.ResolveUnknownUserIdsJobIntervalMin))
                        {
                            lastUserResolveTime = DateTime.UtcNow;
                            ResolveUnknownUserIds(ctx, cancellationToken).Wait();
                            logger.Debug("[{TenantName}] Note: you can configure the interval in which unknown user IDs are checked via config*.json property '{PropName}'", ctx.Tenant.TenantName, nameof(config.ResolveUnknownUserIdsJobIntervalMin));
                            forceHeartbeatMessage = true;
                        }
                    }
                }
                catch (ProtocolException e) // TBD: this knowledge about an IMAP-based backend shouldn't be here; refactor
                {
                    var waitMins = 30;
                    logger.Error(e, "[{TenantName}] IMAP exception in main chat processing loop which hopefully resolves itself. Waiting {WaitMins} minutes before proceeding with chat handling.", ctx.Tenant.TenantName, waitMins);
                    if (cancellationToken.WaitHandle.WaitOne(TimeSpan.FromMinutes(waitMins)))
                        return;

                }
                catch (Exception e)
                {
                    logger.Error(e, "[{TenantName}] Exception in main chat processing loop, won't process any more chats. Better exit and restart or inspect the log.", ctx.Tenant.TenantName);
                    throw;
                }
            } finally
            {
                logger.Information("[{TenantName}] Leaving chat retrieval thread function; this ends chat retrieval for the tenant", ctx.Tenant.TenantName);
            }
        }

        private void ProcessChatFromQueue(ChatQueueItem chatQueueItem, int chatsLeftCount)
        {
            var (ctx, chat) = (chatQueueItem.ctx, chatQueueItem.chat?.Chat);
            try
            {
                if (chat == null)
                {
                    return;
                }

                logger.Debug("[{TenantName}] Retrieval thread got chat to retrieve: {ChatId}", ctx.Tenant.TenantName, chat.id.Truncate(Constants.ChatIdLogLength, true));
                ChatRetrievalResult result;
                IChatChangeInfo? processedChat;
                long newMessagesCount;

                // serialize actual chat retrieval to no overload the API with parallel requests and to not overload the IMAP server; TBD think about maybe changing this?
                lock (serializeActualChatRetrievalLock)
                {
                    (result, processedChat, newMessagesCount) = chatRegistry.RetrieveChatAndMessagesIfNeededAsync(ctx, chat).Result;
                }

                double slowdownMultiplier = 1;
                if (processedChat != null)
                {
                    slowdownMultiplier = Math.Max(1, (DateTime.UtcNow -  Utils.JavaScriptUtcMsToDateTime(processedChat.GetLastMessageVersionWithLogic().Item2)).TotalDays / 200);
                    logger.Write(result != ChatRetrievalResult.IsUpToDate ? Serilog.Events.LogEventLevel.Debug : Serilog.Events.LogEventLevel.Verbose, "[{TenantName}] Version info for chat {ChatId}: Version={Version}, ThreadVersion={ThreadVersion}, LastMessageVersionWithLogic={LastMessageVersion}, SlowdownMultiplier={SlowdownMultiplier}", ctx.Tenant.TenantName, chat.id.Truncate(Constants.ChatIdLogLength, true), Utils.JavaScriptUtcMsToDateTime(processedChat.Version), Utils.JavaScriptUtcMsToDateTime(processedChat.ThreadVersion), Utils.JavaScriptUtcMsToDateTime(processedChat.GetLastMessageVersionWithLogic().Item2), slowdownMultiplier);
                }

                if (result == ChatRetrievalResult.SuccessfulFullRetrieval)
                {
                    // default wait time depends on the number of retrieved messages with a maximum of config.SlowChatRetrievalWaitTimeMin
                    var waitTime = TimeSpan.FromSeconds(Math.Max(60, Math.Min(newMessagesCount * 10 * slowdownMultiplier, config.SlowChatRetrievalWaitTimeMin * 60)));
                    if (processedChat != null)
                    {
                        var ageOfChat = DateTime.UtcNow - Utils.JavaScriptUtcMsToDateTime(processedChat.GetLastMessageVersionWithLogic().Item2);
                        if (ageOfChat <= TimeSpan.FromDays(Math.Max(1, config.FastChatRetrievalDays)))
                        {
                            logger.Debug("[{TenantName}] Found chat with activity {Age} days ago, retrieving next one fast (there are {Count} left to check)", ctx.Tenant.TenantName, ageOfChat.TotalDays, chatsLeftCount);
                            waitTime = TimeSpan.FromSeconds(Math.Max(30, Math.Min(newMessagesCount * 3, config.FastChatRetrievalWaitTimeMin * 60)));
                        }
                        else
                        {
                            logger.Debug("[{TenantName}] Found older chat with activity {Age} days ago, retrieving next one a bit slower (there are {Count} left to check)", ctx.Tenant.TenantName, ageOfChat.TotalDays, chatsLeftCount);
                        }
                    }
                    else
                    {
                        logger.Debug("[{TenantName}] Got no chat info at all, retrieving slowly to be safe", ctx.Tenant.TenantName);
                        waitTime = TimeSpan.FromMinutes(Math.Max(1, config.SlowChatRetrievalWaitTimeMin));
                    }

                    logger.Debug("[{TenantName}] >>> Waiting {WaitTimeSec} minutes after full chat retrieval before checking the next of {Count} chats in queue <<<", ctx.Tenant.TenantName, waitTime.TotalMinutes, chatsLeftCount);
                    cancellationToken.WaitHandle.WaitOne(waitTime);
                }
                else
                if (result == ChatRetrievalResult.SuccessfulUpdate)
                {
                    var waitTimeSec = config.UpdatedChatRetrievalWaitTimeSec;
                    logger.Debug("[{TenantName}] Waiting {WaitTimeSec} seconds after chat update", ctx.Tenant.TenantName, waitTimeSec);
                    cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(waitTimeSec));
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Exception in chat handling loop.");
                logger.Error("[{TenantName}] Exception in chat handling loop. Sleeping some seconds and continuing. Offending chat: '{@chat}')", ctx.Tenant.TenantName, chat);
                cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(60));
            }
        }

        private async Task ResolveUnknownUserIds(TeamsDataContext ctx, CancellationToken cancellationToken = default)
        {
            logger.Debug("[{TenantName}] Trying to resolve unknown user ids for tenant", ctx.Tenant.TenantName);
            await chatRegistry.VisitMissingUserDisplayNames(ctx, message => ResolveParticipantPlaceholders(logger, userRegistry, ctx, message), cancellationToken);
        }

        public static async Task<(bool, List<string>)> ResolveParticipantPlaceholders(ILogger logger, ITeamsUserRegistry userRegistry, TeamsDataContext ctx, IMutableChatMessage message)
        {
            var replacedSomething = false;
            var notes = new List<string>();

            var pattern = TeamsParticipant.PlaceholderPattern;
            var replacers = new List<(Func<IMutableChatMessage, string?>, Action<IMutableChatMessage, string>, string)>
                    {
                        (m => m.MessageSubject, (m, s) => m.MessageSubject = s, "subject"),
                        (m => m.TextContent, (m, s) => m.TextContent = s, "text content"),
                        (m => m.HtmlContent, (m, s) => m.HtmlContent = s, "HTML content")
                    };

            foreach (var stringReplacer in replacers)
            {
                var (stringGetter, stringSetter, replacementTarget) = stringReplacer;
                var stringToHandle = stringGetter(message);
                if (stringToHandle == null)
                    continue;

                var matches = Regex.Matches(stringToHandle, pattern);
                if (matches.Count == 0)
                    continue;

                var resolvedUserNames = new Dictionary<TeamsParticipant, ProcessedTeamsUser>();
                foreach (Match? match in matches)
                {
                    var userId = (TeamsParticipant)match?.Groups[1].Value;
                    if (!userId.IsValid)
                        continue;

                    var user = await userRegistry.GetUserByIdAsync(ctx, userId, false);
                    if (user != null && user.HasDisplayName)
                    {
                        logger.Debug("[{TenantName}] Found unresolved user id {UserId} and resolved it to name {Name}", ctx.Tenant.TenantName, userId, user.DisplayName);
                        resolvedUserNames[userId] = user;
                    }
                    else
                    {
                        logger.Verbose("[{TenantName}] Found unresolved user id {UserId} and could not resolve it to display name", ctx.Tenant.TenantName, userId);
                    }
                }

                if (resolvedUserNames.Count > 0)
                {
                    var stringToHandleWithPlaceholdersReplaced = Regex.Replace(stringToHandle, pattern, (match) =>
                    {
                        var userId = (TeamsParticipant)match.Groups[1].Value;
                        if (resolvedUserNames.TryGetValue(userId, out var user))
                        {
                            notes.Add($"Replacing '{match.Value}' with '{user.DisplayName}' in {replacementTarget}");
                            return user.DisplayName;
                        }
                        else
                        {
                            return match.Value;
                        }
                    });
                    if (!stringToHandle.Equals(stringToHandleWithPlaceholdersReplaced))
                    {
                        stringSetter(message, stringToHandleWithPlaceholdersReplaced);
                        replacedSomething = true;
                    }
                }
            }
            return (replacedSomething, notes);
        }

        public void AttachToRequestPipeline(IObservable<TeamsDataContext> requestPipeline, CancellationToken cancellationToken = default)
        {
            this.cancellationToken = cancellationToken;
            requestPipeline
                .Merge(internalChatRetrievalRequests)
                .GroupBy(ctx => ctx.Tenant.TenantId)
                .ForEachAsync(group =>
                {
                    group
                    .Throttle(TimeSpan.FromSeconds(30)) // handle one tenant only every x seconds
                    .Subscribe(ctx =>
                    {
                        lock (this)
                        {
                            RetrieveChatsAsync(ctx).Wait();
                        }
                    });
                });
        }

        private void EnsureLongPollingEndpointsAreUpAndRunning(TeamsDataContext ctx)
        {
            if (config.TenantIdsToNotSubscribeForNotifications != null && config.TenantIdsToNotSubscribeForNotifications.Contains(ctx.Tenant.TenantId, StringComparer.InvariantCultureIgnoreCase))
            {
                logger.Debug("[{TenantName}] Skipping subscription for events for tenant since it is included in the ignore list.", ctx.Tenant.TenantName);
                return;
            }

            longPollingRegistry.EnsureLongPollingEndpointWithPoliciesAsync(ctx,
                chatMessages =>
                {
                    _ = Task.Run(async () =>
                    {
                        logger.Debug("[{TenantName}] Retrieved {0} chat messages as notification from {@From}", ctx.Tenant.TenantName, chatMessages.Count, chatMessages.Select(m => m.From.Select(u => u.DisplayName)));
                        var someFailed = false;
                        foreach (var message in chatMessages)
                        {
                            var result = await chatRegistry.StoreSingleChatMessageAsync(ctx, message);
                            if (!result)
                            {
                                someFailed = true;
                            }
                        }
                        if (someFailed)
                        {
                            logger.Debug("[{TenantName}] Received notifications for chats yet unknown. Queueing chat retrieval.", ctx.Tenant.TenantName);
                            // this will retrieve new chats and create the corresponding folders
                            internalChatRetrievalRequests.OnNext(ctx);
                        }
                    });
                }, default);
        }

        private SimplePriorityQueue<ChatQueueItem, HigherVersionWinsComparerChat> GetQueueForDataContext(TeamsDataContext ctx)
        {
            if (!chatsToRetrieve.TryGetValue(ctx, out var queue))
            {
                queue = new SimplePriorityQueue<ChatQueueItem, HigherVersionWinsComparerChat>(new LowerVersionWinsChatComparer());
                // try to add new queue; this might fail if another thread already did this
                if (!chatsToRetrieve.TryAdd(ctx, queue))
                {
                    // in case another thread added a queue use it
                    queue = chatsToRetrieve[ctx];
                } else 
                {
                    logger.Debug("[{TenantName}] Created thread retrieval queue for tenant", ctx.Tenant.TenantName);
                }
            }
            lock(threads)
            {
                if (!threads.TryGetValue(ctx, out var thread))
                {
                    thread = new Thread(() => ThreadFunc(ctx)) { IsBackground = true };
                    thread.Start();
                    logger.Debug("[{TenantName}] Starting retrieval thread for tenant", ctx.Tenant.TenantName);
                    threads.Add(ctx, thread);
                }
            }

            return queue;
        }

        public async Task WaitForThreadsToEnd()
        {
            logger.Debug("Waiting for all chat retrieval threads come to an end (this might take some time if a chat is currently being handled)...");
            var waitSecondsCount = 0;
            while (threads.Values.Where(value => value.IsAlive).Any())
            {
                await Task.Delay(1000);

                if (waitSecondsCount++ > 60)
                {
                    logger.Debug("Cancelling due to timeout: Waiting for all chat retrieval threads come to an end");
                    break;
                }
            }
            logger.Debug("DONE: Waiting for all chat retrieval threads come to an end");
        }

        private async Task RetrieveChatsAsync(TeamsDataContext ctx)
        {
            logger.Debug("[{TenantName}] Entering {Method} for tenant | {Context}", ctx.Tenant.TenantName, nameof(RetrieveChatsAsync), ctx);

            var debugWhiteList = new List<string>();

            if (debugWhiteList.Count == 0)
                EnsureLongPollingEndpointsAreUpAndRunning(ctx);

            MyChatsAndTeams? myChatsAndTeams = await chatRegistry.GetUpdatedChatsAndTeamsAsync(ctx);
            if (myChatsAndTeams == null)
            {
                logger.Debug("[{TenantName}] Got no chats in {Method}, exiting", ctx.Tenant.TenantName, nameof(RetrieveChatsAsync));
                return;
            }

            logger.Debug("[{TenantName}] Retrieved {ChatCount} chats, queuing all for retrieval/update check | {Context}", ctx.Tenant.TenantName, myChatsAndTeams.chats.Count, ctx);
            var queue = GetQueueForDataContext(ctx);
            foreach (var chat in myChatsAndTeams.chats)
            {
                var comparableChat = new HigherVersionWinsComparerChat(chat);
                if (queue.TryRemove(new ChatQueueItem(ctx, comparableChat)))
                {
                    logger.Verbose("[{TenantName}] Updating already queued chat {ChatId} retrieval entry | {Context}", ctx.Tenant.TenantName, chat.id.Truncate(Constants.ChatIdLogLength, true), ctx);
                }

                if (debugWhiteList.Count == 0 || debugWhiteList.Contains(chat.id))
                {
                    queue.Enqueue(new ChatQueueItem(ctx, comparableChat), comparableChat);
                }
            }
        }
    }
}
