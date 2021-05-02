using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Numerics;
using TeasmCompanion.TeamsInternal.TeamsInternalApi._shared;
using TeasmCompanion.TeamsInternal.TeamsInternalApi.v1.users.me.conversations;

#nullable enable

namespace TeasmCompanion.TeamsInternal.TeamsInternalApi.v2.users.me.endpoints
{

    public class GET_Endpoint_ResponseBody
    {
        public string? next { get; set; }
        public string? previous { get; set; }
        public List<Eventmessage>? eventMessages { get; set; }
    }

    public class Eventmessage
    {
        public BigInteger? id { get; set; }
        public DateTime? time { get; set; }
        // EventMessage, 
        public string? type { get; set; }
        public string? resourceLink { get; set; }
        // NewMessage, MessageUpdate, CustomUserProperties, ThreadUpdate
        public string? resourceType { get; set; }
        public Resource? resource { get; set; }
    }

    public partial class Resource
    {
        public string? clientmessageid { get; set; }
        public string? content { get; set; }
        public string? from { get; set; }
        public string? imdisplayname { get; set; }
        public string? id { get; set; }
        // Control/Typing, Text, ThreadActivity/MemberConsumptionHorizonUpdate
        public string? messagetype { get; set; }
        public DateTime? originalarrivaltime { get; set; }
        public MessageProperties? properties { get; set; }
        public BigInteger? sequenceId { get; set; }
        public string? version { get; set; }
        public DateTime? composetime { get; set; }
        // "Thread"
        public string? type { get; set; }
        public string? conversationLink { get; set; }
        /*
         * Sample values:
         * - 19:<guid>_<guid>@unq.gbl.spaces (personal chat 1:1) (with threadtype=chat)
         * - 19:1111112a5f76477d90c0693baf556efb@thread.v2 (personal chat with multiple people) (with threadtype=chat)
         * - 48:notifications (notification about being mentioned) (with threadtype=streamofnotifications)
         */
        public string? to { get; set; }
        // text, announcement
        public string? contenttype { get; set; }
        public Annotationssummary? annotationsSummary { get; set; }
        // "chat", "streamofnotifications", "streamofcalllogs"
        public string? threadtype { get; set; }
        public bool isactive { get; set; }
        public string? threadtopic { get; set; }
        public List<EventResourceMember>? members { get; set; }
        public Rostersummary? rosterSummary { get; set; }
        public long? rosterVersion { get; set; }


        public Message? lastMessage { get; set; }
        public BigInteger? lastUpdatedMessageId { get; set; }
        public long? lastUpdatedMessageVersion { get; set; }
        public string? messages { get; set; }
        public string? targetLink { get; set; }
        public Threadproperties? threadProperties { get; set; }
        // consumptionhorizon, unpinnedTime, ...
        public List<string>? propertiesUpdated { get; set; }
        public Memberproperties? memberProperties { get; set; }
        // note: this seems to be a string?ified JSON object as well
        public string? cortanaSettings { get; set; }
    }

    // for resourceType="MessageUpdate" (many missing but still)
    public partial class Resource
    {
        public string? skypeeditedid { get; set; }
    }

    // for resourceType="NewMessage"
    public partial class Resource
    {
        // a GUID
        public string? skypeguid { get; set; }
        // was set by a Polly message
        public List<Onbehalfof>? onbehalfof { get; set; }
    }

    public class Onbehalfof
    {
        public BigInteger? itemid { get; set; }
        // "person"
        public string? mentionType { get; set; }
        public string? mri { get; set; }
        public string? displayName { get; set; }
    }

    // for resourceType="CustomUserProperties"
    public partial class Resource
    {
        [JsonConverter(typeof(EmbeddedLiteralConverter<Userpinnedapps>))]
        public Userpinnedapps? userPinnedApps { get; set; }
        [JsonConverter(typeof(EmbeddedLiteralConverter<List<TeamOrder>>))]
        public List<TeamOrder>? teamsOrder { get; set; }
        [JsonConverter(typeof(EmbeddedLiteralConverter<PersonalFileSite>))]
        public PersonalFileSite? personalfilesite { get; set; }
        // seems to be a timestamp
        public string? contactsTabLastVisitTime { get; set; }
        [JsonConverter(typeof(EmbeddedLiteralConverter<UserDetails>))]
        public UserDetails? userDetails { get; set; }
        // contains JSON literal; a hashmap with the key being a chat id and the value being the order; is sent when pinning a chat
        public string? favorites { get; set; }
        // contains JSON literal; like: "{\"Web\":\"2021-01-01T01:01:01.000Z\",\"espFirstRunStarted\":\"2021-01-01T01:01:01.000Z\"}"
        public string? firstLoginInformation { get; set; }
        [JsonConverter(typeof(EmbeddedLiteralConverter<List<SuggestedContact>>))]
        public List<SuggestedContact>? suggestedContacts { get; set; }
        // JSON literal of external files/cloud providers (like Google)
        public string? externalFilesProviders { get; set; }
    }

