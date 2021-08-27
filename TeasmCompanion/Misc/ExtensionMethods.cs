using MailKit;
using MailKit.Search;
using System;
using System.Threading.Tasks;
using System.Linq;
using TeasmCompanion.ProcessedTeamsObjects;
using MimeKit;
using System.Reactive.Linq;
using System.Collections.Generic;
using TeasmCompanion.Registries;
using System.Text;
using TeasmCompanion.Misc;
using TeasmCompanion.Interfaces;
using System.IO;

#nullable enable

namespace TeasmCompanion
{
    public static class ExtensionMethods
    {
        public static string Replace(this string s, char[] separators, string newVal)
        {
            string[] temp;

            temp = s.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(newVal, temp);
        }

        public static string MakeSafeFolderName(this string? s, char directorySeparator)
        {
            return s?.Replace(directorySeparator, '_').Replace('/', '_') ?? string.Empty;
        }

        public static string MakeSafeFolderName(this IMailFolder folder, string? s)
        {
            return s.MakeSafeFolderName(folder.DirectorySeparator);
        }

        public static async Task<(UniqueId, IMailFolder?)> FindIdByMessageIdHeader(this IMailFolder folder, string? messageIdHeaderValue, bool recursive, bool skipParent, bool onlyImmediateChildren = false)
        {
            if (messageIdHeaderValue == null)
            {
                return (UniqueId.Invalid, null);
            }

            await folder.OpenAsync(FolderAccess.ReadOnly);
            if (!skipParent)
            {
                var hit = (await folder.SearchAsync(SearchQuery.HeaderContains("Message-ID", messageIdHeaderValue))).FirstOrDefault(); // sometimes there are more than one if the companion got terminated; ignore this, think later about handling this
                if (hit.IsValid)
                    return (hit, folder);
            }

            var subfolders = await folder.GetSubfoldersAsync(false);
            foreach (var subFolder in subfolders)
            {
                var hit2 = await subFolder.FindIdByMessageIdHeader(messageIdHeaderValue, recursive && onlyImmediateChildren, false, onlyImmediateChildren);
                if (hit2.Item1.IsValid)
                {
                    return hit2;
                }
            }

            return (UniqueId.Invalid, null);
        }

        public static long ToJavaScriptMilliseconds(this DateTime? dateTime)
        {
            if (dateTime == null)
            {
                return DateTime.MinValue.ToJavaScriptUtcMilliseconds();
            }
            else
            {
                return dateTime.Value.ToJavaScriptUtcMilliseconds();
            }
        }

        public static long ToJavaScriptUtcMilliseconds(this DateTime dateTime)
        {
            return (long)(dateTime.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        }

        public static string? Truncate(this string? s, int length, bool dotDotDot = false)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= length)
                return s;

            length = Math.Min(s.Length, length);
            if (!dotDotDot || length < 4)
            {
                return s.Substring(0, length - 1);
            } else
            {
                int index1 = length / 2;
                int index2 = s.Length-(length / 2);
                return s.Substring(0, index1-1) + "~" + s.Substring(index2+1);
            }
        }

        public static string? Truncate(this TeamsParticipant userId, int length, bool dotDotDot = false)
        {
            return ((string?)userId)?.Truncate(length, dotDotDot);
        }

        public static bool IsMainUser(this ProcessedUser props)
        {
            return !(props.Properties.userDetails?.upn?.Contains("#EXT#") ?? true);
        }

        public static string EnsureContentIdFormat(this string? s)
        {
            if (s == null)
            {
                return "";
            }

            if (s.EndsWith($"@{Constants.AppDomain}"))
                return s;
            else
                return $"\"{s.Replace('"', '\'')}\"@{Constants.AppDomain}";
        }

        public static async Task<(MimeMessage?, UniqueId)> GetMessageOrDefaultAsync(this IMailFolder folder, UniqueId id, MimeMessage? defaultValue = null)
        {
            try
            {
                return (await folder.GetMessageAsync(id), id);
            } catch (MessageNotFoundException)
            {
                return (null, UniqueId.Invalid);
            }
        }

        public static void Dump<T>(this IObservable<T> source, string name)
        {
            source.Subscribe(
            i => Console.WriteLine("{0}-->{1}", name, i),
            ex => Console.WriteLine("{0} failed-->{1}", name, ex.Message),
            () => Console.WriteLine("{0} completed", name));
        }

        public static IObservable<TResult> If<TSource, TResult>(
          this IObservable<TSource> source,
          Func<TSource, bool> predicate,
          Func<TSource, IObservable<TResult>> thenSource,
          Func<TSource, IObservable<TResult>> elseSource)
        {
            return source
              .SelectMany(
                value => predicate(value)
                  ? thenSource(value)
                  : elseSource(value));
        }

        public static void AddRange<T>(this HashSet<T> set, IEnumerable<T> otherValues)
        {
            if (otherValues == default)
                return;

            foreach (var v in otherValues)
            {
                set.Add(v);
            }
        }

        public static bool ContainsAll<T>(this HashSet<T> set, HashSet<T> otherValues)
        {
            if (otherValues == default)
                return true;

            int hitCount = 0;
            foreach (var v in otherValues)
            {
                if (set.Contains(v))
                    hitCount++;
            }
            return hitCount == otherValues.Count();
        }

        public static string? StartsWithAny(this string? value, IEnumerable<string> prefixes, StringComparison comparisonType)
        {
            if (value == null)
                return null;

            return prefixes.Where(p => value.StartsWith(p, comparisonType)).FirstOrDefault();
        }

        public static string? FromBase64String(this string? encodedValue)
        {
            if (string.IsNullOrWhiteSpace(encodedValue))
                return encodedValue;

            byte[] data = Convert.FromBase64String(encodedValue);
            return Encoding.UTF8.GetString(data);
        }

        public enum VersionSource
        {
            Chat,
            LastMessage
        }

        /// <summary>
        /// Get the version of the last conversation in this chat. This might be different from the "real" thread version which includes
        /// also events like "member removed". But for e.g. retrieving the last messages those events are not important.
        /// </summary>
        /// <param name="chat">Chat to get version for</param>
        /// <returns>Version</returns>
        public static (VersionSource, long) GetLastMessageVersionWithLogic(this IChatChangeInfo? chat)
        {
            if (chat == null)
                return (VersionSource.Chat, Constants.MissingVersionIndicator);

            if (chat.LastMessageVersion > Constants.MissingVersionIndicator)
                return (VersionSource.LastMessage, chat.LastMessageVersion);

            // at this point there is no message in the chat and thus it's rather unimportant; so choose the creation time, if there is any
            var createdAtVersion = chat.CreatedAt.ToJavaScriptMilliseconds();
            if (createdAtVersion > Constants.MissingVersionIndicator) {
                return (VersionSource.Chat, createdAtVersion);
            }

            return (VersionSource.Chat, chat.Version);
        }

        public static string WriteToString(this MimePart? mimePart, Encoding encoding)
        {
            using var stream = new MemoryStream();
            mimePart?.WriteTo(stream);
            return encoding.GetString(stream.ToArray());
        }

        public static long Add(this long value, TimeSpan amount)
        {
            return value + (long)amount.TotalMilliseconds;
        }
    }
}
