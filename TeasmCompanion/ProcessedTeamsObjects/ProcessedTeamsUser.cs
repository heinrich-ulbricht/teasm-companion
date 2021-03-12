using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TeasmCompanion.Misc;
using TeasmCompanion.Registries;
using TeasmCompanion.TeamsInternal.TeamsInternalApi.api.mt.emea.beta.users;

#nullable enable

namespace TeasmCompanion
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ProcessedTeamsUser
    {
        public enum TeamsUserState 
        {
            Undefined,
            FoundInTenant,
            MissingFromTenant
        }

        [JsonProperty]
        public TeamsParticipant UserId { get; set; }
        [JsonProperty]
        public TeamsDataContext DataContext { get; set; }
        [JsonProperty]
        public TeamsUserState State { get; set; } = TeamsUserState.Undefined;
        [JsonProperty]
        public TeamsUser? OriginalUser { get; set; }

        [JsonProperty]
        private SortedList<DateTime, string> alternateDisplayNames = new SortedList<DateTime, string>();
        [JsonProperty]
        private SortedList<DateTime, string> alternateEmailAddresses = new SortedList<DateTime, string>();

        [JsonProperty]
        public long DiscoveryTime { get; set; }

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(OriginalUser?.displayName))
                {
#pragma warning disable CS8603 // Possible null reference return.
                    return OriginalUser?.displayName;
#pragma warning restore CS8603 // Possible null reference return.
                }
                if (alternateDisplayNames.Count > 0)
                {
                    return alternateDisplayNames.Last().Value;
                }
                if (KnownBots.KnownBotNames.TryGetValue(UserId, out var botName))
                {
                    return botName;
                }

                return GetUserNamePlaceholder(); 
            }
        }

        public bool HasDisplayName
        {
            get
            {
                return DisplayName != GetUserNamePlaceholder();
            }
        }

        public string EmailAddress
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(OriginalUser?.email))
                {
#pragma warning disable CS8603 // Possible null reference return.
                    return OriginalUser?.email;
#pragma warning restore CS8603 // Possible null reference return.
                }
                else if (alternateEmailAddresses.Count > 0)
                {
                    return alternateEmailAddresses.Last().Value;
                }

                return GetEmailAddressPlaceholder();
            }
        }

        public bool HasEmailAddress
        {
            get
            {
                return true;
            }

        }

        [JsonConstructor]
        private ProcessedTeamsUser()
        {
        }

        public ProcessedTeamsUser(TeamsDataContext dataContext, TeamsParticipant userId, TeamsUser? originalUser, TeamsUserState state)
        {
            OriginalUser = originalUser;
            UserId = userId;
            DataContext = dataContext;
            State = state;
        }

        public ProcessedTeamsUser(TeamsDataContext ctx, TeamsParticipant userId)
        {
            UserId = userId;
            DataContext = ctx;
        }

        /// <summary>
        /// Get a placeholder for unknown users.
        /// 
        /// Note: the placeholder is constructed such that it can be identified and replaced with a display name in case one becomes available later.
        /// </summary>
        /// <returns>Placeholder string for unknown users</returns>
        private string GetUserNamePlaceholder()
        {
            if (UserId.Kind == ParticipantKind.AppOrBot)
            {
                if (UserId != TeamsParticipant.Null)
                    return $"Bot {{{{{UserId}}}}}"; else
                    return Constants.UnknownBotDisplayName;
            }
            if (UserId != TeamsParticipant.Null)
                return $"User {{{{{UserId}}}}}"; else
                return Constants.UnknownUserDisplayName;
        }

        private string GetEmailAddressPlaceholder()
        {
            return $"unknown_{DataContext.Tenant.TenantId}_{UserId}@{Constants.AppDomain}";
        }

        // return true if changed
        private bool RegisterAlternateValue(string? newAlternateValue, DateTime dt, Func<SortedList<DateTime, string>> getAlternateValueList)
        {
            newAlternateValue = newAlternateValue?.Trim();
            if (string.IsNullOrEmpty(newAlternateValue))
                return false;

            var userNameAsId = (TeamsParticipant)newAlternateValue;
            // don't accept mri as alternate value
            if (userNameAsId.IsValid)
                return false;

            // check for MRI-like-names which were observed for a brief period of time (like "orgid:00000000-0000-beef-0000-000000000000"... as display name...)
            if (Regex.IsMatch(newAlternateValue, TeamsParticipant.GuidPattern))
                return false;

            var alternateValueList = getAlternateValueList();

            // already discovered a name at exactly the same time? bad luck; ignore this
            if (alternateValueList.ContainsKey(dt))
                return false;

            // already know this name, skip or update timestamp
            if (alternateValueList.Where(value => value.Value.Equals(newAlternateValue, StringComparison.InvariantCultureIgnoreCase)).Any())
            {
                var existingEntry = alternateValueList.Where(value => value.Value.Equals(newAlternateValue, StringComparison.InvariantCultureIgnoreCase)).First();
                if (existingEntry.Key < dt)
                {
                    // update timestamp of entry
                    alternateValueList.Remove(existingEntry.Key);
                    alternateValueList.Add(dt, newAlternateValue);
                    return true;
                }
                else
                {
                    // ignore older timestamp for this particular name
                    return false;
                }
            }

            alternateValueList.Add(dt, newAlternateValue);
            return true;
        }

        public bool RegisterAlternateDisplayName(string? alternateDisplayName, DateTime dt)
        {
            return RegisterAlternateValue(alternateDisplayName, dt, () => alternateDisplayNames);
        }

        public bool RegisterAlternateEmailAddress(string? alternateEmailAddress, DateTime dt)
        {
            return RegisterAlternateValue(alternateEmailAddress, dt, () => alternateEmailAddresses);
        }

        public bool IsFromTenant(string tenantId)
        {
            return DataContext.IsTenant(tenantId);
        }

        public bool IsFromTenant(TeamsDataContext ctx)
        {
            return IsFromTenant(ctx.Tenant.TenantId);
        }

        public ProcessedTeamsUser RegisterOriginalUser(TeamsUser originalUser)
        {
            OriginalUser = originalUser;
            return this;
        }

        public static bool operator <(ProcessedTeamsUser l, ProcessedTeamsUser f)
        {
            return l.DiscoveryTime < f.DiscoveryTime;
        }
        public static bool operator >(ProcessedTeamsUser l, ProcessedTeamsUser f)
        {
            return l.DiscoveryTime > f.DiscoveryTime;
        }
    }
}
