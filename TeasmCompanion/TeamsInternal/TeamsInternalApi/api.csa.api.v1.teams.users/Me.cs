using System;
using System.Collections.Generic;
using System.Numerics;
using TeasmCompanion.Interfaces;
using TeasmCompanion.Misc;
using TeasmCompanion.TeamsInternal.TeamsInternalApi._shared;
using TeasmCompanion.TeamsInternal.TeamsInternalApi.v1.users.me.conversations;

namespace TeasmCompanion.TeamsInternal.TeamsInternalApi.api.csa.api.v1.teams.users
{
    public class MyChatsAndTeams
    {
        public List<Team> teams { get; set; }
        public List<Chat> chats { get; set; }
        public List<object> users { get; set; } // TODO: get structure, null so far
        public List<Privatefeed> privateFeeds { get; set; }
        public Metadata metadata { get; set; }
        // note: type "Channel" here is a guess, need to verify
        public List<Channel> channels { get; set; }
    }

    public class Metadata
    {
        public string syncToken { get; set; }
        public bool isPartialData { get; set; }
    }

    public class Team
    {
        public string displayName { get; set; }
        public string id { get; set; }
        public List<Channel> channels { get; set; }
        public string pictureETag { get; set; }
        public string description { get; set; }
        public bool isFavorite { get; set; }
        public bool isCollapsed { get; set; }
        public bool isDeleted { get; set; }
        public bool isTenantWide { get; set; }
        public string smtpAddress { get; set; }
        public string threadVersion { get; set; }
        public string threadSchemaVersion { get; set; }
        public object conversationVersion { get; set; }
        public object classification { get; set; }
        public long accessType { get; set; }
        public string guestUsersCategory { get; set; }
        public bool dynamicMembership { get; set; }
        public bool maximumMemberLimitExceeded { get; set; }
        public Teamsettings teamSettings { get; set; }
        public Teamguestsettings teamGuestSettings { get; set; }
        public Teamstatus teamStatus { get; set; }
        public Teamsiteinformation teamSiteInformation { get; set; }
        public bool isCreator { get; set; }
        public string creator { get; set; }
        public long membershipVersion { get; set; }
        public Membershipsummary membershipSummary { get; set; }
        public bool isUserMuted { get; set; }
        public DateTime lastJoinAt { get; set; }
        public DateTime lastLeaveAt { get; set; }
        public long membershipExpiry { get; set; }
        public int memberRole { get; set; }
        public bool isFollowed { get; set; }
        public string tenantId { get; set; }
        public int teamType { get; set; }
        public Extensiondefinition extensionDefinition { get; set; }
        public bool isArchived { get; set; }
        public bool isTeamLocked { get; set; }
        public bool isUnlockMembershipSyncRequired { get; set; }
        public bool channelOnlyMember { get; set; }
        public Sensitivitylabel sensitivityLabel { get; set; }
        public List<Availablechannel> availableChannels { get; set; }
    }

    public class Availablechannel
    {
        public string id { get; set; }
        public string displayName { get; set; }
        // e.g. -1
        public long version { get; set; }
        // e.g. -1
        public long threadVersion { get; set; }
        public bool isDeleted { get; set; }
        public DateTime creationTime { get; set; }
        public bool isShared { get; set; }
    }

    public class Teamsettings
    {
        public bool createTopic { get; set; }
        public bool updateTopic { get; set; }
        public bool deleteTopic { get; set; }
        public bool createTab { get; set; }
        public bool deleteTab { get; set; }
        public bool createIntegration { get; set; }
        public bool updateIntegration { get; set; }
        public bool deleteIntegration { get; set; }
        public bool teamMention { get; set; }
        public bool channelMention { get; set; }
        public bool giphyEnabled { get; set; }
        public bool stickersEnabled { get; set; }
        public int giphyRating { get; set; }
        public bool customMemesEnabled { get; set; }
        public bool teamMemesEnabled { get; set; }
        public bool addDisplayContent { get; set; }
        public bool removeDisplayContent { get; set; }
        public bool adminDeleteEnabled { get; set; }
        public bool deleteEnabled { get; set; }
        public bool editEnabled { get; set; }
        public bool messageThreadingEnabled { get; set; }
        public int generalChannelPosting { get; set; }
        public bool installApp { get; set; }
        public bool uninstallApp { get; set; }
        public bool isPrivateChannelCreationEnabled { get; set; }
        public bool uploadCustomApp { get; set; }
    }

