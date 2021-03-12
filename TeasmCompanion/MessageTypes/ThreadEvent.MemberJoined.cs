using System.Collections.Generic;

namespace TeasmCompanion.MessageTypes
{
    public class ThreadEventMemberJoined
    {
        public long eventtime { get; set; }
        public string initiator { get; set; }
        public List<JoinedMember> members { get; set; }
    }

    public class JoinedMember
    {
        public string id { get; set; }
        public string friendlyname { get; set; }
    }
}
