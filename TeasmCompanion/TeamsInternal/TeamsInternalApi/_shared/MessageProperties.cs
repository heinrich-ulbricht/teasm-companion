using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Numerics;
using TeasmCompanion.Misc;
using TeasmCompanion.TeamsInternal.TeamsInternalApi.api.csa.api.v1.teams.users;
using TeasmCompanion.TeamsInternal.TeamsInternalApi.v1.users.me.conversations;
using TeasmCompanion.TeamsInternal.TeamsInternalApi.v2.users.me.endpoints;

#nullable enable

namespace TeasmCompanion.TeamsInternal.TeamsInternalApi._shared
{
    // Some common properties
    public partial class MessageProperties
    {
        // this is a user mri
        public string? creator { get; set; }
        public string? tenantid { get; set; }
        // "topic"
        public string? threadType { get; set; }
        public string? createRelatedMessagesIndex { get; set; }
        public string? createdat { get; set; }
        public string? historydisclosed { get; set; }
        public string? isMigrated { get; set; }
        public string? switchWriteEnabled { get; set; }
        public string? gapDetectionEnabled { get; set; }
    }

    // Properties of all sorts of notifications, lumped together
    public partial class MessageProperties
    {
        [JsonConverter(typeof(EmbeddedLiteralConverter<List<Mention>>))]
        public List<Mention>? mentions { get; set; }
        [JsonConverter(typeof(EmbeddedLiteralConverter<List<Card>>))]
        public List<Card>? cards { get; set; }
        [JsonConverter(typeof(EmbeddedLiteralConverter<List<HyperLink>>))]
        public List<HyperLink>? links { get; set; }
        public Activity? activity { get; set; }
        // "skypespacesMT", "concore_gvc", "skypespaces", "MailhookService" (for incoming mails), "SfBInteropGateway" (chat with Skype user)
        public string? s2spartnername { get; set; }
        // seems to be a serialized Meeting object? like this: "{\"meetingtitle\":\"Standup\"}"
        public string? meeting { get; set; }
        public string? isread { get; set; }
        public string? importance { get; set; }
        // for deleted posts this is a string?, e.g. "1111111111111", but was also observed as long
        public object? deletetime { get; set; }
        public Notification? notification { get; set; }
        public bool? skipfanouttobots { get; set; }
        // e.g. for announcements the big headline
        public string? title { get; set; }
        // e.g. for announcements the small sub-headline
        public string? subject { get; set; }
        public List<Emotions>? emotions { get; set; }
        public List<object?>? ams_references { get; set; }

        public DateTime? lastimreceivedtime { get; set; }
        public string? consumptionhorizon { get; set; }
        // "false", "true"
        public string? alerts { get; set; }
        public string? uniquerosterthread { get; set; }
        public string? chatFilesIndexId { get; set; }
        public string? fixedRoster { get; set; }
        // "SMBA" (for Bots), "skypespacesmt" (for ThreadUpdate)
        public string? partnerName { get; set; }
        public string? consumptionHorizonBookmark { get; set; }
        // like "1111111111111"
        public long? edittime { get; set; }
        // like "01/01/2021 11:11:11"
        public string? composetime { get; set; }

        // like "True" (yes, uppercase "T")
        public string? isemptyconversation { get; set; }
        public string? created { get; set; }
        public string? isfollowed { get; set; }
        public string? favorite { get; set; }
        // like "true"
        public string? ispinned { get; set; }

        // like "1aa1a1aaaa111111111111a111111111"
        public string? crossPostId { get; set; }

        public DateTime? lastimportantimreceivedtime { get; set; }
        // like "true"
        public string? collapsed { get; set; }

        [JsonConverter(typeof(EmbeddedLiteralConverter<List<MessageFile>>))]
        public List<MessageFile>? files { get; set; } // note: this is sometimes "null"

        // "concore_gvc" -> with skypeguid set
        public BigInteger? counterPartyMessageId { get; set; }
        public BigInteger? origincontextid { get; set; }
        // seems to be identical to the "callid" of a running call
        public string? skypeguid { get; set; }
    }

