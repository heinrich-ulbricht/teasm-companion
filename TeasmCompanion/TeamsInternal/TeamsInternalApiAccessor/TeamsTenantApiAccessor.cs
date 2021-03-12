using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Reactive.Linq;
using Serilog;
using TeasmCompanion.Misc;
using TeasmCompanion.Interfaces;
using TeasmCompanion.TeamsInternal.TeamsInternalApi.v1.users.me.conversations;
using TeasmCompanion.TeamsInternal.TeamsInternalApi.api.mt.emea.beta.users;
using TeasmCompanion.TeamsInternal.TeamsInternalApi.api.csa.api.v1.teams.users;
using TeasmCompanion.TeamsTokenRetrieval;
using TeasmCompanion.Registries;
using TeasmCompanion.ProcessedTeamsObjects;
using static TeasmCompanion.ProcessedTeamsUser;
using System.Globalization;

#nullable enable

namespace TeasmCompanion.TeamsInternal.TeamsInternalApiAccessor
{
    public class TeamsTenantApiAccessor
    {
        private readonly IProcessedMessageFactory processedMessageFactory;
        private readonly Configuration config;
        private readonly TeamsGlobalApiAccessor teamsGlobalScope;
        private readonly ITeamsUserRegistry teamsUserRegistry;
        private readonly ILogger logger;
        private readonly TeamsTokenRetriever tokenRetriever;

        public TeamsTenantApiAccessor(
            ILogger logger,
            TeamsTokenRetriever tokenRetriever,
            TeamsGlobalApiAccessor teamsGlobalScope,
            ITeamsUserRegistry teamsUserRegistry,
            IProcessedMessageFactory processedMessageFactory,
            Configuration config)
        {
            this.logger = logger.ForContext<TeamsTenantApiAccessor>();
            this.tokenRetriever = tokenRetriever;
            this.teamsGlobalScope = teamsGlobalScope;
            this.teamsUserRegistry = teamsUserRegistry;
            this.processedMessageFactory = processedMessageFactory;
            this.config = config;
        }

        public MyChatsAndTeams SortChats(MyChatsAndTeams chatsAndTeams)
        {
            // note: version often is newer than threadVersion
            // note: threadVersion sometimes is 0 while "version" is set
            // note: version and threadVersion seem to include events like "member removed" etc.
            chatsAndTeams.chats.Sort((a, b) =>
            {
                if (a == null && b == null)
                {
                    return 0;
                }
                else
                if (a == null && b != null)
                {
                    return -1;
                }
                else
                if (b == null && a != null)
                {
                    return 1;
                }

#pragma warning disable CS8602 // Dereference of a possibly null reference.
                var firstValue = a.threadVersion > 0 ? a.threadVersion : a.version;
                var secondValue = b.threadVersion > 0 ? b.threadVersion : b.version;
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                // if there is a message then take this; note: in meetings there might be no message, only start and end events
                if (a.LastMessage?.composetime != null)
                {
                    firstValue = a.LastMessage.composetime.ToJavaScriptMilliseconds();
                }
                if (b.LastMessage?.composetime != null)
                {
                    secondValue = b.LastMessage.composetime.ToJavaScriptMilliseconds();
                }

                // for meetings where never somebody wrote something we take the end time; member changes (due to deactivated accounts etc.) shouldn't "update" the meeting chat for our purposes
                if (a.chatType == "meeting" && a.LastMessage?.composetime == null && a.meetingInformation?.endTime != null)
                {
                    firstValue = a.meetingInformation.endTime.ToJavaScriptUtcMilliseconds();
                }

                if (b.chatType == "meeting" && b.LastMessage?.composetime == null && b.meetingInformation?.endTime != null)
                {
                    secondValue = b.meetingInformation.endTime.ToJavaScriptUtcMilliseconds();
                }

                if (firstValue > secondValue)
                    return -1;
                else if (firstValue < secondValue)
                    return 1;
                else return 0;
            });
            return chatsAndTeams;
        }

