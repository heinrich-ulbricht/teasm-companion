using Newtonsoft.Json;
using System;

namespace TeasmCompanion.TeamsInternal.TeamsInternalApi._shared
{
    public class StarredMessage
    {
        // http://schema.skype.com/StarredMessage
        [JsonProperty("@type")]
        public string type { get; set; }
        public string mri { get; set; }
        public string imdisplayname { get; set; }
        public string messageId { get; set; }
        public string parentMessageId { get; set; }
        public string threadId { get; set; }
        public object imageUri { get; set; }
        public bool isUnresolvedEmailUser { get; set; }
        public DateTime originalComposeTime { get; set; }
        public string creatorType { get; set; }
    }
}