    // type "Message", messagetype "ThreadActivity/DeleteMember"
    public partial class MessageProperties
    {
        // "DeleteUser"
        public string? eventReason { get; set; }
    }

    // this seems to be set if sending to or receiving from outside of Teams
    public partial class MessageProperties
    {
        // "receiverSfB", "senderSfB"
        public string? interopType { get; set; }
        // set when sending a message
        public Deliverystate? deliveryState { get; set; }
    }

    public class Deliverystate
    {
        public string? state { get; set; }
        public DateTime? time { get; set; }
        public object? diagInfo { get; set; }
    }

    // Properties for EventMessage.type="EventMessage", EventMessage.resourceType="ThreadUpdate", EventMessage.resource.type="ThreadUpdate"
    // topicThreadVersion="v5"
    [JsonConverter(typeof(StoreDynamicPropertyWithPrefixInCollection), "tab::,integration:,awareness_conversationLiveState:")]
    public partial class MessageProperties
    {
        // "False"
        public string? containsExternalEntitiesListeningAll { get; set; }
        public string? privacy { get; set; }
        public string? topic { get; set; }
        // "v5"
        public string? topicThreadVersion { get; set; }
        public string? topicThreadTopic { get; set; }
        public string? sharepointChannelDocsFolder { get; set; }
        // "19:00000000000000000000000000000000@thread.skype"
        public string? spaceId { get; set; }
        // "default", ...
        public string? channelDocsDocumentLibraryId { get; set; }
        // "/sites/...", 
        public string? channelDocsFolderRelativeUrl { get; set; }
        // JSON literal like "{\"postPermissions\":1,\"allowReplies\":0,\"allowPinPosts\":0,\"allowBotsPost\":1,\"allowConnectorsPost\":1}"
        public string? channelSettings { get; set; }
        public string? isMigratedThread { get; set; }
        // "92:00000000000000000000000000000000@thread.skype"
        public string? RootResourceGroupId { get; set; }
        public string? groupId { get; set; }
        public string? description { get; set; }

        // "0"
        public string? creatorcid { get; set; }
        // "True"
        public string? favDefault { get; set; }
        // "false"
        public string? hidden { get; set; }

        [JsonConverter(typeof(EmbeddedLiteralConverter<Meetingobjectsconfig>))]
        public Meetingobjectsconfig? meetingobjectsConfig { get; set; }

        // this collects all tab infos; they have a dynamic JSON property name like 
        // - "tab::00000000-0000-beef-0000-000000000000"
        // - "tab::19:meeting_aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa@thread.skype"
        [JsonIgnore]
        [CollectionDictForPrefix("tab::")]
        public Dictionary<string, TabNotificationValue>? tabInfos { get; set; }

        // here the dynamic property name is like "integration:someid"
        [JsonIgnore]
        [CollectionDictForPrefix("integration:")]
        public Dictionary<string, IntegrationValue>? integrationInfos { get; set; }
    }

    // Properties for EventMessage.type="EventMessage", EventMessage.resourceType="ThreadUpdate", EventMessage.resource.type="ThreadUpdate"
    // topicThreadVersion="v9"
    public partial class MessageProperties
    {
        public string? spaceThreadTopic { get; set; }
        public string? spaceThreadVersion { get; set; }
        public string? aadTeamSmtpAddress { get; set; }
        public string? teamAlias { get; set; }
        public string? isSpoFileModifedFieldIndexed { get; set; }
        public string? adminLockedProperties { get; set; }
        public string? lastSyncTime { get; set; }
        public string? teamSmtpAddress { get; set; }
        public string? sharepointRootLibrary { get; set; }
        // seems to be channels
        [JsonConverter(typeof(EmbeddedLiteralConverter<List<Topic>>))]
        public List<Topic>? topics { get; set; }
        [JsonConverter(typeof(EmbeddedLiteralConverter<Spaceadminsettings>))]
        public Spaceadminsettings? spaceAdminSettings { get; set; }
        public string? spaceType { get; set; }
        // "private"
        public string? visibility { get; set; }
        public string? pictureETag { get; set; }
        [JsonConverter(typeof(EmbeddedLiteralConverter<Teamstatus>))]
        public Teamstatus? teamStatus { get; set; }
        public string? sharepointSiteUrl { get; set; }
        public string? sensitivityLabelId { get; set; }
        public string? joiningenabled { get; set; }
        // "none"
        public string? guestUsersCategory { get; set; }
        // public string? extensionDefinitionContainer { get; set; }
        // "True"
        public string? dynamicMembership { get; set; }
        // "Internal", ...
        public string? classification { get; set; }

