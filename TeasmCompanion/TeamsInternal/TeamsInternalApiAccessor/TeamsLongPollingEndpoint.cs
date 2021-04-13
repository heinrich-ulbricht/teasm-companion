using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using TeasmCompanion.Interfaces;
using TeasmCompanion.Misc;
using TeasmCompanion.TeamsInternal.TeamsInternalApi.v2.users.me.endpoints;
using TeasmCompanion.TeamsTokenRetrieval;

#nullable enable

namespace TeasmCompanion.TeamsInternal.TeamsInternalApiAccessor
{
    public class TeamsLongPollingEndpoint
    {
        private readonly ILogger logger;
        private readonly TeamsTokenRetriever tokenRetriever;
        private readonly RegisterEndpoint_ResponseBody endpoint;
        private readonly IProcessedNotificationMessageFactory processedNotificationMessageFactory;
        private readonly TeamsLongPollingApiAccessor apiAccessor;
        private bool isPolling = false;
        private bool isDeleted = false;

        public string Id { get => endpoint.id; }

        public TeamsLongPollingEndpoint(ILogger logger, TeamsTokenRetriever tokenRetriever, IProcessedNotificationMessageFactory processedNotificationMessageFactory, TeamsLongPollingApiAccessor apiAccessor, RegisterEndpoint_ResponseBody endpoint)
        {
            this.logger = logger.ForContext<TeamsLongPollingEndpoint>();
            this.tokenRetriever = tokenRetriever;
            this.endpoint = endpoint;
            this.processedNotificationMessageFactory = processedNotificationMessageFactory;
            this.apiAccessor = apiAccessor;
        }

        public bool IsValid()
        {
            return !isDeleted && isPolling;
        }

        private bool IsChatMessage(Eventmessage value)
        {
            // this catches "normal" chat messages
            if (value.resource?.threadtype == "chat" || value.resource?.threadtype == "sfbinteropchat")
                return true;

            // cannot handle channel messages yet
            if (value.resource?.threadtype == "topic")
                return false;

            // this catches threadtype "meeting"
            if (value.resourceType == "NewMessage" && !string.IsNullOrEmpty(value.resource?.content))
                return true;

            return false;
        }

        public async Task<string?> PollAsync(TeamsDataContext ctx, string url, Action<List<IChatMessage>> onChatChanged)
        {
            var userId = ctx.Tenant.UserId;
            var tokenContext = tokenRetriever.GetOrCreateUserTokenContext(userId);
            var tokenInfo = tokenContext[TeamsTokenType.MyChatsAuthHeader];

            if (tokenInfo == null || !tokenInfo.IsValid())
            {
                logger.Debug("[{TenantName}] Exiting PollAsync for endpoint {EndpointId} because no token found or already expired", ctx.Tenant.TenantName, Id.Truncate(Constants.UserIdLogLength, true));
                return null;
            }

            var client = Utils.CreateHttpClient();
            client.Timeout = TimeSpan.FromSeconds(60);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("x-ms-client-type", "web");
            client.DefaultRequestHeaders.Add("Authentication", tokenInfo.AuthHeader);
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36 Edg/87.0.664.57");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("Referer", "https://teams.microsoft.com/");
            client.DefaultRequestHeaders.Add("Origin", "https://teams.microsoft.com");
            client.DefaultRequestHeaders.Add("ClientInfo", "os=windows; osVer=10; proc=x86; lcid=de-de; deviceType=1; country=de; clientName=skypeteams; utcOffset=+01:00; timezone=Europe/Berlin");
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
            client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("de"));

            // let exception through
            var messagesHttpResult = await client.GetAsync(url);
            if (messagesHttpResult.IsSuccessStatusCode)
            {
                var buffer = await messagesHttpResult.Content.ReadAsByteArrayAsync();
                var data = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
                var result = JsonUtils.DeserializeObject<GET_Endpoint_ResponseBody>(logger, data);
                if (result?.eventMessages?.Count > 0)
                {
                    logger.Debug("[{TenantName}] Long polling for endpoint {EndpointId} returned with result: \r\nRaw: {@Data}\r\nParsed: {@Result}", ctx.Tenant.TenantName, Id.Truncate(Constants.UserIdLogLength, true), data, result);
                    var realChatMessages = result
                                    .eventMessages?
                                    .Where(e => IsChatMessage(e)) ?? new List<Eventmessage>();

                    var chatMessages = new List<IChatMessage>();
                    foreach (var m in realChatMessages)
                    {
                        string chatId = m.resource?.to ?? "";
                        if (string.IsNullOrWhiteSpace(chatId))
                            continue;

                        if (m.resource == null)
                            continue;

                        var processedMessage = processedNotificationMessageFactory.CreateProcessedNotificationMessage();
                        chatMessages.Add(await processedMessage.InitFromMessageAsync(ctx, chatId, m.resource));
                    }
                    if (chatMessages.Count > 0)
                    {
                        onChatChanged(chatMessages);
                    }
                } else
                {
                    logger.Verbose("[{TenantName}] Long polling for endpoint {EndpointId} returned empty", ctx.Tenant.TenantName, Id.Truncate(Constants.UserIdLogLength, true));
                }

                return result?.next;
            } else
            {
                logger.Debug("[{TenantName}] Long polling for endpoint {EndpointId} returned with non-success status code; complete result: \r\n{@messagesHttpResult}", ctx.Tenant.TenantName, Id.Truncate(Constants.UserIdLogLength, true), messagesHttpResult);
            }

            return null;
        }

        private async Task PollUntilErrorInternalAsync(TeamsDataContext ctx, Action<List<IChatMessage>> onChatChanged)
        {
            logger.Debug("[{TenantName}] Beginning long poll for tenant with endpoint {EndpointId}", ctx.Tenant.TenantName, Id.Truncate(Constants.UserIdLogLength, true));
            var nextUrl = endpoint.subscriptions[0].longPollUrl;
            while (nextUrl != null && !isDeleted)
            {
                nextUrl = await PollAsync(ctx, nextUrl, onChatChanged);
                if (nextUrl == null)
                {
                    isPolling = false;
                    throw new TeamsLongPollException();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="onChatChanged"></param>
        /// <returns>True if a new job has been registered, false if an existing job is already running.</returns>
        public async Task<bool> PollUntilErrorAsync(TeamsDataContext ctx, Action<List<IChatMessage>> onChatChanged)
        {
            if (isDeleted)
            {
                throw new InvalidOperationException($"Cannot start polling on deleted endpoint {Id}");
            }

            if (isPolling)
            {
                throw new InvalidOperationException($"Cannot start polling on already being-polled endpoint {Id}");
            }
            isPolling = true;
            await PollUntilErrorInternalAsync(ctx, (messages) => 
            {
                // if this endpoint should be deleted but still deliveres notifications - ignore those
                if (!isDeleted)
                {
                    onChatChanged(messages);
                }
            });

            // this probably won't ever get called
            return true;            
        }

        public async Task<bool?> DeleteAsync(TeamsDataContext ctx)
        {
            logger.Debug("[{TenantName}] Deleting long poll endpoint {EndpointId}", ctx.Tenant.TenantName, Id.Truncate(Constants.UserIdLogLength, true));
            isDeleted = true;
            isPolling = false;
            return await apiAccessor.DeleteEndpoint(ctx, Id);
        }
    }
}
