using System.Collections.Generic;

namespace TeasmCompanion.MessageTypes
{

    public class ThreadActivityDeleteMemberWrapper
    {
        public ThreadActivityDeleteMember root { get; set; }
    }

    public class ThreadActivityDeleteMember
    {
        public Deletemember deletemember { get; set; }
    }

    public class Deletemember
    {
        public string eventtime { get; set; }
        // mri; can also be app -> e.g. "28:app:00000000-0000-beef-0000-000000000000_00000000-0000-feeb-0000-000000000000"
        public string initiator { get; set; }
        public List<string> target { get; set; }
        public string rosterVersion { get; set; }
        public string lastRosterVersion { get; set; }
        public List<Detailedtargetinfo> detailedtargetinfo { get; set; }
        public Detailedinitiatorinfo detailedinitiatorinfo { get; set; }
    }

    public class Detailedinitiatorinfo
    {
        public string friendlyName { get; set; }
    }
}
