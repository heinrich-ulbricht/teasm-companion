using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TeasmCompanion.Registries;

#nullable enable

namespace TeasmCompanion.Interfaces
{
    public interface ITeamsUserRegistry
    {
        IObservable<(TeamsDataContext, ProcessedTeamsUser)> GetOnUserCreatedOrModified();
        /**
         * Register the display name for a user that was discovered at a certain time.
         * 
         * Note: Display names can come from different sources like messages, user API responses etc. 
         * Note: The time can aid in handling user name changes.
         */
        Task RegisterDisplayNameForUserIdAsync(TeamsDataContext ctx, TeamsParticipant userId, string? displayName, DateTime? dt);

        Task<ProcessedTeamsUser?> GetUserByIdAsync(TeamsDataContext ctx, TeamsParticipant userId, bool createDummyUserIfNotFound);
        Task<ProcessedTeamsUser> GetUserByIdOrDummyAsync(TeamsDataContext ctx, TeamsParticipant userId);

        /**
         * Replace all Teams user IDs with their display names.
         */
        Task<string> ReplaceUserIdsWithDisplayNamesAsync(TeamsDataContext ctx, string? stringWithUserIds);

        /**
         * Get display name for the given Teams user ID.
         * 
         * Note: if the user cannot be found a placeholder must be returned, like "Unknown user (userId)"
         */
        Task<string> GetDisplayNameForUserIdAsync(TeamsDataContext ctx, TeamsParticipant userId);

        /**
         * Recognize the existence of the given Teams user ID.
         */
        Task RecognizeUserIdAsync(TeamsDataContext ctx, TeamsParticipant userId, ProcessedTeamsUser? existingUser = null);

        /**
         * Get all currently Teams users known for the given context.
         */
        Task<IEnumerable<ProcessedTeamsUser>> GetTenantUsersAsync(TeamsDataContext ctx);
        Task MarkUserAsChanged(TeamsDataContext ctx, ProcessedTeamsUser changedUser);
    }
}
