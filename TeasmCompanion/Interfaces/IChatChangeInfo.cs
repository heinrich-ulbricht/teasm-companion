#nullable enable

using System;

namespace TeasmCompanion.Interfaces
{
    public interface IChatChangeInfo
    {
        string Id { get; }
        long Version { get; }
        long ThreadVersion { get; }
        // last message version or Constants.MissingVersionIndicator
        long LastMessageVersion { get; }
        DateTime? CreatedAt { get; }
        string? TitleOrFolderName { get; }
    }
}