    public class Teamguestsettings
    {
        public bool createTopic { get; set; }
        public bool updateTopic { get; set; }
        public bool deleteTopic { get; set; }
        public bool createTab { get; set; }
        public bool deleteTab { get; set; }
        public bool createIntegration { get; set; }
        public bool updateIntegration { get; set; }
        public bool deleteIntegration { get; set; }
        public bool teamMention { get; set; }
        public bool channelMention { get; set; }
        public bool giphyEnabled { get; set; }
        public bool stickersEnabled { get; set; }
        public int giphyRating { get; set; }
        public bool customMemesEnabled { get; set; }
        public bool teamMemesEnabled { get; set; }
        public bool addDisplayContent { get; set; }
        public bool removeDisplayContent { get; set; }
        public bool adminDeleteEnabled { get; set; }
        public bool deleteEnabled { get; set; }
        public bool editEnabled { get; set; }
        public bool messageThreadingEnabled { get; set; }
        public int generalChannelPosting { get; set; }
        public bool installApp { get; set; }
        public bool uninstallApp { get; set; }
        public bool isPrivateChannelCreationEnabled { get; set; }
        public bool uploadCustomApp { get; set; }
    }

    public class Teamsiteinformation
    {
        public string groupId { get; set; }
        public string sharepointSiteUrl { get; set; }
        public bool isOneNoteProvisioned { get; set; }
        public string notebookId { get; set; }
    }

    public class Membershipsummary
    {
        public int botCount { get; set; }
        public int mutedMembersCount { get; set; }
        public int totalMemberCount { get; set; }
        public int adminRoleCount { get; set; }
        public int userRoleCount { get; set; }
        public int guestRoleCount { get; set; }
    }

    public class Extensiondefinition
    {
        public DateTime updatedTime { get; set; }
    }

    public class Sensitivitylabel
    {
        public string id { get; set; }
        public string name { get; set; }
        public string displayName { get; set; }
        public string toolTip { get; set; }
    }

    public class Channel
    {
        public string id { get; set; }
        public string displayName { get; set; }
        public Consumptionhorizon consumptionHorizon { get; set; }
        public Userconsumptionhorizon userConsumptionHorizon { get; set; }
        public object retentionHorizon { get; set; }
        public object retentionHorizonV2 { get; set; }
        public long version { get; set; }
        public long threadVersion { get; set; }
        public string threadSchemaVersion { get; set; }
        public string parentTeamId { get; set; }
        public bool isGeneral { get; set; }
        public bool isFavorite { get; set; }
        public bool isFollowed { get; set; }
        public bool isMember { get; set; }
        public string creator { get; set; }
        public bool isMessageRead { get; set; }
        public bool isImportantMessageRead { get; set; }
        public bool isGapDetectionEnabled { get; set; }
        public Defaultfilesettings defaultFileSettings { get; set; }
        public List<Tab> tabs { get; set; }
        public List<Connectorprofile> connectorProfiles { get; set; }
        public Message lastMessage { get; set; }
        public DateTime lastImportantMessageTime { get; set; }
        public bool isDeleted { get; set; }
        public bool isPinned { get; set; }
        public DateTime lastJoinAt { get; set; }
        public DateTime lastLeaveAt { get; set; }
        public int memberRole { get; set; }
        public bool isMuted { get; set; }
        public long membershipExpiry { get; set; }
        public bool isFavoriteByDefault { get; set; }
        public DateTime creationTime { get; set; }
        public bool isArchived { get; set; }
        public int channelType { get; set; }
        public long membershipVersion { get; set; }
        public Membershipsummary membershipSummary { get; set; }
        public bool isModerator { get; set; }
        public string groupId { get; set; }
        public bool channelOnlyMember { get; set; }
        public Channelsettings channelSettings { get; set; }
        public string tenantId { get; set; }
        public string description { get; set; }
        public Membersettings memberSettings { get; set; }
        public Guestsettings1 guestSettings { get; set; }
        public string sharepointSiteUrl { get; set; }
        public bool explicitlyAdded { get; set; }
        public List<Activemeetup> activeMeetups { get; set; }
        public bool isShared { get; set; }
    }