        public async Task<MyChatsAndTeams?> GetMyChatsAndTeamsAsync(TeamsDataContext ctx, string? base64SyncToken = null)
        {
            var userId = ctx.Tenant.UserId;
            logger.Debug("[{TenantName}] Entering {Method} for user {UserId}", ctx.Tenant.TenantName, nameof(GetMyChatsAndTeamsAsync), userId.Truncate(Constants.UserIdLogLength, true));

            var tokenContext = tokenRetriever.GetOrCreateUserTokenContext(userId);
            var tokenInfo = tokenContext[TeamsTokenType.MyTeamsAuthHeader];

            if (tokenInfo == null || !tokenInfo.IsValid())
            {
                logger.Debug("[{TenantName}] Exiting {Method} because no token found or already expired.", ctx.Tenant.TenantName, nameof(GetMyChatsAndTeamsAsync));
                return null;
            }

            var client = Utils.CreateHttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("x-ms-client-type", "web");
            client.DefaultRequestHeaders.Add("Authorization", tokenInfo.AuthHeader);
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36 Edg/87.0.664.57");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("Referer", "https://teams.microsoft.com/_");
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
            client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("de"));

            if (base64SyncToken != null)
            {
                var decodedSyncToken = base64SyncToken.FromBase64String();
                logger.Debug("[{TenantName}] Retrieving chats and teams update for sync token {SyncToken}", ctx.Tenant.TenantName, decodedSyncToken);
                client.DefaultRequestHeaders.Add("x-ms-synctoken", base64SyncToken);
            }

            string url;
            if (base64SyncToken == null)
            {
                url = "https://teams.microsoft.com/api/csa/api/v1/teams/users/me?isPrefetch=false&enableMembershipSummary=true";
            } else
            {
                url = "https://teams.microsoft.com/api/csa/api/v1/teams/users/me/updates?isPrefetch=false&enableMembershipSummary=true";
            }
            var result = await client.GetAsync(url);
            if (result.IsSuccessStatusCode)
            {
                var buffer = await result.Content.ReadAsByteArrayAsync();
                var data = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
                var chatsAndTeams = JsonUtils.DeserializeObject<MyChatsAndTeams>(logger, data);
                logger.Debug("[{TenantName}] Successfully retrieved chats and teams data for user {UserId}", ctx.Tenant.TenantName, userId.Truncate(Constants.UserIdLogLength, true));

                return SortChats(chatsAndTeams);
            }
            else
            {
                logger.Information("[{TenantName}] Got error status code {StatusCode} when retrieving chats for user {UserId}", ctx.Tenant.TenantName, result.StatusCode, userId.Truncate(Constants.UserIdLogLength, true));
                return null;
            }
        }

        public async Task<IEnumerable<Message>> RetrieveAllMessagesForChatAsync(TeamsDataContext ctx, Chat chat)
        {
            return await RetrieveMessagesForChatAsync(ctx, chat, 1);
        }

        /**
         * startTime = version of a chat, which is a JavaScript UTC time
         */
        public async Task<IEnumerable<Message>> RetrieveMessagesForChatSinceAsync(TeamsDataContext ctx, Chat chat, long startTime)
        {
            return await RetrieveMessagesForChatAsync(ctx, chat, startTime);
        }

        private async Task<IEnumerable<Message>> RetrieveMessagesForChatAsync(TeamsDataContext ctx, Chat chat, long startTime)
        {
            logger.Debug("[{TenantName}] Entering {Method} to retrieve messages for chat {ChatId}", ctx.Tenant.TenantName, nameof(RetrieveMessagesForChatAsync), chat.id.Truncate(Constants.ChatIdLogLength, true));
            var userId = ctx.Tenant.UserId;
            var tokenContext = tokenRetriever.GetOrCreateUserTokenContext(userId);
            var tokenInfo = tokenContext[TeamsTokenType.MyChatsAuthHeader];

            if (config.ChatIdIgnoreList.Contains(chat.id, StringComparer.InvariantCultureIgnoreCase))
            {
                logger.Debug("[{TenantName}] Chat {ChatId} is on the ignore list, exiting with fake empty message list", ctx.Tenant.TenantName, chat.id.Truncate(Constants.ChatIdLogLength, true));
                return new List<Message>();
            }

            if (tokenInfo == null || !tokenInfo.IsValid())
            {
                logger.Debug("[{TenantName}] Exiting {MethodName} because no token found or already expired.", ctx.Tenant.TenantName, nameof(RetrieveMessagesForChatAsync));
                return new List<Message>();
            }

            var urlLeftPart = $"{tokenContext?.ChatServiceUrl}/v1/users/ME/conversations";

            var client = Utils.CreateHttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("x-ms-client-type", "web");
            client.DefaultRequestHeaders.Add("Authentication", tokenInfo.AuthHeader);
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36 Edg/87.0.664.57");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("Referer", "https://teams.microsoft.com/");
            client.DefaultRequestHeaders.Add("Origin", "https://teams.microsoft.com");
            client.DefaultRequestHeaders.Add("ClientInfo", "ClientInfo: os=windows; osVer=10; proc=x86; lcid=de-de; deviceType=1; country=de; clientName=skypeteams; utcOffset=+01:00; timezone=Europe/Berlin");
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
            client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("de"));

