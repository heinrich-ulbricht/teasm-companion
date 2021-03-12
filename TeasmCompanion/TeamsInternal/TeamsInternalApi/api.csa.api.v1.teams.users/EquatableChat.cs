using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace TeasmCompanion.TeamsInternal.TeamsInternalApi.api.csa.api.v1.teams.users
{
    public class HigherVersionWinsComparerChat : IComparable<HigherVersionWinsComparerChat>
    {
        public Chat? Chat { get; }

        public HigherVersionWinsComparerChat(Chat chat)
        {
            Chat = chat;
        }

        // about ordering chats by time:
        // - order so that the highest version is retrieved first, but chats with last message info are always retrieved before chats without this info
        // - note: in an ideal world all chat messages would have last message infos and we would use those, but older chats seem to miss this information
        // - note: mass updates of chats e.g. by a person leaving the company are a problem; dozens of old chats get a new version when somebody leaves
        public int CompareTo([AllowNull] HigherVersionWinsComparerChat other)
        {
            if (other == null)
                return 1;

            if (other.Chat == null && Chat == null)
                return 0;

            if (other.Chat != null && Chat == null)
                return -1;

            if (other.Chat == null && Chat != null)
                return 1;


#pragma warning disable CS8602 // Dereference of a possibly null reference.
            var (thisVersionSource, thisVersion) = Chat.GetLastMessageVersionWithLogic();
            var (otherVersionSource, otherVersion) = other.Chat.GetLastMessageVersionWithLogic();

            if (thisVersionSource == otherVersionSource)
            {
                return thisVersion == otherVersion ? 0 : thisVersion > otherVersion ? 1 : -1;
            }
            // the one with last message is retrieved first; all other chats are old
            if (thisVersionSource == ExtensionMethods.VersionSource.LastMessage && otherVersionSource != ExtensionMethods.VersionSource.LastMessage)
                return 1;
            if (thisVersionSource != ExtensionMethods.VersionSource.LastMessage && otherVersionSource == ExtensionMethods.VersionSource.LastMessage)
                return -1;
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            return 0;
        }
    }

    public class LowerVersionWinsChatComparer : IComparer<HigherVersionWinsComparerChat>
    {
        public int Compare([AllowNull] HigherVersionWinsComparerChat x, [AllowNull] HigherVersionWinsComparerChat y)
        {
            if (x == null && y == null)
                return 0;

            if (x != null && y == null)
                return -1;

            if (x == null && y != null)
                return 1;

#pragma warning disable CS8602 // Dereference of a possibly null reference.
            return y.CompareTo(x);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        }
    }
}
