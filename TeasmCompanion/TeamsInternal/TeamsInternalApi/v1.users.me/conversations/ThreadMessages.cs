using System;
using System.Collections.Generic;
using System.Numerics;
using TeasmCompanion.TeamsInternal.TeamsInternalApi._shared;

namespace TeasmCompanion.TeamsInternal.TeamsInternalApi.v1.users.me.conversations
{
    public class ThreadMessages
    {
        public List<Message> messages { get; set; }
        public _Metadata _metadata { get; set; }
    }

    public class _Metadata
    {
        public string backwardLink { get; set; }
        public string syncState { get; set; }
        public long lastCompleteSegmentStartTime { get; set; }
        public long? lastCompleteSegmentEndTime { get; set; }
    }

    public class Message
    {
        public string id { get; set; }
        public BigInteger? sequenceId { get; set; }
        public string clientmessageid { get; set; }
        public string version { get; set; }
        // like "19:<guid>_<guid>@unq.gbl.spaces"
        public string conversationid { get; set; }
        public string conversationLink { get; set; }
        public string type { get; set; }
        // RichText/Html, ThreadActivity/MemberJoined, Text (with contenttype==null), ThreadActivity/TabUpdated, Event/Call (with contenttype==application/user+xml), ThreadActivity/MemberLeft (contenttype==null),
        // ThreadActivity/TopicUpdate (contenttype==null), ThreadActivity/DeleteMember (contenttype==null), RichText/Media_CallRecording (contenttype==RichText/Media_CallRecording), RichText/Media_Card
        public string messagetype { get; set; }
        // text (can be combined with messagetype RichText/Html, seemingly for file links)
        public string contenttype { get; set; }
        public string content { get; set; }
        public List<string> amsreferences { get; set; }
        public string from { get; set; }
        public string to { get; set; }
        public string imdisplayname { get; set; }
        public DateTime? composetime { get; set; }
        public DateTime? originalarrivaltime { get; set; }
        public MessageProperties properties { get; set; }
        public Annotationssummary annotationsSummary { get; set; }
        public string s2spartnername { get; set; }
        public string skypeguid { get; set; }
        public string origincontextid { get; set; }
        public long? skypeeditoffset { get; set; }
        // like "1234590415455614000" as string
        public string skypeeditedid { get; set; }

        // the following properties are from a "last message" message, not seen on a regular one, yet
        public bool? isEscalationToNewPerson { get; set; }
        public string containerId { get; set; }
        public string parentMessageId { get; set; }
        public object threadType { get; set; }
        // set for threadtype=="meeting", messagetype=="ThreadActivity/MemberJoined"
        public bool isactive { get; set; }
    }

    public class Emotion
    {
        public string key { get; set; }
        public List<User> users { get; set; }
    }

    public class User
    {
        public string mri { get; set; }
        public long time { get; set; }
        public string value { get; set; }
    }

    public class Annotationssummary
    {
        public Emotions emotions { get; set; }
    }

    public class Emotions
    {
        public string key { get; set; }
        public List<User> users { get; set; }
        public int? like { get; set; }
        public int? heart { get; set; }
        public int? star { get; set; }
        public int? surprised { get; set; }
        public int? laugh { get; set; }
        public int? sad { get; set; }
        public int? angry { get; set; }
        public int? follow { get; set; }
    }
}
