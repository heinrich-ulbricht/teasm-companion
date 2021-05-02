namespace TeasmCompanion.TeamsInternal.TeamsInternalApi.v2.users.me.endpoints
{
    public class awareness_conversationLiveState
    {
        // flightproxy URL like "https://api.flightproxy.teams.microsoft.com/api/v2/ep/conv-euwe-07.conv.skype.com/conv/<ID>"
        public string conversationUrl { get; set; }
        // a GUID
        public string conversationId { get; set; }
        public string groupCallInitiator { get; set; }
        public long expiration { get; set; }
        // "Active", ...
        public string status { get; set; }
        public string conversationType { get; set; }
        public bool isHostless { get; set; }
        public Meetinginfo meetingInfo { get; set; }
        public string iCalUid { get; set; }
        public bool wasInitiatorInLobby { get; set; }
        public string exchangeId { get; set; }
        // unknown structure, empty so far
        public object meetingData { get; set; }
    }

    public class Meetinginfo
    {
        public string organizerId { get; set; }
        public string tenantId { get; set; }
        public string replyChainMessageId { get; set; }
    }
}