        public long lastDeletedAt { get; set; }
        public long lastRestoredAt { get; set; }

    }

    // Properties for EventMessage.type="EventMessage", EventMessage.resourceType="ThreadUpdate", EventMessage.resource.type="Message"
    public partial class MessageProperties
    {
        [JsonConverter(typeof(EmbeddedLiteralConverter<Emaildetails>))]
        // set when message is a forwarded email
        public Emaildetails? emaildetails { get; set; }
    }

    // for threadType="meeting" and apparently also threadType "topic" for a channel meeting (?)
    public partial class MessageProperties
    {
        // "{\"updatedTime\":\"1111111111111\"}"
        public string? extensionDefinitionContainer { get; set; }
        // "false"
        public string? ongoingCallChatEnforcement { get; set; }

        // here the dynamic property name is like "integration:longtimestamp", like
        // - awareness_conversationLiveState:1600066000000
        // - awareness_conversationLiveState:0
        [JsonIgnore]
        [CollectionDictForPrefix("awareness_conversationLiveState:")]
        public Dictionary<string, awareness_conversationLiveState>? awareness_conversationLiveState { get; set; }

        [JsonConverter(typeof(EmbeddedLiteralConverter<List<MeetingContent>>))]
        public List<MeetingContent>? meetingContent { get; set; }
        public bool? isactive { get; set; }
    }

    // Properties for EventMessage.type="EventMessage", EventMessage.resourceType="ThreadUpdate", EventMessage.resource.type="Thread"
    public partial class MessageProperties
    {
        // JSON literal, like "{\"linkedSpaceInfoItems\":[{\"spaceThreadId\":\"19:00000000000000000000000000000000@thread.tacv2\",\"role\":0,\"linkState\":0}]}"
        public string? parentSpaces { get; set; }
        // "1-00000000-feeb-0000-beef-000000000000"
        public string? notebookId { get; set; }
        // "1"
        public string? spaceTypes { get; set; }
        // "true"
        public string? isDeleted { get; set; }
    }

    // for threadtype="streamofcalllogs"
    public partial class MessageProperties
    {
        [JsonConverter(typeof(EmbeddedLiteralConverter<CallLog>))]
        [JsonProperty("call-log")]
        public CallLog? calllog { get; set; }
    }

    // for notification that is sent when a chat is hidden by the user
    public partial class MessageProperties
    {
        // time the chat has been hidden by the user as part of resourceType==ConversationUpdate
        public string? unpinnedTime { get; set; }
    }

    public partial class MessageProperties
    {
        // JSON string like ""{\"connectorSenderGuid\":\"00000000-0000-beef-0000-000000000000\",\"providerAccountUniqueId\":null,\"connectorConfigurationAlternateId\":\"000000000000beef0000000000000000\"}""
        public string? meta { get; set; }
        //public string? importance { get; set; }
        //public string? subject { get; set; }
    }

    // Properties for EventMessage.type="EventMessage", EventMessage.resourceType="MessageUpdate", EventMessage.resource.type="Message"
    // topicThreadVersion="v5"
    public partial class MessageProperties
    {
        // "AuthorAndOwners"
        public string? replyPermission { get; set; }
    }

    // Properties for EventMessage.type="EventMessage", EventMessage.resourceType="MessageUpdate", EventMessage.resource.type="Message", EventMessage.resource.threadtype="Space"
    public partial class MessageProperties
    {
        // JSON literal -> an array containing document links and additionally BASE64-encoded data
        public string? atp { get; set; }
    }

