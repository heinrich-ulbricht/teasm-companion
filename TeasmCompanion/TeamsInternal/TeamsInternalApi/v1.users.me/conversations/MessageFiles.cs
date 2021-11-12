using Newtonsoft.Json;

#nullable enable

namespace TeasmCompanion.TeamsInternal.TeamsInternalApi.v1.users.me.conversations
{
    // http://schema.skype.com/File
    public class MessageFile
    {
        [JsonProperty("@type")]
        public string? _type { get; set; }
        // 2
        public long? version { get; set; }
        // "tab::00000000-0000-beef-0000-000000000000"
        public string? id { get; set; }
        public string? baseUrl { get; set; }
        // "deeplink"
        public string? type { get; set; }
        public string? title { get; set; }
        // "reference"
        public string? state { get; set; }
        public string? objectUrl { get; set; }
        // "channel"
        public string? parentContext { get; set; }
        // embedded JSON like this: "{\\\"code\\\":null,\\\"type\\\":0}"
        public string? providerData { get; set; }
        public string? itemid { get; set; }
        public string? fileName { get; set; }
        // "deeplink"
        public string? fileType { get; set; }
        public string? channelId { get; set; }
        public Fileinfo? fileInfo { get; set; }
        public Botfileproperties? botFileProperties { get; set; }
        // can be a number, but can also be "share-point"...
        public string? sourceOfFile { get; set; }
        public Filepreview? filePreview { get; set; }
        public Filechicletstate? fileChicletState { get; set; }
        public Chicletbreadcrumbs? chicletBreadcrumbs { get; set; }
        // can differ from fileName
        public string? originalFileName { get; set; }
        // a GUID
        public string? requestId { get; set; }
        // seen to be null
        public long? size { get; set; }
        public object? anchorFileId { get; set; }
    }

    public class Chicletbreadcrumbs
    {
        public string? sourceTeamName { get; set; }
        public string? sourceChannelName { get; set; }
    }

    public class Fileinfo
    {
        public object? itemId { get; set; }
        public string? fileUrl { get; set; }
        public string? siteUrl { get; set; }
        // can be something like "{\"pageId\":109,\"sectionId\":112,\"origin\":2}"
        public string? serverRelativeUrl { get; set; }
        public object? shareUrl { get; set; }
        public object? shareId { get; set; }
    }

    public class Botfileproperties
    {
        // "https:\/\/contoso-my.sharepoint.com\/personal\/USERNAME\/_layouts\/15\/download.aspx?UniqueId=GUID&Translate=false&tempauth=TOKEN&ApiVersion=2.0"
        public string? url { get; set; }
    }

    public class Filepreview
    {
        public string? previewUrl { get; set; }
    }

    public class Filechicletstate
    {
        // "teams"
        public string? serviceName { get; set; }
        public string? state { get; set; }
    }

}
