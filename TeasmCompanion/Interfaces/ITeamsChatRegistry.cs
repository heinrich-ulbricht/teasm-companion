using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TeasmCompanion.Registries;
using TeasmCompanion.TeamsInternal.TeamsInternalApi.api.csa.api.v1.teams.users;

#nullable enable

namespace TeasmCompanion.Interfaces
{
    public interface ITeamsChatRegistry
    {
        Task<MyChatsAndTeams?> GetAllChatsAndTeamsAsync(TeamsDataContext ctx);
        Task<MyChatsAndTeams?> GetUpdatedChatsAndTeamsAsync(TeamsDataContext ctx);

        Task<ProcessedChat?> GetStoredProcessedChatAsync(TeamsDataContext ctx, bool forceChatIndexUpdate, string chatId);

        /// <summary>
        /// Retrieve all messages for chat.
        /// </summary>
        /// <param name="ctx">Data context</param>
        /// <param name="chat">Chat to retrieve</param>
        /// <returns>Retrieved chat and the number of retrieved messages</returns>
        Task<(ProcessedChat, long)> RetrieveProcessedChatWithAllMessagesAsync(TeamsDataContext ctx, Chat oldChat);
        /// <summary>
        /// Retrieve only new messages for chat.
        /// </summary>
        /// <param name="ctx">Data context</param>
        /// <param name="oldChat">Old chat to retrieve update for</param>
        /// <returns>Retrieved chat and the number of retrieved new messages</returns>
        Task<(ProcessedChat, long)> RetrieveProcessedChatWithOnlyNewMessagesAsync(TeamsDataContext ctx, IChatChangeInfo oldChat, Chat newChat);

        /// <summary>
        /// Retrieve the neccessary amount of messages, either all, updated or none.
        /// </summary>
        /// <param name="ctx">Data context</param>
        /// <param name="chat">Chate to retrieve</param>
        /// <returns>Result of operation, information about the updated chat and the number of newly retrieved messages</returns>
        Task<(ChatRetrievalResult, IChatChangeInfo?, long)> RetrieveChatAndMessagesIfNeededAsync(TeamsDataContext ctx, Chat chat);

        Task StoreMailThreadAsyncAndUpdateMetadataAsync(TeamsDataContext ctx, string title, ProcessedChat chat, IOrderedEnumerable<IChatMessage> messages);
        // see ITeamsChatStore documentation
        Task<bool> StoreSingleChatMessageAsync(TeamsDataContext ctx, IChatMessage message);

        Task VisitMissingUserDisplayNames(TeamsDataContext ctx, Func<IMutableChatMessage, Task<(bool, List<string>)>> visitor, CancellationToken cancellationToken = default);
    }
}
