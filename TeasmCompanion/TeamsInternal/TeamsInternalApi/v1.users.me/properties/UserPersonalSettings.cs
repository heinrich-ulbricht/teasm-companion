namespace TeasmCompanion.TeamsInternal.TeamsInternalApi.v1.users.me.properties
{

    public class UserPersonalSettings
    {
        public Emailsettings emailSettings { get; set; }
        public Toastsettings toastSettings { get; set; }
        public Activitygenerationsettings activityGenerationSettings { get; set; }
        public Soundssettings soundsSettings { get; set; }
        public Sfbsettings sfbSettings { get; set; }
        public Chatsettings chatSettings { get; set; }
        public Muteunmutepostsettings muteUnmutePostSettings { get; set; }
        public Closedcaptionssettings closedCaptionsSettings { get; set; }
    }

    public class Emailsettings
    {
        public string EmailNotificationDelay { get; set; }
        public bool Mention_Person { get; set; }
        public bool Mention_Channel { get; set; }
        public bool Mention_Team { get; set; }
        public bool Like { get; set; }
        public bool Reaction { get; set; }
        public bool Reaction_Angry { get; set; }
        public bool Reaction_Heart { get; set; }
        public bool Reaction_Laugh { get; set; }
        public bool Reaction_Like { get; set; }
        public bool Reaction_Sad { get; set; }
        public bool Reaction_Surprised { get; set; }
        public bool Reply { get; set; }
        public bool ReplyToReply { get; set; }
        public bool Chat { get; set; }
        public bool TeamMembershipChange_AddedToTeam { get; set; }
        public bool TeamMembershipChange_PromotedToTeamAdmin { get; set; }
        public bool Follow_ChannelNewMessage { get; set; }
        public bool Follow_ChannelReplyMessage { get; set; }
        public bool Inferred { get; set; }
        public bool DLP_NotifySender { get; set; }
        public bool Trending { get; set; }
        public bool Priority { get; set; }
        public bool TeamMembershipChange_PendingOnBehalfOfJoinRequest { get; set; }
        public bool TeamMembershipChange_PendingSelfJoinRequest { get; set; }
        public bool MentionInChat_Person { get; set; }
        public bool LikeInChat { get; set; }
        public bool Visible_ChannelNewMessage { get; set; }
        public bool Visible_ChannelReplyMessage { get; set; }
        public bool Visible_ChannelMention { get; set; }
        public string EmailDigestDelay { get; set; }
        public bool CallTransferFailed { get; set; }
        public bool MessageSendFailed { get; set; }
        public bool Error { get; set; }
        public bool PresenceStatus { get; set; }
        public bool Download_DownloadStarted { get; set; }
        public bool Download_Downloaded { get; set; }
        public bool Download_DownloadFailed { get; set; }
        public bool MeetingWentLiveNotification { get; set; }
        public bool ThirdParty_Bot { get; set; }
        public bool ProjectBoardAssignments { get; set; }
        public bool ReplyToPolls { get; set; }
        public bool TeamMembershipChange_PendingOnBehalfOfJoinRequestMultiple { get; set; }
    }

    public class Toastsettings
    {
        public bool Mention_Person { get; set; }
        public bool Mention_Channel { get; set; }
        public bool Mention_Team { get; set; }
        public bool Like { get; set; }
        public bool Reaction { get; set; }
        public bool Reaction_Angry { get; set; }
        public bool Reaction_Heart { get; set; }
        public bool Reaction_Laugh { get; set; }
        public bool Reaction_Like { get; set; }
        public bool Reaction_Sad { get; set; }
        public bool Reaction_Surprised { get; set; }
        public bool Reply { get; set; }
        public bool ReplyToReply { get; set; }
        public bool Chat { get; set; }
        public bool TeamMembershipChange_AddedToTeam { get; set; }
        public bool TeamMembershipChange_PromotedToTeamAdmin { get; set; }
        public bool Download_DownloadStarted { get; set; }
        public bool Download_Downloaded { get; set; }
        public bool Download_DownloadFailed { get; set; }
        public bool MeetingWentLiveNotification { get; set; }
        public bool ThirdParty_Bot { get; set; }
        public bool Follow_ChannelNewMessage { get; set; }
        public bool Follow_ChannelReplyMessage { get; set; }
        public bool MessageSendFailed { get; set; }
        public bool Inferred { get; set; }
        public bool Trending { get; set; }
        public bool DLP_NotifySender { get; set; }
        public bool Priority { get; set; }
        public bool CallTransferFailed { get; set; }
        public bool Error { get; set; }
        public bool PresenceStatus { get; set; }
        public bool TeamMembershipChange_PendingOnBehalfOfJoinRequest { get; set; }
        public bool TeamMembershipChange_PendingSelfJoinRequest { get; set; }
        public bool MentionInChat_Person { get; set; }
        public bool LikeInChat { get; set; }
        public bool Visible_ChannelNewMessage { get; set; }
        public bool Visible_ChannelReplyMessage { get; set; }
        public bool Visible_ChannelMention { get; set; }
        public bool TeamsEngagementActivity_WelcomeNewUser { get; set; }
        public bool ProjectBoardAssignments { get; set; }
        public bool ReplyToPolls { get; set; }
        public bool ShowPreviewInToasts { get; set; }
        public bool popOutChat { get; set; }
        public bool TeamMembershipChange_PendingOnBehalfOfJoinRequestMultiple { get; set; }
    }

    public class Activitygenerationsettings
    {
        public bool Mention_Person { get; set; }
        public bool Mention_Channel { get; set; }
        public bool Mention_Team { get; set; }
        public bool Like { get; set; }
        public bool Reaction { get; set; }
        public bool Reaction_Angry { get; set; }
        public bool Reaction_Heart { get; set; }
        public bool Reaction_Laugh { get; set; }
        public bool Reaction_Like { get; set; }
        public bool Reaction_Sad { get; set; }
        public bool Reaction_Surprised { get; set; }
        public bool Reply { get; set; }
        public bool ReplyToReply { get; set; }
        public bool Chat { get; set; }
        public bool TeamMembershipChange_AddedToTeam { get; set; }
        public bool TeamMembershipChange_PromotedToTeamAdmin { get; set; }
        public bool Follow_ChannelNewMessage { get; set; }
        public bool Follow_ChannelReplyMessage { get; set; }
        public bool Inferred { get; set; }
        public bool Trending { get; set; }
        public bool DLP_NotifySender { get; set; }
        public bool Priority { get; set; }
        public bool TeamMembershipChange_PendingOnBehalfOfJoinRequest { get; set; }
        public bool TeamMembershipChange_PendingSelfJoinRequest { get; set; }
        public bool MentionInChat_Person { get; set; }
        public bool LikeInChat { get; set; }
        public bool Visible_ChannelNewMessage { get; set; }
        public bool Visible_ChannelReplyMessage { get; set; }
        public bool Visible_ChannelMention { get; set; }
        public bool CallTransferFailed { get; set; }
        public bool MessageSendFailed { get; set; }
        public bool Error { get; set; }
        public bool PresenceStatus { get; set; }
        public bool MeetingWentLiveNotification { get; set; }
        public bool ThirdParty_Bot { get; set; }
        public bool ProjectBoardAssignments { get; set; }
        public bool ReplyToPolls { get; set; }
        public bool Download_DownloadStarted { get; set; }
        public bool Download_Downloaded { get; set; }
        public bool Download_DownloadFailed { get; set; }
        public bool TeamMembershipChange_PendingOnBehalfOfJoinRequestMultiple { get; set; }
    }

    public class Soundssettings
    {
        public string alertSound { get; set; }
    }

    public class Sfbsettings
    {
        public bool enableSfBInterop { get; set; }
    }

    public class Chatsettings
    {
        public string meetingChatNotification { get; set; }
    }

    public class Muteunmutepostsettings
    {
        public bool turnOn { get; set; }
        public bool turnOff { get; set; }
    }

    public class Closedcaptionssettings
    {
        public int fontSize { get; set; }
    }
}

