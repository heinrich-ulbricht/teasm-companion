using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using TeasmCompanion.ProcessedTeamsObjects;
using TeasmCompanion.Registries;

#nullable enable

namespace TeasmCompanion
{
    /**
     * Terminology:
     * * main account = user account in the main tenant, i.e. the corporation the user is member of (internal, non B2B)
     * * guest user/account = user account in other tenants a user has been invited to as guest
     * * TBD: Azure B2B accounts ?? https://docs.microsoft.com/en-us/azure/active-directory/external-identities/invite-internal-users
     * * TBD: what about non-corporate user accounts like Outlook and their main user account id?
     * 
     * Tokens for retrieving Teams data exist in the context of a tenant and a user ID for this tenant. Each user
     * is member of (assumption!) exactly one "main" tenant and multiple guest tenants. So for each member user ID there
     * are multiple guest user IDs for other tenants. For retrieving data in one tenant the user ID of this tenant and 
     * the accompanied tokens are relevant. Furthermore we save certain data in the context of the main tenant user ID and 
     * thus need this ID as well.
     * 
     * Docs:
     * * guest access: https://docs.microsoft.com/de-de/microsoftteams/guest-access
     */
    [JsonObject(MemberSerialization.OptIn)]
    public struct TeamsDataContext
    {
        private TeamsParticipant _mainUserId;
        // the main tenant user
        [JsonProperty]
        [DisallowNull]
        public TeamsParticipant MainUserId 
        {
            get => _mainUserId;
            private set
            {
                if (!value.IsValid)
                {
                    //Debugger.Break();
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _mainUserId = value;
            }
        }
        [JsonProperty]
        public ProcessedTenant Tenant { get; private set; }

        [JsonConstructor]
        public TeamsDataContext(TeamsParticipant mainUserId, ProcessedTenant tenant) : this()
        {
            this.MainUserId = mainUserId;
            this.Tenant = tenant;
        }

        public bool IsTenant(string tenantId)
        {
            if (Tenant?.TenantId == null)
                return false;

            return (Tenant.TenantId.ToLowerInvariant().Equals(tenantId?.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase));
        }

        public TeamsDataContext CheckTenantIdSet()
        {
            if (string.IsNullOrWhiteSpace(Tenant?.TenantId))
            {
                throw new ArgumentNullException("Tenant ID");
            }
            return this;
        }

        public TeamsDataContext CheckTenantUserIdSet()
        {
            if (!Tenant?.UserId.IsValid ?? true)
            {
                throw new ArgumentNullException("Tenant User ID");
            }
            return this;
        }

        public override bool Equals(object? obj)
        {
            if (!(obj is TeamsDataContext))
            {
                return false;
            }

            var otherContext = (TeamsDataContext)obj;
            return
                otherContext.MainUserId == MainUserId
                && otherContext.Tenant?.TenantId == Tenant?.TenantId
                && otherContext.Tenant?.UserId == Tenant?.UserId;
        }

        public override int GetHashCode()
        {
            return $"{MainUserId}{Tenant?.TenantId ?? "null"}{Tenant?.UserId ?? "null"}".GetHashCode();
        }

        public override string ToString()
        {
            return $"'{Tenant.TenantName}' | User {Tenant.UserId} ({Tenant.UserType})";
        }
    }
}
