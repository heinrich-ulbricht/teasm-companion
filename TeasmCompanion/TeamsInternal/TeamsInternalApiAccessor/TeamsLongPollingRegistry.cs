using Polly;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TeasmCompanion.Interfaces;
using TeasmCompanion.Misc;

#nullable enable

namespace TeasmCompanion.TeamsInternal.TeamsInternalApiAccessor
{
    public class TeamsLongPollingRegistry
    {
        private readonly ILogger logger;
        private readonly TeamsLongPollingApiAccessor longPollingApi;
        private readonly Dictionary<TeamsDataContext, TeamsLongPollingEndpoint> knownEndpoints = new Dictionary<TeamsDataContext, TeamsLongPollingEndpoint>();

        public TeamsLongPollingRegistry(ILogger logger, TeamsLongPollingApiAccessor longPollingApi)
        {
            this.logger = logger.ForContext<TeamsLongPollingRegistry>();
            this.longPollingApi = longPollingApi;
        }

        public TeamsLongPollingEndpoint? GetEndpoint(TeamsDataContext ctx)
        {
            lock (knownEndpoints)
            {
                if (knownEndpoints.TryGetValue(ctx, out var result))
                {
                    return result;
                }
            }

            return null;
        }

        public void RemoveEndpointIfExisting(TeamsDataContext ctx)
        {
            lock (knownEndpoints)
            {
                var endpoint = GetEndpoint(ctx);
                if (endpoint != null)
                {
                    logger.Debug("[{TenantName}] Removing long poll endpoint {EndpointId}", ctx.Tenant.TenantName, endpoint.Id.Truncate(Constants.UserIdLogLength, true));
                    knownEndpoints.Remove(ctx);
                    endpoint.DeleteAsync(ctx).Wait();
                }
            }
        }

        private async Task EnsureLongPollingEndpointAndReplaceExistingAsync(TeamsDataContext ctx, Action<List<IChatMessage>> onChatChanged, CancellationToken cancellationToken)
        {
            logger.Debug("[{TenantName}] EnsureLongPollingEndpointAndReplaceExistingAsync called", ctx.Tenant.TenantName);
            TeamsLongPollingEndpoint? endpoint;
            lock (knownEndpoints)
            {
                RemoveEndpointIfExisting(ctx);
                endpoint = longPollingApi.CreateEndpoint(ctx).Result;
                if (endpoint == null)
                {
                    logger.Debug("[{TenantName}] Couldn't create long polling endpoint", ctx.Tenant.TenantName);

                    // maybe we got no token
                    throw new TeamsLongPollException($"Got null result for long polling endpoint creation for tenant {ctx.Tenant.TenantName}");
                }
                logger.Debug("[{TenantName}] Storing long poll endpoint {EndpointId}", ctx.Tenant.TenantName, endpoint.Id.Truncate(Constants.UserIdLogLength, true));
                knownEndpoints.Add(ctx, endpoint);
            }

            logger.Debug("[{TenantName}] Starting polling for endpoint {EndpointId}", ctx.Tenant.TenantName, endpoint.Id.Truncate(Constants.UserIdLogLength, true));
            await endpoint.PollUntilErrorAsync(ctx, onChatChanged);
        }


        public async void EnsureLongPollingEndpointWithPoliciesAsync(TeamsDataContext ctx, Action<List<IChatMessage>> onChatChanged, CancellationToken cancellationToken)
        {
            var endpoint = GetEndpoint(ctx);
            if (endpoint?.IsValid() ?? false)
            {
                // keep valid endpoint running
                return;
            }

            logger.Debug("[{TenantName}] EnsureLongPollingEndpointWithPoliciesAsync called, entering wait loop", ctx.Tenant.TenantName);
            while (!cancellationToken.IsCancellationRequested)
            {
                var retryPolicy = Policy.Handle<Exception>().WaitAndRetryForeverAsync((retryAttempt) =>
                {
                    var waitTimeSec = Math.Min(Math.Pow(2, retryAttempt - 1), 30 * 60);
                    logger.Debug("[{TenantName}] Calculated retry time for retry attempt #{RetryAttempty}: {Seconds} seconds", ctx.Tenant.TenantName, retryAttempt, waitTimeSec);
                    return TimeSpan.FromSeconds(waitTimeSec);
                });

                await retryPolicy.ExecuteAsync(async (cancellationToken) =>
                {
                    // this will never return except there is an exception
                    await EnsureLongPollingEndpointAndReplaceExistingAsync(ctx, onChatChanged, cancellationToken);
                }, cancellationToken);
            }
        }
    }
}
