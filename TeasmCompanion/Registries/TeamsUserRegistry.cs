using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Akavache;
using Serilog;
using TeasmCompanion.Interfaces;
using TeasmCompanion.Misc;
using TeasmCompanion.Registries;

#nullable enable

namespace TeasmCompanion
{
    /// <summary>
    /// This user registry implements user storage and retrieval. 
    /// 
    /// User info will be retrieved from the Teams API, from the (slow) user store or from a local cache to optimize things.
    /// User info will be stored in a given (slow) user store and in local cache.
    /// </summary>
    public class TeamsUserRegistry : ITeamsUserRegistry
    {
        private ConcurrentDictionary<TeamsParticipant, ProcessedTeamsUser>? globalUserLookupList;
        public ConcurrentBag<TeamsParticipant> GlobalUsersNotFound = new ConcurrentBag<TeamsParticipant>();
        private Subject<(TeamsDataContext, ProcessedTeamsUser)> onUserCreatedOrModified = new Subject<(TeamsDataContext, ProcessedTeamsUser)>();
        private readonly ITeamsUserStore userStore;
        private ILogger logger;

        public TeamsUserRegistry(ILogger logger, ITeamsUserStore userStore)
        {
            this.userStore = userStore;
            this.logger = logger.ForContext<TeamsUserRegistry>();
            userStore.SubscribeOnUserCreatedOrModified(onUserCreatedOrModified);
        }

        public IObservable<(TeamsDataContext, ProcessedTeamsUser)> GetOnUserCreatedOrModified() => onUserCreatedOrModified;

        private async Task<ConcurrentDictionary<TeamsParticipant, ProcessedTeamsUser>> EnsureGlobalUserLookupListInitializedAsync(TeamsDataContext ctx)
        {
            if (globalUserLookupList == null)
            {
                globalUserLookupList = await BlobCache.UserAccount.GetOrFetchObject("ts.globalusers", async () =>
                {
                    var result = new ConcurrentDictionary<TeamsParticipant, ProcessedTeamsUser>();
                    var users = await userStore.RetrieveUsersAsync(ctx);
                    foreach (var u in users)
                    {
                        result.TryAdd(u.UserId, u);
                    }
                    return result;
                });
            }
            return globalUserLookupList;
        }

        public async Task<IEnumerable<ProcessedTeamsUser>> GetTenantUsersAsync(TeamsDataContext ctx)
        {
            var lookupList = await EnsureGlobalUserLookupListInitializedAsync(ctx);
            return lookupList.Values.Where(value => value.IsFromTenant(ctx));
        }


        public async Task<string> GetDisplayNameForUserIdAsync(TeamsDataContext ctx, TeamsParticipant userId)
        {
            var lookupList = await EnsureGlobalUserLookupListInitializedAsync(ctx);
            if (!userId.IsValid)
            {
                return GetUnknownUserString(ctx, userId);
            }

            if (lookupList.TryGetValue(userId, out var user))
            {
                return user.DisplayName;
            } else 
            {
                return GetUnknownUserString(ctx, userId);
            }
        }

        private ProcessedTeamsUser? GetUserById(ConcurrentDictionary<TeamsParticipant, ProcessedTeamsUser> lookupList, TeamsDataContext ctx, TeamsParticipant userId, bool createDummyUserIfNotFound)
        {
            if (lookupList.TryGetValue(userId, out var user))
            {
                return user;
            }
            else
            {
                return createDummyUserIfNotFound ? new ProcessedTeamsUser(ctx, userId) : null;
            }
        }

        public async Task<ProcessedTeamsUser?> GetUserByIdAsync(TeamsDataContext ctx, TeamsParticipant userId, bool createDummyUserIfNotFound)
        {
            var lookupList = await EnsureGlobalUserLookupListInitializedAsync(ctx);
            return GetUserById(lookupList, ctx, userId, createDummyUserIfNotFound);
        }

        public async Task<ProcessedTeamsUser> GetUserByIdOrDummyAsync(TeamsDataContext ctx, TeamsParticipant userId)
        {
            var lookupList = await EnsureGlobalUserLookupListInitializedAsync(ctx);
#pragma warning disable CS8603 // Possible null reference return.
            return GetUserById(lookupList, ctx, userId, true);
#pragma warning restore CS8603 // Possible null reference return.
        }

