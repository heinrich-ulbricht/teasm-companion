using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using TeasmCompanion.Misc;
using TeasmCompanion.TeamsInternal.TeamsInternalApi.v2.users.me.endpoints;
using TeasmCompanion.TeamsTokenRetrieval;

#nullable enable

namespace TeasmCompanion.TeamsInternal.TeamsInternalApiAccessor
{
    public class TeamsLongPollingApiAccessor
    {
        private readonly ILogger logger;
        private readonly TeamsTokenRetriever tokenRetriever;
        private readonly IProcessedNotificationMessageFactory processedNotificationMessageFactory;

        public TeamsLongPollingApiAccessor(ILogger logger, TeamsTokenRetriever tokenRetriever, IProcessedNotificationMessageFactory processedNotificationMessageFactory)
        {
            this.logger = logger.ForContext<TeamsGlobalApiAccessor>();
            this.tokenRetriever = tokenRetriever;
            this.processedNotificationMessageFactory = processedNotificationMessageFactory;
        }

        public async Task<TeamsLongPollingEndpoint?> CreateEndpoint(TeamsDataContext ctx)
        {
            logger.Debug("[{TenantName}] Entering CreateEndpoint to create long poll endpoint", ctx.Tenant.TenantName);
            var userId = ctx.Tenant.UserId;
            var tokenContext = tokenRetriever.GetOrCreateUserTokenContext(userId);
            var tokenInfo = tokenContext[TeamsTokenType.MyChatsAuthHeader];

            if (tokenInfo == null || !tokenInfo.IsValid())
            {
                logger.Debug("[{TenantName}] Exiting CreateEndpoint because no token found or already expired", ctx.Tenant.TenantName);
                return null;
            }

            var client = Utils.CreateHttpClient();
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

            var endpointId = Guid.NewGuid();
            var url = $"{tokenContext?.ChatServiceUrl}/v2/users/ME/endpoints/{endpointId}";

            var requestBody = new PUT_RegisterEndpoint_RequestBody()
            {
                startingTimeSpan = 0,
                endpointFeatures = "Agent,Presence2015,MessageProperties,CustomUserProperties,NotificationStream,SupportsSkipRosterFromThreads",
                subscriptions = new List<RequestSubscription>() { 
                    new RequestSubscription() { 
                        channelType = "HttpLongPoll", 
                        interestedResources = new List<string>() { 
                            "/v1/users/ME/conversations/ALL/properties", 
                            "/v1/users/ME/conversations/ALL/messages", 
                            "/v1/threads/ALL" } }
                }
            };

            var messagesHttpResult = await client.PutAsync(url,
                new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json"));
            if (messagesHttpResult.IsSuccessStatusCode)
            {
                var buffer = await messagesHttpResult.Content.ReadAsByteArrayAsync();
                var data = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
                var result = JsonUtils.DeserializeObject<RegisterEndpoint_ResponseBody>(logger, data);
                logger.Debug("[{TenantName}] Successfully created long poll endpoint: {EndpointId}", ctx.Tenant.TenantName, result.id.Truncate(Constants.UserIdLogLength, true));
                return new TeamsLongPollingEndpoint(logger, tokenRetriever, processedNotificationMessageFactory, this, result);
            } else
            {
                logger.Debug("[{TenantName}] Got non-success status code for long poll endpoint creation; complete result: \r\n{@messagesHttpResult}", ctx.Tenant.TenantName, messagesHttpResult);
            }

            return null;
        }

        public async Task<TeamsLongPollingEndpoint?> GetExistingEndpoint(TeamsDataContext ctx, string endpointId)
        {
            logger.Debug("[{TenantName}] Entering GetExistingEndpoint to get long poll endpoint status", ctx.Tenant.TenantName);
            var userId = ctx.Tenant.UserId;
            var tokenContext = tokenRetriever.GetOrCreateUserTokenContext(userId);
            var tokenInfo = tokenContext[TeamsTokenType.MyChatsAuthHeader];

            if (tokenInfo == null || !tokenInfo.IsValid())
            {
                logger.Debug("[{TenantName}] Exiting GetExistingEndpoint because no token found or already expired", ctx.Tenant.TenantName);
                return null;
            }

            var client = Utils.CreateHttpClient();
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

            var url = $"{tokenContext?.ChatServiceUrl}/v2/users/ME/endpoints/{endpointId}";
            var messagesHttpResult = await client.GetAsync(url);
            if (messagesHttpResult.IsSuccessStatusCode)
            {
                var buffer = await messagesHttpResult.Content.ReadAsByteArrayAsync();
                var data = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
                var result = JsonUtils.DeserializeObject<RegisterEndpoint_ResponseBody>(logger, data);
                logger.Debug("[{TenantName}] Successfully got long poll endpoint info: {EndpointId}", ctx.Tenant.TenantName, result.id.Truncate(Constants.UserIdLogLength, true));
                return new TeamsLongPollingEndpoint(logger, tokenRetriever, processedNotificationMessageFactory, this, result);
            }
            else
            {
                logger.Debug("[{TenantName}] Got non-success status code for long poll endpoint status retrieval; complete result: \r\n{@messagesHttpResult}", ctx.Tenant.TenantName, messagesHttpResult);
            }

            return null;
        }

        public async Task<bool?> DeleteEndpoint(TeamsDataContext ctx, string endpointId)
        {
            logger.Debug("[{TenantName}] Entering GetEndpointInfo to get long poll endpoint status", ctx.Tenant.TenantName);
            var userId = ctx.Tenant.UserId;
            var tokenContext = tokenRetriever.GetOrCreateUserTokenContext(userId);
            var tokenInfo = tokenContext[TeamsTokenType.MyChatsAuthHeader];

            if (tokenInfo == null || !tokenInfo.IsValid())
            {
                logger.Debug("[{TenantName}] Exiting DeleteEndpoint because no token found or already expired", ctx.Tenant.TenantName);
                return null;
            }

            var client = Utils.CreateHttpClient();
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

            var url = $"{tokenContext?.ChatServiceUrl}/v2/users/ME/endpoints/{endpointId}";
            var messagesHttpResult = await client.DeleteAsync(url);
            if (messagesHttpResult.IsSuccessStatusCode)
            {
                logger.Debug("[{TenantName}] Successfully deleted long poll endpoint: {EndpointId}", ctx.Tenant.TenantName, endpointId.Truncate(Constants.UserIdLogLength, true));
                return true;
            }
            else
            {
                logger.Debug("[{TenantName}] Got non-success status code for long poll endpoint deletion; complete result: \r\n{@messagesHttpResult}", ctx.Tenant.TenantName, messagesHttpResult);
                return false;
            }
        }
    }
}
