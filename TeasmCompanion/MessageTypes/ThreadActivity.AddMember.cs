using System.Collections.Generic;

namespace TeasmCompanion.MessageTypes
{

    public class ThreadActivityAddMemberWrapper
	{
		public ThreadActivityAddMember root { get; set; }
	}

	public class ThreadActivityAddMember
	{
		public Addmember addmember { get; set; }
	}

	public class Addmember
	{
		public string eventtime { get; set; }
		public string initiator { get; set; }
		public string rosterVersion { get; set; }
		public string lastRosterVersion { get; set; }
		public List<string> target { get; set; }
		public List<Detailedtargetinfo> detailedtargetinfo { get; set; }
		public Detailedinitiatorinfo detailedinitiatorinfo { get; set; }
	}

	public class Detailedtargetinfo
	{
		public string id { get; set; }
		public string friendlyName { get; set; }
		// "0"
		public string sharehistorytime { get; set; }
		// "temp"
		public string meetingMemberType { get; set; }
	}

}