    // Properties for EventMessage.type="EventMessage", EventMessage.resourceType="NewMessage", EventMessage.resource.type="Message", EventMessage.resource.threadtype="streamofbookmarks"
    public partial class MessageProperties
    {
        [JsonConverter(typeof(EmbeddedLiteralConverter<StarredMessage>))]
        public StarredMessage? starred { get; set; } // this is generated when you bookmark a post
    }

    // Properties for EventMessage.type="EventMessage", EventMessage.resourceType="MessageUpdate", EventMessage.resource.type="Message", EventMessage.resource.threadtype="topic"
    public partial class MessageProperties
    {
        // JSON literal like "{\"creatorId\":\"8:orgid:00000000-0000-0000-0000-000000000000\",\"pinnedTime\":0000000000000}"
        public string? pinned { get; set; } // goes along with properties importance, subject and links
    }

    // EventMessage.type="EventMessage", EventMessage.resourceType="NewMessage", EventMessage.resource.type="Message", EventMessage.resource.messagetype="RichText/Media_Card", EventMessage.resource.threadtype="meeting"
    public partial class MessageProperties
    {
        // note: first occured with a Polly poll sent by a user contained in this list
        public List<OnBehalfOf>? onbehalfof { get; set; }

    }

    // add more here
    public partial class MessageProperties
    {
        ////////////////// ADD HERE
    }

    public class Emaildetails
    {
        public EmailParticipant? from { get; set; }
        public object? sender { get; set; }
        public List<EmailParticipant>? to { get; set; }
        public object? cc { get; set; }
        public string? emailLink { get; set; }
        [JsonConverter(typeof(EmbeddedLiteralConverter<MessageFile>))]
        public MessageFile? emailFileLink { get; set; }
        public bool? isTruncated { get; set; }
        public object? emailHtmlLink { get; set; }
        public int? totalRecipients { get; set; }
    }

    public class EmailParticipant
    {
        public string? name { get; set; }
        public string? smtp { get; set; }
    }

    public class MeetingContent
    {
        // "whiteboard"
        public string? type { get; set; }
        public Data? data { get; set; }
        public string? id { get; set; }
        public string? url { get; set; }
        public string? shareUrl { get; set; }
    }

    public partial class Data
    {
        public string? url { get; set; }
        public string? shareUrl { get; set; }
    }
    public partial class Data
    {
        // Info separated with "|"
        public string? serializedRecordingData { get; set; }
        // "1,2,3,4"
        public string? recordingMessageIds { get; set; }
    }
    public class Teamstatus
    {
        public int? exchangeTeamCreationStatus { get; set; }
        public int? exchangeTeamDeletionStatus { get; set; }
        public int? sharepointSiteCreationStatus { get; set; }
        public long? sharepointProvisioningStartTime { get; set; }
        public int? teamResourceUrlCreationStatus { get; set; }
        public int? teamNotebookCreationStatus { get; set; }
    }

    public class Topic
    {
        public string? id { get; set; }
        public string? name { get; set; }
        public object? picture { get; set; }
        public string? createdat { get; set; }
        public bool? isdeleted { get; set; }
    }

    public class IntegrationValue
    {
        public string? integrationId { get; set; }
        public string? integrationType { get; set; }
        public string? displayName { get; set; }
        public string? avatarUrl { get; set; }
        public object? providerGuid { get; set; }
        public string? dataSchema { get; set; }
        public object? templateName { get; set; }
        public string? creatorSkypeMri { get; set; }
    }

    public class Notification
    {
        public bool? alert { get; set; }
        public bool? alertInMeeting { get; set; }
        public string? externalResourceUrl { get; set; }
    }

    public class OnBehalfOf
    {
        public long? itemid { get; set; }
        // "person", ...
        public string? mentionType { get; set; }
        // mri of user
        public string? mri { get; set; }
        // display name of user
        public string? displayName { get; set; }
    }
}
