using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using TeasmCompanion.Misc;
using TeasmCompanion.ProcessedTeamsObjects;
using TeasmCompanion.Registries;
using TeasmCompanion.TeamsInternal.TeamsInternalApi.api.mt.emea.beta.users;
using TeasmCompanion.TeamsInternal.TeamsInternalApi.v1.users.me.properties;
using TeasmCompanion.TeamsTokenRetrieval;

#nullable enable

namespace TeasmCompanion.TeamsInternal.TeamsInternalApiAccessor
{
    public class TeamsGlobalApiAccessor
    {
        private readonly ILogger logger;

        private TeamsTokenRetriever tokenRetriever { get; set; }
        public List<ProcessedTenant> Tenants { get; private set; }

        public TeamsGlobalApiAccessor(ILogger logger, TeamsTokenRetriever tokenRetriever)
        {
            Tenants = new List<ProcessedTenant>();
            this.logger = logger.ForContext<TeamsGlobalApiAccessor>();
            this.tokenRetriever = tokenRetriever;
        }

        private List<ProcessedTenant> UpdateTenantList(IEnumerable<Tenant> tenants)
        {
            logger.Information("Updating known tenant list with tenants: {Tenants}", tenants.Select(t => (t.tenantName, t.tenantId)));
            var addedTenants = tenants.Select(t => new ProcessedTenant(t, DateTime.UtcNow)).ToList();
            lock (Tenants)
            {
                Tenants.RemoveAll(existingTenant => addedTenants.FirstOrDefault(newTenant => newTenant.TenantId == existingTenant.TenantId && newTenant.UserId == existingTenant.UserId) != null);
                Tenants.AddRange(addedTenants);
            }
            return addedTenants;
        }

        public ProcessedTenant? TenantByName(string tenantName)
        {
            ProcessedTenant? result;
            lock (Tenants)
            {
                result = Tenants.Where(t => t.TenantName == tenantName).FirstOrDefault();
            }
            return result;
        }

        public async Task<IEnumerable<ProcessedTenant>?> GetTenantsAsync(TeamsParticipant userId)
        {
            var userContext = tokenRetriever.GetOrCreateUserTokenContext(userId);
            var tokenType = TeamsTokenType.MyTenantsAuthHeader;
            var tokenInfo = userContext[tokenType];
            if (tokenInfo == null)
            {
                logger.Debug("Cannot get tenants for user {Mri}, no token of type {TokenType} present", userId.ToString().Truncate(Constants.UserIdLogLength), tokenType);
                return null;
            }

            return await GetTenantsAsync(tokenInfo);
        }


        public async Task<IEnumerable<ProcessedTenant>> GetTenantsAsync(TeamsTokenInfo tokenInfo)
        {
            logger.Debug("Start: Retrieving tenants for user {UserId}...", tokenInfo.UserId.Truncate(Constants.UserIdLogLength, true));
            try
            {
                var client = Utils.CreateHttpClient();
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("authority", "teams.microsoft.com");
                client.DefaultRequestHeaders.Add("scheme", "https");
                client.DefaultRequestHeaders.Add("pragma", "no-cache");
                client.DefaultRequestHeaders.Add("cache-control", "no-cache");
                client.DefaultRequestHeaders.Add("x-ms-client-type", "web");
                client.DefaultRequestHeaders.Add("Authorization", tokenInfo.AuthHeader);
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36 Edg/87.0.664.57");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("Referer", "https://teams.microsoft.com/");
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
                client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("de"));

                var result = await client.GetAsync("https://teams.microsoft.com/api/mt/emea/beta/users/tenants");
                if (result.IsSuccessStatusCode)
                {
                    var buffer = await result.Content.ReadAsByteArrayAsync();
                    var data = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
                    var tenants = JsonUtils.DeserializeObject<List<Tenant>>(logger, data);
                    logger.Debug("Got tenant list for user {UserId}: {TenantCount}", tokenInfo.UserId.Truncate(Constants.UserIdLogLength, true), tenants.Count);
                    return UpdateTenantList(tenants);
                }
                else
                {
                    logger.Debug("No success status code while retrieving tenants for user {UserId}: {StatusCode}", tokenInfo.UserId.Truncate(Constants.UserIdLogLength, true), result.StatusCode);
                    return new List<ProcessedTenant>();
                }
            }
            finally
            {
                logger.Debug("Done: Retrieving tenants for user {UserId}", tokenInfo.UserId.Truncate(Constants.UserIdLogLength, true));
            }
        }

        public async Task<List<ProcessedUser>> RetrieveIdentityPropsWherePossibleAsync()
        {
            var result = new List<ProcessedUser>();
            var usersWithTokens = tokenRetriever.GetIdentitiesWithToken(TeamsTokenType.MyChatsAuthHeader);

            foreach (var userContext in usersWithTokens)
            {
                logger.Debug("Retrieving identity props for user {UserId}", userContext.UserId.Truncate(Constants.UserIdLogLength, true));
                var client = Utils.CreateHttpClient();
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Authentication", userContext[TeamsTokenType.MyChatsAuthHeader]?.AuthHeader);
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36 Edg/87.0.664.57");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("Origin", "https://teams.microsoft.com");
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
                client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("de"));
                // https://emea.ng.msg.teams.microsoft.com/v1/users/ME/properties";
                var url = $"{userContext.ChatServiceUrl}/v1/users/ME/properties";

                var httpResult = await client.GetAsync(url);
                if (httpResult.IsSuccessStatusCode)
                {
                    var buffer = await httpResult.Content.ReadAsByteArrayAsync();
                    var data = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
                    var myProps = JsonUtils.DeserializeObject<MyProperties>(logger, data);
                    result.Add(new ProcessedUser(userContext.UserId, myProps));
                }
            }

            return result;
        }

        private async Task GetMyPropsAsync(string authHeader)
        {
            var url = "https://emea.ng.msg.teams.microsoft.com/v1/users/ME/properties";
            var client = Utils.CreateHttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Authentication", authHeader);
            client.DefaultRequestHeaders.Add("Host", "emea.ng.msg.teams.microsoft.com");
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36 Edg/87.0.664.57");
            client.DefaultRequestHeaders.Add("ClientInfo", "os=windows; osVer=10; proc=x86; lcid=de-de; deviceType=1; country=de; clientName=skypeteams; utcOffset=+01:00; timezone=Europe/Berlin");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("Origin", "https://teams.microsoft.com");
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
            client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("de"));

            var result = await client.GetAsync(url);
            if (result.IsSuccessStatusCode)
            {
                var buffer = await result.Content.ReadAsByteArrayAsync();
                var data = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
                var myProps = JsonUtils.DeserializeObject<MyProperties>(logger, data);

                //return tenants;
            }
        }

    }
}


// GET /api/csa/api/v1/teams/users/me/pinnedChannels HTTP/1.1
// GET /api/csa/api/v1/teams/19:id@thread.tacv2/channels/19:id@thread.tacv2?pageSize=5 HTTP/1.1
// GET /api/csa/api/v1/teams/users/me/teams/tagCards/?pageSize=500&tagType=any HTTP/1.1