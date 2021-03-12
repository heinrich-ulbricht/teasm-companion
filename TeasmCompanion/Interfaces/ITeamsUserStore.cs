using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace TeasmCompanion.Interfaces
{
    public interface ITeamsUserStore
    {
        Task<List<ProcessedTeamsUser>> RetrieveUsersAsync(TeamsDataContext ctx);
        Task PersistUserAsync(TeamsDataContext ctx, ProcessedTeamsUser user);
        Task PersistUsersAsync(TeamsDataContext ctx, IEnumerable<ProcessedTeamsUser> users);
        void SubscribeOnUserCreatedOrModified(IObservable<(TeamsDataContext, ProcessedTeamsUser)>? onUserCreatedOrModified);
        Task<bool> CanAccessStore(CancellationToken cancellationToken = default);
    }
}
