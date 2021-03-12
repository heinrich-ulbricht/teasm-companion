using TeasmCompanion.Registries;
using TeasmCompanion.TeamsInternal.TeamsInternalApi.v1.users.me.properties;

namespace TeasmCompanion.ProcessedTeamsObjects
{
    public class ProcessedUser
    {
        public TeamsParticipant UserId { get; private set; }
        public MyProperties Properties { get; private set; }

        public ProcessedUser(TeamsParticipant userId, MyProperties props)
        {
            Properties = props;
            UserId = userId;
        }
    }
}
