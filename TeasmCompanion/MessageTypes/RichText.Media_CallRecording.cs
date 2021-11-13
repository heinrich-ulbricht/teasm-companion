using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace TeasmCompanion.MessageTypes
{

    public class RichTextMedia_CallRecordingWrapper
    {
        public RichTextMedia_CallRecording root { get; set; }
    }

    public class RichTextMedia_CallRecording
    {
        public Uriobject URIObject { get; set; }
    }

    public class Uriobject
    {
        // "1.0"
        [JsonProperty("@format_version")]
        public string format_version { get; set; }
        // "Video.2/CallRecording.1", ...
        [JsonProperty("@type")]
        public string type { get; set; }
        [JsonProperty("@url_thumbnail")]
        public string url_thumbnail { get; set; }
        [JsonProperty("@url_thumbnail_extra_small")]
        public string url_thumbnail_extra_small { get; set; }
        [JsonProperty("@url_thumbnail_small")]
        public string url_thumbnail_small { get; set; }
        [JsonProperty("@url_thumbnail_medium")]
        public string url_thumbnail_medium { get; set; }
        [JsonProperty("@url_thumbnail_large")]
        public string url_thumbnail_large { get; set; }
        [JsonProperty("@uri")]
        public string uri { get; set; }
        // 1.0
        [JsonProperty("@version")]
        public string version { get; set; }
        public Recordingstatus RecordingStatus { get; set; }
        public Sessionendreason SessionEndReason { get; set; }
        public Chunkendreason ChunkEndReason { get; set; }
        public string Title { get; set; }
        public A a { get; set; }
        public Originalname OriginalName { get; set; }
        public Meetingorganizerid MeetingOrganizerId { get; set; }
        public MeetingorganizerTenantId MeetingOrganizerTenantId { get; set; }
        public Recordinginitiatorid RecordingInitiatorId { get; set; }
        public ICalUid ICalUid { get; set; }
        public List<Identifier> Identifiers { get; set; }
        public List<Recordingcontent> RecordingContent { get; set; }
//        public List<Requestedexport> RequestedExports { get; set; }
        public object RequestedExports { get; set; }
    }

    public class Recordingstatus
    {
        // "Success"
        [JsonProperty("@status")]
        public string status { get; set; }
        [JsonProperty("@code")]
        public string code { get; set; }
        public object amsErrorResult { get; set; }
        public object StreamExportErrors { get; set; }
        public Onedriveforbusinessexporterror OnedriveForBusinessExportError { get; set; }
    }

    public class Onedriveforbusinessexporterror
    {
        [JsonProperty("@responseCode")]
        public string responseCode { get; set; }
        [JsonProperty("@errorCode")]
        public string errorCode { get; set; }
        [JsonProperty("@failingOperation")]
        public string failingOperation { get; set; }
        public Msgrapherror MsGraphError { get; set; }
    }

    public class Msgrapherror
    {
        [JsonProperty("@errorCode")]
        public string errorCode { get; set; }
        [JsonProperty("@innerErrorCode")]
        public string innerErrorCode { get; set; }
    }

    public class Sessionendreason
    {
        // SessionStillOngoing
        [JsonProperty("@value")]
        public string value { get; set; }
    }

    public class Chunkendreason
    {
        // ChunkIsBeingRecorded
        [JsonProperty("@value")]
        public string value { get; set; }
    }

    public class A
    {
        [JsonProperty("@href")]
        public string href { get; set; }
        // "Play"
        [JsonProperty("#text")]
        public string text { get; set; }
    }

    public class Originalname
    {
        [JsonProperty("@v")]
        public string v { get; set; }
    }

    public class Meetingorganizerid
    {
        [JsonProperty("@value")]
        public string value { get; set; }
    }

    public class MeetingorganizerTenantId
    {
        [JsonProperty("@value")]
        public string value { get; set; }
    }

    public class ICalUid
    {
        [JsonProperty("@value")]
        public string value { get; set; }
    }

    public class Recordinginitiatorid
    {
        [JsonProperty("@value")]
        public string value { get; set; }
    }

    //public class Requestedexports
    //{
    //    public Exportresult ExportResult { get; set; }
    //}

    //public class Exportresult
    //{
    //    // "ExportToOnedriveForBusiness"
    //    public string type { get; set; }
    //}

    public class Identifier
    {
        public List<Id> Id { get; set; }
    }

    public class Id
    {
        // callId, callLegId, chunkIndex, AMSDocumentID, StreamVideoId
        [JsonProperty("@type")]
        public string type { get; set; }
        [JsonProperty("@value")]
        public string value { get; set; }
    }

    public class Recordingcontent
    {
        [JsonProperty("@timestamp")]
        public DateTime timestamp { get; set; }
        [JsonProperty("@duration")]
        public string duration { get; set; }
        [JsonProperty("@canVideoExpire")]
        // "False"
        public string canVideoExpire { get; set; }
        public List<Item> item { get; set; }
    }

    public class Item
    {
        // "amsVideo", "rosterevents", "onedriveForBusinessVideo"
        [JsonProperty("@type")]
        public string type { get; set; }
        [JsonProperty("@uri")]
        public string uri { get; set; }
    }

    public class Requestedexport
    {
        [JsonProperty("@type")]
        public string type { get; set; }
    }

}