    public class Consumptionhorizon
    {
        public long originalArrivalTime { get; set; }
        public long timeStamp { get; set; }
        public string clientMessageId { get; set; }
    }

    public class Userconsumptionhorizon
    {
        public long originalArrivalTime { get; set; }
        public long timeStamp { get; set; }
        public string clientMessageId { get; set; }
    }

    public class Defaultfilesettings
    {
        public string filesRelativePath { get; set; }
        // default, 
        public string documentLibraryId { get; set; }
        // Freigegebene Dokumente, 
        public string sharepointRootLibrary { get; set; }
    }

    public class Channelsettings
    {
        public long channelPostPermissions { get; set; }
        public long channelReplyPermissions { get; set; }
        public long channelPinPostPermissions { get; set; }
        public long channelBotsPostPermissions { get; set; }
        public long channelConnectorsPostPermissions { get; set; }
        public Membersettings memberSettings { get; set; }
        public Guestsettings guestSettings { get; set; }
    }

    public class Membersettings
    {
        public bool createTab { get; set; }
        public bool deleteTab { get; set; }
        public bool createIntegration { get; set; }
        public bool updateIntegration { get; set; }
        public bool deleteIntegration { get; set; }
        public bool giphyEnabled { get; set; }
        public bool stickersEnabled { get; set; }
        public int giphyRating { get; set; }
        public bool customMemesEnabled { get; set; }
        public bool teamMemesEnabled { get; set; }
        public bool teamMention { get; set; }
        public bool adminDeleteEnabled { get; set; }
        public bool deleteEnabled { get; set; }
        public bool editEnabled { get; set; }
        public bool installApp { get; set; }
        public bool uninstallApp { get; set; }
    }

    public class Guestsettings
    {
        public bool createTab { get; set; }
        public bool deleteTab { get; set; }
        public bool createIntegration { get; set; }
        public bool updateIntegration { get; set; }
        public bool deleteIntegration { get; set; }
        public bool giphyEnabled { get; set; }
        public bool stickersEnabled { get; set; }
        public int giphyRating { get; set; }
        public bool customMemesEnabled { get; set; }
        public bool teamMemesEnabled { get; set; }
        public bool teamMention { get; set; }
        public bool adminDeleteEnabled { get; set; }
        public bool deleteEnabled { get; set; }
        public bool editEnabled { get; set; }
        public bool installApp { get; set; }
        public bool uninstallApp { get; set; }
    }

    public class Guestsettings1
    {
        public bool createTab { get; set; }
        public bool deleteTab { get; set; }
        public bool createIntegration { get; set; }
        public bool updateIntegration { get; set; }
        public bool deleteIntegration { get; set; }
        public bool giphyEnabled { get; set; }
        public bool stickersEnabled { get; set; }
        public int giphyRating { get; set; }
        public bool customMemesEnabled { get; set; }
        public bool teamMemesEnabled { get; set; }
        public bool teamMention { get; set; }
        public bool adminDeleteEnabled { get; set; }
        public bool deleteEnabled { get; set; }
        public bool editEnabled { get; set; }
        public bool installApp { get; set; }
        public bool uninstallApp { get; set; }
    }

    public class Tab
    {
        public string id { get; set; }
        public string name { get; set; }
        public string definitionId { get; set; }
        public string directive { get; set; }
        public string tabType { get; set; }
        public float order { get; set; }
        public BigInteger replyChainId { get; set; }
        public Settings settings { get; set; }
        public string externalId { get; set; }

    }