    public class UserDetails
    {
        public string? name { get; set; }
        public string? upn { get; set; }
    }

    public class TeamOrder
    {
        public int? order { get; set; }
        public string? teamId { get; set; }
    }

    public class Card
    {
        // like "11111fa3111b4573b77c41c4078d5750"
        public string? cardClientId { get; set; }
        public CardContent? content { get; set; }
        // "application/vnd.microsoft.teams.messaging-announcementBanner"
        // "application/vnd.microsoft.card.codesnippet" -> card client ID 111111a168e04361ac70daf72cd1d4e6
        // "application/vnd.microsoft.card.thumbnail" -> card client ID 111119675cf94000b377ffccd93b1742
        // "application/vnd.microsoft.card.adaptive"
        // see https://github.com/microsoftgraph/microsoft-graph-docs/blob/master/api-reference/beta/resources/chatmessageattachment.md ?
        public string? contentType { get; set; }

        // "com.microsoftstream.embed.skypeteamstab", or some GUID
        public string? appId { get; set; }
        // "Stream"
        public string? appName { get; set; }
        // "de-de"
        public string? locale { get; set; }
        public string? appIcon { get; set; }
    }

    // common properties
    public partial class CardContent
    {
        public string? cardClientId { get; set; }
        public string? title { get; set; }
        public string? text { get; set; }
        public List<Image>? images { get; set; }
        public Tap? tap { get; set; }
    }

    // contenttype=application/vnd.microsoft.card.hero && appId=com.microsoft.teamspace.tab.youtube
    public partial class CardContent
    {
        public string? subtitle { get; set; }
        public List<object>? buttons { get; set; }
    }

    // for application/vnd.microsoft.teams.messaging-announcementBanner
    public partial class CardContent
    {
        public long? colorTheme { get; set; }
        public Imagedata? imageData { get; set; }
    }

    // for application/vnd.microsoft.card.codesnippet
    public partial class CardContent
    {
        public List<object>? observers { get; set; }
        public List<object>? cardButtons { get; set; }
        public object? tapButton { get; set; }
        public object? cardSender { get; set; }
        public string? replyChainId { get; set; }
        public string? conversationId { get; set; }
        public bool? alwaysExpand { get; set; }
        public bool? viewOnly { get; set; }
        public string? clientMessageId { get; set; }
        public string? serverMessageId { get; set; }
        public string? messageType { get; set; }
        public bool? isInputExtension { get; set; }
        public bool? hasMentions { get; set; }
        // codeSnippetDialog_00000000-0000-beef-0000-000000000000
        public string? handler { get; set; }
        public bool? editable { get; set; }
        public string? id { get; set; }
        public string? name { get; set; }
        // Text
        public string? language { get; set; }
        public int lines { get; set; }
        public string? amsReferenceId { get; set; }
        public bool? wrap { get; set; }
    }

    // application/vnd.microsoft.card.audio
    public partial class CardContent
    {
        // "PT2S"
        public string? duration { get; set; }
        public List<Medium>? media { get; set; }
    }

    // for contentType=="application/vnd.microsoft.card.adaptive" && content.type=="AdaptiveCard"
    public partial class CardContent
    {
        //public string type { get; set; }
        public List<AdaptiveCardBody>? body { get; set; }
        // "https://adaptivecards.io/schemas/adaptive-card.json"
        [JsonProperty("$schema")]
        public string? schema { get; set; }
        // "1.0"
        public string? version { get; set; }
        public MsTeams? msTeams { get; set; }

    }

    // general
    public partial class CardContent
    {
        // "AdaptiveCard"
        public string? type { get; set; }

    }

    public class MsTeams
    {
        public string? width { get; set; }
    }


    public class Medium
    {
        // e.g. "https://eu-api.asm.skype.com/v1/objects/0-neu-d2-<id>/views/audio"
        public string? url { get; set; }
    }


    public class Tap
    {
        // "openUrl"
        public string? type { get; set; }
        public string? title { get; set; }
        public string? value { get; set; }
    }

    public class Image
    {
        public string? alt { get; set; }
        public string? url { get; set; }
    }

    public partial class Activitycontext
    {
        public string? WebhookCorrelationId { get; set; }
        public string? activityProcessingLatency { get; set; }
    }

    // activitySubtype="like"
    public partial class Activitycontext
    {
        // "1"
        public string? like { get; set; }
        public string? heart { get; set; }
        public string? star { get; set; }
        public string? surprised { get; set; }
        public string? laugh { get; set; }
        public string? sad { get; set; }
        public string? angry { get; set; }
        public string? follow { get; set; }
    }

    // activitySubtype="tag"
    public partial class Activitycontext
    {
        // name of tag
        public string? tagname { get; set; }
    }