            var result = new List<Message>();
            var url = $"{urlLeftPart}/{HttpUtility.UrlEncode(chat.id)}/messages?view=msnp24Equivalent|supportsMessageProperties&pageSize=200&startTime={startTime}";
            var waitSomeTime = false;
            do
            {
                if (waitSomeTime)
                {
                    var waitSecs = 30;
                    logger.Debug("[{TenantName}] Waiting {Secs} seconds before retrieving the next batch for chat {ChatId}", ctx.Tenant.TenantName, waitSecs, chat.id.Truncate(Constants.ChatIdLogLength, true));
                    await Task.Delay(TimeSpan.FromSeconds(waitSecs)); // there is no evidence that we need this but it seems like a cautious approach
                }

                logger.Debug("[{TenantName}] Retrieving messages for chat {ChatId} from URL {Url}", ctx.Tenant.TenantName, chat.id.Truncate(Constants.ChatIdLogLength, true), url);
                var messagesHttpResult = await client.GetAsync(url);
                if (messagesHttpResult.IsSuccessStatusCode)
                {
                    var buffer = await messagesHttpResult.Content.ReadAsByteArrayAsync();
                    var data = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
                    var messages = JsonUtils.DeserializeObject<ThreadMessages>(logger, data);
                    startTime = messages._metadata.lastCompleteSegmentStartTime; // this will be 1 if there is only one page, otherwise this is the startTime for the next query
                    logger.Debug("[{TenantName}] Got {Count} chat messages for chat {ChatId}", ctx.Tenant.TenantName, messages.messages.Count, chat.id.Truncate(Constants.ChatIdLogLength, true));
                    result.AddRange(messages.messages);
                    waitSomeTime = messages.messages.Count > 20;

                    if (url == messages._metadata.syncState)
                    {
                        break;
                    }
                    url = messages._metadata.syncState;
                } else {
                    break;
                }
            } while (startTime > 1);

