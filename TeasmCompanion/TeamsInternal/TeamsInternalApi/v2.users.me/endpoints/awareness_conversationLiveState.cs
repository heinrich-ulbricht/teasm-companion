namespace TeasmCompanion.TeamsInternal.TeamsInternalApi.v2.users.me.endpoints
{
    public class awareness_conversationLiveState
    {
        public string conversationUrl { get; set; }
        public string conversationId { get; set; }
        public string groupCallInitiator { get; set; }
        public long expiration { get; set; }
        public string status { get; set; }
        public string conversationType { get; set; }
        public bool isHostless { get; set; }
        public Meetinginfo meetingInfo { get; set; }
        public string iCalUid { get; set; }
        public bool wasInitiatorInLobby { get; set; }
        public string exchangeId { get; set; }
    }

    public class Meetinginfo
    {
        public string organizerId { get; set; }
        public string tenantId { get; set; }
        public string replyChainMessageId { get; set; }
    }
}
