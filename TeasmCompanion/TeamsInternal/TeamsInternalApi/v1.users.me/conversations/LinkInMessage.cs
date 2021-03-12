using Newtonsoft.Json;

namespace TeasmCompanion.TeamsInternal.TeamsInternalApi.v1.users.me.conversations
{
    // http://schema.skype.com/HyperLink
    public class HyperLink
    {
        [JsonProperty("@type")]

        public string type { get; set; }
        // can be a number or the URL
        public string itemid { get; set; }
        public string url { get; set; }
        public Preview preview { get; set; }
        public bool previewenabled { get; set; }
        public Fileshareurlmeta fileShareUrlMeta { get; set; }
    }

    public class Fileshareurlmeta
    {
        public string objectUrl { get; set; }
        public string shareUrl { get; set; }
        public string id { get; set; }
        public string type { get; set; }
    }

    public class Preview
    {
        public string previewurl { get; set; }
        public Previewmeta previewmeta { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public bool isLinkUnsafe { get; set; }
        public bool isFromApp { get; set; }
        public string safeUrl { get; set; }
    }

    public class Previewmeta
    {
        public int width { get; set; }
        public int height { get; set; }
    }
}
