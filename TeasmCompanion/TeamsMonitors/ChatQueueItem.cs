using TeasmCompanion.TeamsInternal.TeamsInternalApi.api.csa.api.v1.teams.users;

#nullable enable

namespace TeasmCompanion.TeamsMonitors
{
    public class ChatQueueItem
    {
        public TeamsDataContext ctx;
        public HigherVersionWinsComparerChat? chat;

        public bool Equals(ChatQueueItem? other)
        {
            return chat?.Equals(other?.chat) ?? false;
        }

        public override bool Equals(object? obj)
        {
            if (!(obj is ChatQueueItem))
                return base.Equals(obj);

            var other = (ChatQueueItem?)obj;
            if (other?.chat?.Chat?.id == null)
                return false;

            return other.chat.Chat?.id == chat?.Chat?.id;
        }

        public override int GetHashCode()
        {
            return chat?.Chat?.id?.GetHashCode() ?? 0;
        }

        public ChatQueueItem(TeamsDataContext ctx, HigherVersionWinsComparerChat? chat)
        {
            this.ctx = ctx;
            this.chat = chat;
        }
    }
}