    public class Settings
    {
        // "extension", "webpage"
        public string subtype { get; set; }
        public string name { get; set; }
        public string url { get; set; }
        public string removeUrl { get; set; }
        public string websiteUrl { get; set; }
        public string entityId { get; set; }
        public DateTime dateAdded { get; set; }
        public BigInteger meetingNotesPageId { get; set; }
        public string sharepointPath { get; set; }
        public string reports { get; set; }
        public string originatingTeam { get; set; }
        public string originatingTeamId { get; set; }
        public BigInteger wikiTabId { get; set; }
        public bool wikiDefaultTab { get; set; }
        public bool hasContent { get; set; }
        public bool isPrivateMeetingWiki { get; set; }
        public bool meetingNotes { get; set; }
        public string scenarioName { get; set; }
        public string siteUrl { get; set; }
        public string libraryServerRelativeUrl { get; set; }
        public string libraryId { get; set; }
        public string selectedDocumentLibraryTitle { get; set; }
        public string selectedSiteImageUrl { get; set; }
        public string selectedSiteTitle { get; set; }
        public string file { get; set; }
        public string objectId { get; set; }
        public string suggestedTabName { get; set; }
        // has been null so far; seen for subtype "webpage"
        public object similarApp { get; set; }
    }

    public class Connectorprofile
    {
        // "https://statics.teams.microsoft.com/evergreen-assets/mailhookservice/mailicon.png?v=2"
        public string avatarUrl { get; set; }
        // "Email Connector"
        public string displayName { get; set; }
        public object incomingUrl { get; set; }
        // "Incoming"
        public string connectorType { get; set; }
        public string id { get; set; }
    }

    public partial class Chat
    {
        public string id { get; set; }
        public Consumptionhorizon1 consumptionHorizon { get; set; }
        public Userconsumptionhorizon1 userConsumptionHorizon { get; set; }
        public object retentionHorizon { get; set; }
        public object retentionHorizonV2 { get; set; }
        public List<ChatMember> members { get; set; }
        public string title { get; set; }
        public long version { get; set; }
        public long threadVersion { get; set; }
        public bool isRead { get; set; }
        public bool isHighImportance { get; set; }
        public bool isOneOnOne { get; set; }
        public Message LastMessage { get; set; }
        public bool isLastMessageFromMe { get; set; }
        public int chatSubType { get; set; }
        public DateTime lastJoinAt { get; set; }
        public DateTime createdAt { get; set; }
        public string creator { get; set; }
        public string tenantId { get; set; }
        public bool hidden { get; set; }
        public bool isGapDetectionEnabled { get; set; }
        public long interopType { get; set; }
        public bool isMessagingDisabled { get; set; }
        public bool isMuted { get; set; }
        public bool isDisabled { get; set; }
        // "chat", "meeting", "sfbinteropchat", ...
        public string chatType { get; set; }
        // "None", ...
        public string interopConversationStatus { get; set; }
        public long conversationBlockedAt { get; set; }
        public bool hasTranscript { get; set; }
        public bool isSticky { get; set; }
        // "Unknown"
        public string meetingPolicy { get; set; }
        public object meetingContent { get; set; }
        public DateTime lastLeaveAt { get; set; }
        public Meetinginformation meetingInformation { get; set; }
        public DateTime shareHistoryFromTime { get; set; }
        public Extensiondefinition1 extensionDefinition { get; set; }
        public Meetingobjectsconfig meetingObjectsConfig { get; set; }
        public List<Tab> tabs { get; set; }
        public Activemeetup activeMeetup { get; set; }
        public DateTime lastImportantMessageTime { get; set; }


        // set when hiding a chat
        public DateTime unpinnedTime { get; set; }
        // set when hiding a chat
        public DateTime hiddenTime { get; set; }
        public bool isConversationDeleted { get; set; }
        public bool isExternal { get; set; }
        // "unknown"
        public string importState { get; set; }

    }

    public partial class Chat : IChatChangeInfo
    {
        public string Id => id;

        public long Version => version;

        public long ThreadVersion => threadVersion;

        public long LastMessageVersion => long.TryParse(LastMessage?.version, out var lastMessageVersion) ? lastMessageVersion : Constants.MissingVersionIndicator;

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        public string? TitleOrFolderName => null;
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        public DateTime? CreatedAt => createdAt;
    }