        public async Task MarkUserAsChanged(TeamsDataContext ctx, ProcessedTeamsUser changedUser)
        {
            logger.Debug("[{TenantName}] Marking user '{UserName}' as changed", ctx.Tenant.TenantName, changedUser.DisplayName);
            var lookupList = await EnsureGlobalUserLookupListInitializedAsync(ctx);
            if (lookupList.TryGetValue(changedUser.UserId, out var user))
            {
                // somebody might create ProcessedTeamsUsers without informing the user registry... this is not supported and we should take measures against this; later...
                Debug.Assert(changedUser == user);
                onUserCreatedOrModified.OnNext((ctx, user));
            } else {
                // markind non-existing user as changed? well... register instead
                await RecognizeUserIdAsync(ctx, changedUser.UserId, changedUser);
            }
        }

        public async Task RegisterDisplayNameForUserIdAsync(TeamsDataContext ctx, TeamsParticipant userId, string? displayName, DateTime? dt)
        {
            if (!userId.IsValid || string.IsNullOrWhiteSpace(displayName))
                return;

            if (userId.Kind == ParticipantKind.TeamsChat)
            {
                // don't store chat ids
                return;
            }

            var lookupList = await EnsureGlobalUserLookupListInitializedAsync(ctx);
            var dtValue = dt ?? DateTime.Now;

            if (lookupList.TryGetValue(userId, out var user))
            {
                if (user.RegisterAlternateDisplayName(displayName, dtValue))
                {
                    onUserCreatedOrModified.OnNext((ctx, user));
                }
            }
            else
            {
                var newUser = new ProcessedTeamsUser(ctx, userId);
                newUser.RegisterAlternateDisplayName(displayName, dtValue);
                if (lookupList.TryAdd(userId, newUser))
                {
                    onUserCreatedOrModified.OnNext((ctx, newUser));
                    await SaveCacheAsync(ctx);
                }
            }
        }

        public static string GetUnknownUserString(TeamsDataContext ctx, TeamsParticipant userId)
        {
            var dummyUser = new ProcessedTeamsUser(ctx, userId);
            return dummyUser.DisplayName;
        }

        public async Task<string> ReplaceUserIdsWithDisplayNamesAsync(TeamsDataContext ctx, string? stringWithUserIds)
        {
            if (string.IsNullOrWhiteSpace(stringWithUserIds))
                return "";

            var lookupList = await EnsureGlobalUserLookupListInitializedAsync(ctx);
            // first replace any chat/meeting ID that participated in the conversation
            var result = Regex.Replace(stringWithUserIds, TeamsParticipant.ChatIdPattern, m =>
            {
                return Constants.MicrosoftTeamsChatSenderName;
            });
            // now that the chat ids are out of the way: replace mris
            result = Regex.Replace(result, TeamsParticipant.MriPatternOpen, m =>
            {
                var userId = (TeamsParticipant)m.Groups[0].Value;
                var user = GetUserById(lookupList, ctx, userId, false);
#pragma warning disable CS8603 // Possible null reference return.
                return user?.DisplayName ?? userId;
#pragma warning restore CS8603 // Possible null reference return.
            });
            return result;
        }
        
        public async Task RecognizeUserIdAsync(TeamsDataContext ctx, TeamsParticipant userId, ProcessedTeamsUser? existingUser = null)
        {
            if (userId.Kind == ParticipantKind.TeamsChat)
            {
                // don't store chat ids
                return;
            }
            // sanity check
            if (!existingUser?.UserId.Equals(userId) ?? false)
            {
                // this should never happen
                logger.Warning("[{TenantName}] Tried to register and existing user for a user ID that is not the user's.", ctx.Tenant.TenantName);
                Debugger.Break();
                existingUser = null;
            }
            var lookupList = await EnsureGlobalUserLookupListInitializedAsync(ctx);
            if (!lookupList.ContainsKey(userId))
            {
                var newUser = existingUser ?? new ProcessedTeamsUser(ctx, userId);
                if (lookupList.TryAdd(userId, newUser))
                {
                    onUserCreatedOrModified.OnNext((ctx, newUser));
                    await SaveCacheAsync(ctx);
                }
            } else if (existingUser != null)
            {
                logger.Warning("[{TenantName}] Tried to register a user object '{@ExistingUser}' while there does already one exist in the user registry. Needs to be analyzed.", ctx.Tenant.TenantName, existingUser);
                Debugger.Break();
                await RegisterDisplayNameForUserIdAsync(ctx, existingUser.UserId, existingUser.DisplayName, Utils.JavaScriptUtcMsToDateTime(existingUser.DiscoveryTime));
            }
        }

        public async Task SaveCacheAsync(TeamsDataContext ctx)
        {
            await EnsureGlobalUserLookupListInitializedAsync(ctx);
            await BlobCache.UserAccount.InsertObject("ts.globalusers", globalUserLookupList);
        }
    }
}