            return result;
        }

        public async Task<ProcessedChat> ProcessChatMessagesAsync(TeamsDataContext ctx, Chat chat, IEnumerable<Message> messages)
        {
            var result = new ProcessedChat(chat);
            // like 00000000-0000-beef-0000-000000000000
            var userId = ctx.Tenant.UserId;
            // from oldest to newest
            var orderedMessages = messages.OrderBy(m => m.originalarrivaltime);
            // TODO: combine this with the conversion logic of processedMessageFactory.CreateProcessedMessage().InitFromMessageAsync -> extract users first to have names ready, then generate processed messages
            HashSet<TeamsUserWithSource> usersFromChat;
            usersFromChat = CollectUsersFromChatAndMessages(chat, orderedMessages.ToList());
            result.UserIds.AddRange(usersFromChat.Select(value => value.User).Distinct()); // TODO: check if this is still necessary of if we use TeamsUserStore instead
            await UpdateUserObjectsAsync(ctx, result.UserIds);

            // note: it is important to process them from oldest to newest to catch all user names floating around in messages (e.g. chat messages contain user display names needed to put names in call end messages)
            // note2: this does not always work which is why there is the job to resolve unknown user ids
            result.OrderedMessages = (await Task.WhenAll(orderedMessages.Select(async m => await processedMessageFactory.CreateProcessedMessage().InitFromMessageAsync(ctx, chat.id, m)))).OrderBy(m => m.OriginalArrivalTime);

            // case 1: there is a custom title
            var title = chat.title?.Trim() ?? "";

            // this handles a title containing user mris (instead of custom title set by the user)
            var matches = Regex.Matches(title, TeamsParticipant.MriPatternOpen);
            // case 2: Teams provides a title consisting of user ids - collect them
            if (matches.Count > 0)
            {
                usersFromChat.AddRange(matches.Select(g => new TeamsUserWithSource((TeamsParticipant)g.Value, TeamsUserSource.FoundInChatTitle)));
            }

            // remove self and remove any "chat" that participated
            usersFromChat.RemoveWhere(value => value.User.Equals(ctx.Tenant.UserId) || value.User.Kind == ParticipantKind.TeamsChat);
            if (usersFromChat.Count == 0)
            {
                // no users left? add self...
                usersFromChat.Add(new TeamsUserWithSource(ctx.Tenant.UserId, TeamsUserSource.Self));
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                title = string.Join(", ", usersFromChat
                    .Where(value => value.UserSource == TeamsUserSource.ChatCreator || value.UserSource == TeamsUserSource.FoundInChatTitle || value.UserSource == TeamsUserSource.OfficialChatMember || value.UserSource == TeamsUserSource.SenderOfMessageInChat || value.UserSource == TeamsUserSource.Self)
                    .Select(value => value.User)
                    .Distinct());
            }

            title = await teamsUserRegistry.ReplaceUserIdsWithDisplayNamesAsync(ctx, title?.Trim());
            if (string.IsNullOrEmpty(title))
            {
                title = chat.id;
            }
            result.ChatTitle = title;
            return result;
        }

        public enum TeamsUserSource
        {
            Unspecified,
            // was mentioned
            MentionedInChat,
            // sent a message
            SenderOfMessageInChat,
            // chat member as reported by Teams
            OfficialChatMember,
            // sometimes user MRIs are part of a chat title
            FoundInChatTitle,
            // the chat creator as reported by Teams
            ChatCreator,
            // this is me
            Self
        }

        public class TeamsUserWithSource
        {
            public TeamsUserSource UserSource { get; set; }
            public TeamsParticipant User { get; set; }

            public TeamsUserWithSource(TeamsParticipant user, TeamsUserSource userSource)
            {
                User = user;
                UserSource = userSource;
            }

            public override bool Equals(object? obj)
            {
                if (obj is TeamsUserWithSource)
                {
                    return User.Equals(((TeamsUserWithSource)obj).User) && ((TeamsUserWithSource)obj).UserSource == UserSource;
                }

                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(User, UserSource);
            }
        }

        // TBD: need to consolidate with ProcessedMessageBase.ExtractSendersReceiversAndSubject
        private HashSet<TeamsUserWithSource> CollectUsersFromChatAndMessages(Chat chat, List<Message> messages)
        {
            var users = new HashSet<TeamsUserWithSource>();
            var creatorUserId = TeamsParticipant.FromFirstValid(chat.creator);
            if (creatorUserId.IsValid)
            {
                users.Add(new TeamsUserWithSource(creatorUserId, TeamsUserSource.ChatCreator));
            }
            foreach (var m in messages)
            {
                // "https://emea.ng.msg.teams.microsoft.com/v1/users/ME/contacts/8:orgid:00000000-0000-beef-0000-000000000000"
                // note: for system messages like user leave and user join this is the contact url for the chat id
                var userId = (TeamsParticipant)m.from;
                if (userId.IsValid && !(userId.Kind == ParticipantKind.TeamsChat))
                {
                    users.Add(new TeamsUserWithSource(userId, TeamsUserSource.SenderOfMessageInChat));
                }

                if (m.properties?.mentions != null)
                {
                    foreach (var mention in m.properties.mentions)
                    {
                        users.Add(new TeamsUserWithSource((TeamsParticipant)mention.mri, TeamsUserSource.MentionedInChat));
                    }
                }
            }

            chat.members?.ForEach(member => users.Add(new TeamsUserWithSource((TeamsParticipant)member.mri, TeamsUserSource.OfficialChatMember)));
            return users;
        }

        private async Task UpdateUserObjectsAsync(TeamsDataContext ctx, IEnumerable<TeamsParticipant> userIds)
        {
            logger.Information("[{TenantName}] Processing request to update users: '{UserIds}'", ctx.Tenant.TenantName, userIds);
            foreach (var id in userIds)
            {
                await teamsUserRegistry.RecognizeUserIdAsync(ctx, id);
            }

            var userId = ctx.Tenant.UserId;
            var tokenInfo = tokenRetriever.GetTokenForIdentity(userId, "https://teams.microsoft.com/api/mt/emea/beta/users/fetch");
            if (tokenInfo == null)
            {
                logger.Debug("[{TenantName}] Cannot get user objects in user context {userId}, no token present; exiting", ctx.Tenant.TenantName, userId.Truncate(Constants.UserIdLogLength, true));
                return;
            }

            var tenantUsersWithUndefinedState = (await teamsUserRegistry.GetTenantUsersAsync(ctx)).Where(user => user.State == TeamsUserState.Undefined);
            var tenantUsersWithUndefinedStateCount = tenantUsersWithUndefinedState.Count();
            logger.Debug("[{TenantName}] Have to handle {Count} users with undefined state", ctx.Tenant.TenantName, tenantUsersWithUndefinedStateCount);
            var count = 0;
            foreach (var user in tenantUsersWithUndefinedState)
            {
                if (count > 0)
                {
                    var waitSecs = 1;
                    logger.Debug("[{TenantName}] Waiting {Secs} seconds before retrieving the user info for '{UserName}'", ctx.Tenant.TenantName, waitSecs, user.DisplayName);
                    await Task.Delay(TimeSpan.FromSeconds(waitSecs)); // there is some evidence that we need this if there are many users in undefined state
                }
                count++;
                try
                {
                    logger.Debug("[{TenantName}] Trying to get user info for '{UserName}' ({Count} of {AllCount})", ctx.Tenant.TenantName, user.DisplayName, count, tenantUsersWithUndefinedStateCount);
                    var client = Utils.CreateHttpClient();
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Add("authorization", tokenInfo.AuthHeader);
                    client.DefaultRequestHeaders.Add("authority", "teams.microsoft.com");
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.111 Safari/537.36");
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json") { CharSet = Encoding.UTF8.WebName });
                    client.DefaultRequestHeaders.Add("Origin", "https://teams.microsoft.com");
                    client.DefaultRequestHeaders.Add("Connection", "keep-alive");
                    client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                    client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
                    client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));

                    var result = await client.PostAsync("https://teams.microsoft.com/api/mt/emea/beta/users/fetch?isMailAddress=false&canBeSmtpAddress=false&enableGuest=true&includeIBBarredUsers=true&skypeTeamsInfo=true",
                        new StringContent($"[\"{user.UserId}\"]", Encoding.UTF8, "application/json"));
                    if (result.IsSuccessStatusCode)
                    {
                        var buffer = await result.Content.ReadAsByteArrayAsync();
                        var data = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
                        logger.Information("[{TenantName}] User info retrieval request returned successful for '{UserId}': {Data}", ctx.Tenant.TenantName, user.UserId, data);
                        var dataObject = JsonUtils.DeserializeObject<FetchUserResponse>(logger, data);
                        if (dataObject.value?.Count == 1)
                        {
                            logger.Information("[{TenantName}] Got info, registering '{UserName}' as found in tenant", ctx.Tenant.TenantName, user.DisplayName);
                            user.RegisterOriginalUser(dataObject.value[0]).State = TeamsUserState.FoundInTenant;
                            await teamsUserRegistry.MarkUserAsChanged(ctx, user);
                        }
                        else
                        if (dataObject.value?.Count == 0)
                        {                                
                            logger.Information("[{TenantName}] Got empty info, registering '{UserName}' as missing from tenant", ctx.Tenant.TenantName, user.DisplayName);
                            user.State = TeamsUserState.MissingFromTenant;
                            await teamsUserRegistry.MarkUserAsChanged(ctx, user);
                        }
                        else
                        {
                            throw new TeasmCompanionException($"[{ctx.Tenant.TenantName}] Found multiple users for '{user.UserId.Mri}' - this is never expected to happen");
                        }
                    }
                    else
                    {
                        if (result.StatusCode != System.Net.HttpStatusCode.Unauthorized)
                        {
                            logger.Information("[{TenantName}] Got non-success status code for user '{UserId}'; marking this user as missing from tenant", ctx.Tenant.TenantName, user.UserId);
                            user.State = TeamsUserState.MissingFromTenant; // TODO: need to check for auth errors here
                            await teamsUserRegistry.MarkUserAsChanged(ctx, user);
                        } else {
                            logger.Debug("[{TenantName}] Got auth error while fetching info for user '{UserId}'; will be tried again later", ctx.Tenant.TenantName, user.UserId);
                        }
                    }
                }
                catch (Exception e)
                {
                    logger.Warning(e, "[{TenantName}] Exception while fetching info for user '{UserId}'; this will probably happen again, investigate the cause! Eating the exception for now.", ctx.Tenant.TenantName, user.UserId);
                }
            }
        }

        public async Task DownloadImagesAsync(TeamsDataContext ctx, IOrderedEnumerable<IChatMessage> messages)
        {
            var userId = ctx.Tenant.UserId;
            var tokenContext = tokenRetriever.GetUserTokenContext(userId, true);
            var tokenInfo = tokenContext?[TeamsTokenType.MyChatsAuthHeader];
            if (tokenInfo == null || !tokenInfo.IsValid())
            {
                logger.Debug("[{TenantName}] Cannot get conversations for user {userId}, no token present", ctx.Tenant.TenantName, userId.Truncate(Constants.UserIdLogLength, true));
                return;
            }
            var clientForTeamsImages = Utils.CreateHttpClient();
            clientForTeamsImages.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            clientForTeamsImages.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            clientForTeamsImages.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
            clientForTeamsImages.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
            clientForTeamsImages.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("de"));
            clientForTeamsImages.DefaultRequestHeaders.Add("x-ms-client-type", "web");
            clientForTeamsImages.DefaultRequestHeaders.Add("Authorization", tokenInfo.AuthHeader.Replace("skypetoken=", "skype_token "));
            clientForTeamsImages.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36 Edg/87.0.664.57");
            clientForTeamsImages.DefaultRequestHeaders.Add("Referer", "https://teams.microsoft.com/");
            clientForTeamsImages.DefaultRequestHeaders.Add("Origin", "https://teams.microsoft.com");

            var clientForPublicImages = Utils.CreateHttpClient();
            clientForPublicImages.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/webp", 0.8));
            clientForPublicImages.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/apng", 0.8));
            clientForPublicImages.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/*", 0.8));
            clientForPublicImages.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.8));
            clientForPublicImages.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            clientForPublicImages.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
            clientForPublicImages.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
            clientForPublicImages.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("de"));
            clientForPublicImages.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36 Edg/87.0.664.57");

            foreach (var m in messages)
            {
                foreach (var cid in m.ContentIds.Keys)
                {
                    var imageInfo = m.ContentIds[cid];

                    byte[]? buffer;
                    try
                    {
                        buffer = await Akavache.BlobCache.UserAccount.Get(imageInfo.CacheKey);
                    }
                    catch
                    {
                        buffer = null;
                    }
                    if (buffer == null)
                    {
                        HttpResponseMessage result;

                        if (imageInfo.ImageType == ImageType.TeamsWithAuthentication)
                        {
                            result = await clientForTeamsImages.GetAsync(imageInfo.Url);
                        }
                        else if (imageInfo.ImageType == ImageType.Public)
                        {
                            result = await clientForPublicImages.GetAsync(imageInfo.Url);
                        }
                        else
                        {
                            throw new TeasmCompanionException("Unknown image type");
                        }
                        if (result.IsSuccessStatusCode)
                        {
                            buffer = await result.Content.ReadAsByteArrayAsync();
                            await Akavache.BlobCache.UserAccount.Insert(imageInfo.CacheKey, buffer);
                            await Akavache.BlobCache.UserAccount.Flush();
                        }
                    }
                }
            }
            await Akavache.BlobCache.UserAccount.Flush();
        }
    }
}