    public class Consumptionhorizon1
    {
        public long originalArrivalTime { get; set; }
        public long timeStamp { get; set; }
        public string clientMessageId { get; set; }
    }

    public class Userconsumptionhorizon1
    {
        public long originalArrivalTime { get; set; }
        public long timeStamp { get; set; }
        public string clientMessageId { get; set; }
    }

    public class Meetinginformation
    {
        public string subject { get; set; }
        public string location { get; set; }
        public DateTime startTime { get; set; }
        public DateTime endTime { get; set; }
        public string exchangeId { get; set; }
        public string iCalUid { get; set; }
        public bool isCancelled { get; set; }
        public Eventrecurrencerange eventRecurrenceRange { get; set; }
        public Eventrecurrencepattern eventRecurrencePattern { get; set; }
        public int appointmentType { get; set; }
        public int meetingType { get; set; }
        public string scenario { get; set; }

        public string organizerId { get; set; }
        public string tenantId { get; set; }
    }

    public class Eventrecurrencerange
    {
        public DateTime startDate { get; set; }
        public DateTime endDate { get; set; }
    }

    public class Eventrecurrencepattern
    {
        public int patternType { get; set; }
        public Weekly weekly { get; set; }
        public Daily daily { get; set; }
        public Relativemonthly relativeMonthly { get; set; }
    }

    public class Weekly
    {
        public long interval { get; set; }
        public List<int> daysOfTheWeek { get; set; }
    }

    public class Daily
    {
        public long interval { get; set; }
    }

    public class Relativemonthly
    {
        public long interval { get; set; }
        public int weekOfTheMonthIndex { get; set; }
        public int dayOfTheWeek { get; set; }
    }

    public class Extensiondefinition1
    {
        public DateTime updatedTime { get; set; }
    }

    public class Meetingobjectsconfig
    {
        public Configurations configurations { get; set; }
    }

    public class Configurations
    {
        public Attendancereportconfig attendancereportConfig { get; set; }
        public Transcriptconfig transcriptConfig { get; set; }
    }

    public class Transcriptconfig
    {
        // "true", ...
        public string hasTranscript { get; set; } // note: this is _without_ quotes (e.g. a bool) in the list of personal chats, but with quotes (e.g. a string) when coming as notification...
    }

    public class Attendancereportconfig
    {
        // "True"
        public bool hasAttendanceReport { get; set; }
        // "00000000-0000-beef-0000-000000000000@00000000-0000-beef-0000-000000000000"
        public string organizerId { get; set; }
        public string callId { get; set; }
        // "api"
        public string serviceLocation { get; set; }
        public DateTime updateTime { get; set; } // found with notifications
    }

    public class Activemeetup
    {
        public string messageId { get; set; }
        public string conversationUrl { get; set; }
        public string conversationId { get; set; }
        public string groupCallInitiator { get; set; }
        public bool wasInitiatorInLobby { get; set; }
        public DateTime expiration { get; set; }
        public string status { get; set; }
        public bool isHostless { get; set; }
        public string tenantId { get; set; }
        public string organizerId { get; set; }
        public long callMeetingType { get; set; }
        public string conversationType { get; set; }
        public Meetingdata meetingData { get; set; }
        // "0"
        public string replyChainMessageId { get; set; }
    }

    public class Meetingdata
    {
    }

    public class ChatMember
    {
        public bool isMuted { get; set; }
        public string mri { get; set; }
        // id part of the MRI
        public string objectId { get; set; }
        public string role { get; set; }
        public DateTime shareHistoryTime { get; set; }
        public string friendlyName { get; set; }
        public string tenantId { get; set; }
    }

    public class Privatefeed
    {
        public string id { get; set; }
        public string type { get; set; }
        public long version { get; set; }
        public Properties properties { get; set; }
        public Message LastMessage { get; set; }
        public string messages { get; set; }
        public string targetLink { get; set; }
        public string streamType { get; set; }
    }

    public class Properties
    {
        public string consumptionhorizon { get; set; }
        public string isemptyconversation { get; set; }
        public string consumptionHorizonBookmark { get; set; }
    }
}
