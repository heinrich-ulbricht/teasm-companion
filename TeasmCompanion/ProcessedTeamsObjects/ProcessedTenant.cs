using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using TeasmCompanion.Registries;
using TeasmCompanion.TeamsInternal.TeamsInternalApi.api.mt.emea.beta.users;

namespace TeasmCompanion.ProcessedTeamsObjects
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ProcessedTenant
    {
        [JsonProperty]
        private Tenant tenant;
        [JsonProperty]
        public DateTime DiscoveryTimeUtc { get; private set; }
        public string TenantId { get => tenant.tenantId; }
        // user ID within the TenantId tenant
        public TeamsParticipant UserId { get => (TeamsParticipant)tenant.userId; }
        public string UserType { get => tenant.userType; }
        public string TenantName { get => tenant.tenantName; }

        public ProcessedTenant(Tenant tenant, DateTime discoveryTimeUtc)
        {
            this.tenant = tenant;
            DiscoveryTimeUtc = discoveryTimeUtc;
        }
    }
}
