using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Reactive.Subjects;
using Serilog;
using TeasmCompanion.TeamsInternal.TeamsInternalApiAccessor;
using TeasmCompanion.TeamsTokenRetrieval;

#nullable enable

namespace TeasmCompanion.TeamsMonitors
{
    public class TeamsUserTenantsRetriever
    {
        public IObservable<TeamsTokenInfo> TokenSource { get; }
        private readonly TeamsGlobalApiAccessor teamsGlobalApiAccessor;

        private readonly List<TeamsDataContext> dataContexts = new List<TeamsDataContext>();
        private readonly ILogger logger;
        public ReplaySubject<TeamsDataContext> TenantSource = new ReplaySubject<TeamsDataContext>();

        public TeamsUserTenantsRetriever(ILogger logger, TeamsTokenRetriever tokens, TeamsGlobalApiAccessor teamsGlobalApiAccessor)
        {
            this.logger = logger.ForContext<TeamsUserTenantsRetriever>();
            this.TokenSource = tokens.TokenSource;
            this.teamsGlobalApiAccessor = teamsGlobalApiAccessor;
        }

        public List<TeamsDataContext> GetDataContexts()
        {
            return dataContexts.ToList();
        }

        public void StartRetrievingTenants()
        {
            TokenSource
                .Where(tokenInfo => tokenInfo.TokenType == TeamsTokenType.MyTenantsAuthHeader)
                .Subscribe(async tokenInfo =>
                {
                    await UpdateTenantsForTokenAsync(tokenInfo);
                });
        }

        private async Task UpdateTenantsForTokenAsync(TeamsTokenInfo tokenInfo)
        {
            Debug.Assert(tokenInfo.TokenType == TeamsTokenType.MyTenantsAuthHeader);

            var contexts = dataContexts.Where(ctx => ctx.Tenant?.UserId == tokenInfo.UserId);
            if (!contexts.Any())
            {
                var tenants = await teamsGlobalApiAccessor.GetTenantsAsync(tokenInfo);
                if (tenants == null)
                    return;

                var mainTenant = tenants.Where(t => t.UserType == "member");
                if (!mainTenant.Any())
                {
                    logger.Warning("Cannot find main tenant for user {UserId}", tokenInfo.UserId);
                    return;
                }
                if (mainTenant.Count() > 1)
                {
                    logger.Warning("Found multiple main tenants for user {UserId}; this needs to be handled in code", tokenInfo.UserId);
                    return;
                }
                var mainUserId = mainTenant.Single().UserId;

                foreach (var tenant in tenants)
                {
                    var dataContext = new TeamsDataContext(mainUserId, tenant);
                    dataContexts.Add(dataContext);
                    TenantSource.OnNext(dataContext);
                }
            }
        }
    }
}
