using System;
using System.Collections.Generic;
using TeasmCompanion.ProcessedTeamsObjects;

#nullable enable

namespace TeasmCompanion.Interfaces
{
    public interface IChatMessage
    {
        string? Id { get; }
        string? ChatId { get; }
        DateTime OriginalArrivalTime { get; }
        string? MessageSubject { get; }
        string? HtmlContent { get; }
        string? TextContent { get; }
        List<ProcessedTeamsUser> From { get; }
        List<ProcessedTeamsUser> To { get; }
        Dictionary<string, ImageInfo> ContentIds { get; }

        string SerializeOriginalMessageAsJson();
    }

    public interface IMutableChatMessage : IChatMessage
    {
        new string? MessageSubject { get; set; }
        new string? HtmlContent { get; set;  }
        new string? TextContent { get; set; }
    }
}
