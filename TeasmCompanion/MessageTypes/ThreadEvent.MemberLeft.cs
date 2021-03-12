using System.Collections.Generic;

namespace TeasmCompanion.MessageTypes
{

    public class ThreadEventMemberLeft
    {
        public long eventtime { get; set; }
        public string initiator { get; set; }
        public List<JoinedMember> members { get; set; }
    }
}