    // activityType=="mention" activitySubtype="channel"
    public partial class Activitycontext
    {
        // "false"
        public string? displayBanner { get; set; }
    }

    // activityType=="call" activitySubtype="missedCall"
    public partial class Activitycontext
    {
        // JSON literal like "{\"riskLevel\":\"medium-low\"}"
        public string? spamProperties { get; set; }
    }

    public class User
    {
        public string? mri { get; set; }
        public long? time { get; set; }
        public string? value { get; set; }
    }

    public class Annotationssummary
    {
        public Emotions? emotions { get; set; }
    }

    public class Rostersummary
    {
        public int? memberCount { get; set; }
        public int? botCount { get; set; }
        public int? readerCount { get; set; }
        public Rolecounts? roleCounts { get; set; }
        public int? externalMemberCount { get; set; }
    }

    public class Rolecounts
    {
        public int? User { get; set; }
        public int? Admin { get; set; }
        public int? Guest { get; set; }
    }

    public class EventResourceMember
    {
        // "true"
        public string? alerts { get; set; }
        public long? expiration { get; set; }
        public string? isFollowing { get; set; }
        public string? id { get; set; }
        // "Anonymous", "Admin"
        public string? role { get; set; }
        public Botsettings? botSettings { get; set; }
        public long? shareHistoryTime { get; set; }
        public long? memberExpirationTime { get; set; }
        public long? expirationTimeInSeconds { get; set; }
        public string? tenantId { get; set; }
    }

    public class Activity
    {
        // "replyToReply", "reactionInChat"
        public string? activityType { get; set; }
        // e.g. "2"
        public string? count { get; set; }
        // "like" (for activityType="reactionInChat")
        public string? activitySubtype { get; set; }
        public DateTime? activityTimestamp { get; set; }
        public BigInteger? activityId { get; set; }
        // like "19:someid@thread.tacv2"
        public string? sourceThreadId { get; set; }
        public BigInteger? sourceMessageId { get; set; }
        // mri of user
        public string? sourceUserId { get; set; }
        public string? sourceUserImDisplayName { get; set; }
        // mri
        public string? targetUserId { get; set; }
        public string? messagePreview { get; set; }
        // "hasText", "previewUnavailable"
        public string? messagePreviewTemplateOption { get; set; }
        public Activitycontext? activityContext { get; set; }
        public string? sourceThreadTopic { get; set; }
        public int sourceThreadRosterNonBotMemberCount { get; set; }
        public string? sourceThreadIsPrivateChannel { get; set; }

        public BigInteger? sourceReplyChainId { get; set; }
        // like "19:someid@thread.tacv2"
        public string? targetThreadId { get; set; }

        public string? activityTitle { get; set; }
        public string? customTapAction { get; set; }
    }

    public partial class Threadproperties
    {
        public string? hidden { get; set; }
        // "meeting", "space", 
        public string? threadType { get; set; }
        public string? topic { get; set; }
        public string? ongoingCallChatEnforcement { get; set; }
        public bool isCreator { get; set; }
        public string? gapDetectionEnabled { get; set; }
        public string? lastjoinat { get; set; }
        public string? lastSequenceId { get; set; }
        public long? version { get; set; }
        public long? rosterVersion { get; set; }
        public string? tenantid { get; set; }

        public string? privacy { get; set; }
        public string? topicThreadTopic { get; set; }
        public string? spaceId { get; set; }
        public string? lastleaveat { get; set; }
        // e.g. "1"
        public string? spaceTypes { get; set; }
        // "standard", 
        public string? spaceType { get; set; }
        // seems to be the channel title
        public string? spaceThreadTopic { get; set; }
        // nested JSON like "{\"linkedSpaceInfoItems\":[{\"spaceThreadId\":\"19:someid@thread.skype\",\"role\":0,\"linkState\":0}]}",
        public string? parentSpaces { get; set; }
        public string? sharepointSiteUrl { get; set; }
        // nested JSON like "topics": "[{\"id\":\"19:someid@thread.tacv2\",\"name\":\"Topic 1\",\"picture\":null,\"createdat\":\"1111111111\",\"isdeleted\":false},{\"id\":\"19:someid@thread.tacv2\",\"name\":\"Topic 2\",\"picture\":null,\"createdat\":\"1111111111\",\"isdeleted\":false}]",
        public string? topics { get; set; }
        // a GUID
        public string? groupId { get; set; }
    }
    public partial class Threadproperties
    {
        // "True"; this is set for chats with Skype users
        public string? isFederated { get; set; }
    }

    public class Memberproperties
    {
        // "Anonymous", ...
        public string? role { get; set; }
        public bool? isReader { get; set; }
        public long? memberExpirationTime { get; set; }
        public bool? isModerator { get; set; }
        public bool? isFollowing { get; set; }
        public object? isFavorite { get; set; }
        public object? isPinned { get; set; }
    }

    public class Botsettings
    {
        // At
        public string? messagingMode { get; set; }
    }

}
