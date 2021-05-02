using Serilog;
using System.Linq;
using TeasmCompanion.TeamsInternal.TeamsInternalApi.api.csa.api.v1.teams.users;

#nullable enable

namespace TeasmCompanion
{
    public class Experimental
    {
        public static void CheckForActiveMeeting(ILogger logger, TeamsDataContext ctx, MyChatsAndTeams? myChatsAndTeams)
        {
            var activeMeetups = myChatsAndTeams?.chats?.Where(c => !string.IsNullOrEmpty(c?.activeMeetup?.conversationUrl));
            if (activeMeetups != null && activeMeetups.Any())
            {
                logger.Information("[{TenantName}] Found active meetup! {@Meetups}", ctx.Tenant.TenantName, activeMeetups.Select(m => m.title));
            }
        }
    }
}
