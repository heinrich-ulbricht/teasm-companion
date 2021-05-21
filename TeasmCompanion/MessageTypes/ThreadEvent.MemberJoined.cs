using System.Collections.Generic;

namespace TeasmCompanion.MessageTypes
{
    public class ThreadEventMemberJoined
    {
        public long eventtime { get; set; }
        // mri
        public string initiator { get; set; }
        public List<JoinedMember> members { get; set; }
    }

    public class JoinedMember
    {
        // mri
        public string id { get; set; }
        public string friendlyname { get; set; }
        // "temp"
        public string meetingMemberType { get; set; }
    }
}
