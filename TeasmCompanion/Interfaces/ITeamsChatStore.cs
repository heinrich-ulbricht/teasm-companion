using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TeasmCompanion.Stores;

#nullable enable

namespace TeasmCompanion.Interfaces
{
    public interface ITeamsChatStore
    {
        Task<ProcessedChat?> GetChatMetadataAsync(TeamsDataContext ctx, bool forceChatIndexUpdate, string chatId);
        Task StoreMailThreadAndUpdateMetadataAsync(TeamsDataContext ctx, string title, ProcessedChat chat, IOrderedEnumerable<IChatMessage>? messages);
        /// <summary>
        /// Store a single chat message.
        /// </summary>
        /// <remarks>The corresponding chat folder must already exist.</remarks>
        /// <param name="ctx">Data context</param>
        /// <param name="message">The message</param>
        /// <returns>True if storage was successful, false if there was no corresponding chat folder found</returns>
        Task<bool> StoreSingleChatMessageAsync(TeamsDataContext ctx, IChatMessage message);
        Task<ConcurrentDictionary<string, IChatChangeInfo>> GetChatIndexAsync(TeamsDataContext ctx);
        Task VisitMissingUserDisplayNames(TeamsDataContext ctx, Func<IMutableChatMessage, Task<(bool, List<string>)>> visitor, CancellationToken cancellationToken = default);
        Task EnsureFoldersForTenantsExistAsync(IEnumerable<TeamsDataContext> contexts);
        Task<bool> CanAccessStore(CancellationToken cancellationToken = default);

        event Func<object, TeamsDataContext, Task> OnChatIndexChanged;
    }
}
