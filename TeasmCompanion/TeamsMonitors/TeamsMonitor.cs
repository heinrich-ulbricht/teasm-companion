using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Reactive.Linq;
using TeasmCompanion.TeamsMonitors;
using System.Threading;
using System.Reactive.Subjects;
using System.Collections.Generic;
using TeasmCompanion.TeamsTokenRetrieval;
using TeasmCompanion.Interfaces;
#nullable enable

namespace TeasmCompanion
{
    public class TeamsMonitor
    {
        private readonly TeamsTokenRetriever tokens;
        private readonly TeamsUserTenantsRetriever tenantsRetriever;
        private readonly TeamsChatRetriever teamsChatReceiver;
        private readonly TeamsUserTenantsRetriever userTenantsRetriever;
        private readonly ITeamsUserStore userStore;
        private readonly ILogger logger;
        private readonly ReplaySubject<TeamsDataContext> RetrieveChatsPipeline = new ReplaySubject<TeamsDataContext>();

        public TeamsMonitor(
            ILogger logger, 
            TeamsTokenRetriever tokens, 
            TeamsUserTenantsRetriever tenantsRetriever,
            TeamsChatRetriever teamsChatReceiver,
            TeamsUserTenantsRetriever userTenantsRetriever,
            ITeamsUserStore userStore)
        {
            this.logger = logger.ForContext<TeamsMonitor>();
            this.tokens = tokens;
            this.tenantsRetriever = tenantsRetriever;
            this.teamsChatReceiver = teamsChatReceiver;
            this.userTenantsRetriever = userTenantsRetriever;
            this.userStore = userStore;
        }

        class DataContextWithToken
        {
            public TeamsDataContext DataContext { get; private set; }
            public TeamsTokenInfo TokenInfo { get; private set; }

            public DataContextWithToken(TeamsTokenInfo tokenInfo, TeamsDataContext dataContext)
            {
                DataContext = dataContext;
                TokenInfo = tokenInfo;
            }
        }

        public async Task GoAsync(CancellationToken cancellationToken)
        {
            try
            {
                // check store access; for IMAP server store this means that the IMAP server is up and running
                var hasStoreAccess = await userStore.CanAccessStore(cancellationToken);
                if (!hasStoreAccess)
                {
                    logger.Error("Cannot access user store and will exit now. Check log for details.");
                    return;
                }

                // retrieves everything from the current local storages as well as past known identities
                teamsChatReceiver.AttachToRequestPipeline(RetrieveChatsPipeline, cancellationToken);

                var tenantsWithValidTokensGroupedByTenantsAndTokenType = Observable
                    // combine stream of tokens with stream of tenants
                    .Join(
                        tokens.TokenSource.Where(token => token.IsValid()),
                        tenantsRetriever.TenantSource.Distinct(),
                        left => Observable.Return(0).Delay(left.ValidToUtc - DateTime.UtcNow), // specify the window duration as equal to token lifetime
                        right => Observable.Never<int>(),
                        (left, right) => new DataContextWithToken(left, right)
                        )
                    // filter out valid combinations where the user ID maps between token and tenant
                    .Where(o => o.TokenInfo.UserId == o.DataContext.Tenant?.UserId)
                    // create token stream for each tenant and token type
                    .GroupBy(o => (o.DataContext.Tenant.TenantId, o.TokenInfo.TokenType))
                    .Select(group => new { group.Key, Data = group });

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

                var tenantBlacklistKeywords = new List<string>() { };

                tenantsWithValidTokensGroupedByTenantsAndTokenType
                    .ForEachAsync(group =>
                        group.Data
                        // create a stream of data where each token is being delivered at the expiration time of the previous token
                        .Scan(new { Data = Observable.Empty<DataContextWithToken>(), TokenLifeTime = TimeSpan.FromSeconds(0) },
                             (acc, data) => new { Data = Observable.Delay(Observable.Return(data), acc.TokenLifeTime), TokenLifeTime = data.TokenInfo.ValidToUtc - DateTime.UtcNow })
                        .SelectMany(o => o.Data)
                        // don't serve expired tokens
                        .Where(o => o.TokenInfo.IsValid())
                        // filter duplicates that might be present due to regularly reading of the tokens
                        .Distinct(o => o.TokenInfo.ValidToUtc)
                        .Where(o => !tenantBlacklistKeywords.Where(keyword => o.DataContext.Tenant.TenantName.Contains(keyword, StringComparison.InvariantCultureIgnoreCase)).Any())
                        .Subscribe(data =>
                        {
                            logger.Debug("[{TenantName}] Key {Key}, values: {TokenType}, {ValidToUtc}", data.DataContext.Tenant.TenantName, group.Key, data.TokenInfo.TokenType, data.TokenInfo.ValidToUtc.ToLocalTime());
                            if (data.TokenInfo.TokenType != TeamsTokenType.MyTenantsAuthHeader) // rather hacky optimization; chat retrieval needs 2 other tokens
                                RetrieveChatsPipeline.OnNext(data.DataContext);
                        })
                    );

#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

                var timer = new Timer((state) =>
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    logger.Debug("Time to update chats...");
                    var contexts = userTenantsRetriever.GetDataContexts();
                    var identities = tokens.GetIdentitiesWithToken(TeamsTokenType.MyChatsAuthHeader);

                    var contextsWithTokens = from context in contexts
                                             join identity in identities
                                         on context.Tenant.UserId equals identity.UserId
                                             where identity[TeamsTokenType.MyChatsAuthHeader] != null && identity[TeamsTokenType.MyTeamsAuthHeader] != null
                                             && !tenantBlacklistKeywords.Where(keyword => context.Tenant.TenantName.Contains(keyword, StringComparison.InvariantCultureIgnoreCase)).Any()
                                             select new
                                             {
                                                 Context = context,
                                                 Identity = identity
                                             };

                    logger.Debug("Got {0} contexts with tokens to retrieve chats", contextsWithTokens.Count());
                    foreach (var q in contextsWithTokens)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            return;

                        logger.Debug("{TenantName} queued for chat retrieval (in timer)", q.Context.Tenant.TenantName);
                        // do this one after another - don't know how much load we can put on the endpoints

                        RetrieveChatsPipeline.OnNext(q.Context);
                    }
                }, null, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(120));

                if (cancellationToken.IsCancellationRequested)
                    return;

                tenantsRetriever.StartRetrievingTenants();

                logger.Debug("Starting token retrieval loop");
                while (!cancellationToken.IsCancellationRequested)
                {
                    await tokens.CaptureTokensFromLevelDbLogFilesAsync(cancellationToken);
                    await tokens.CaptureTokensFromLevelDbLdbFilesAsync(cancellationToken);
                    await Task.Delay(TimeSpan.FromMinutes(10), cancellationToken);
                }
            }
            finally
            {
                if (cancellationToken.IsCancellationRequested)
                    await teamsChatReceiver.WaitForThreadsToEnd();
            }

            //await tenantsWithValidTokensGroupedByTenantsAndTokenType;
        }
    }
}
