using MailKit;
using MimeKit;
using Newtonsoft.Json;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using TeasmCompanion.Interfaces;
using TeasmCompanion.Misc;

#nullable enable

namespace TeasmCompanion.Stores.Imap
{
    public class EmailBackedKeyValueStore
    {
        private readonly ILogger? logger;

        public struct Value<T>
        {
            public T AsObject { get; set; }
            public string AsString { get; set; }
        }

        public string? UniqueStoreName { get; private set; }
        public MimeMessageWithUniqueId MessageAndId { get; private set; }
        public EmailBackedKeyValueStore(ILogger? logger, string messageId, string uniqueStoreName)
        {
            UniqueStoreName = uniqueStoreName;
            var m = new MimeMessageWithUniqueId();
            m.Message = new MimeMessage
            {
                MessageId = messageId.EnsureContentIdFormat(),
                Date = new DateTime(2000, 01, 01),
                Subject = GetSubjectFromUniqueStoreName(uniqueStoreName)
            };
            m.Message.From.Add(new MailboxAddress(Constants.AppName, Constants.AppFakeEmail));
            m.Message.To.Add(new MailboxAddress(Constants.AppName, Constants.AppFakeEmail));
            m.Message.Headers.Add("heu-uniquestorename", uniqueStoreName);
            m.UniqueId = UniqueId.Invalid;

            MessageAndId = m;
            this.logger = logger;
        }

        public EmailBackedKeyValueStore(ILogger? logger, MimeMessage mimeMessage)
        {
            MessageAndId = new MimeMessageWithUniqueId
            {
                Message = mimeMessage,
                UniqueId = UniqueId.Invalid
            };
            this.logger = logger?.ForContext<EmailBackedKeyValueStore>();
        }

        public static string GetSubjectFromUniqueStoreName(string uniqueStoreName)
        {
            return $"{Constants.MetadataMessageSubjectPrefix} {uniqueStoreName}";
        }

        public EmailBackedKeyValueStore(ILogger logger, MimeMessageWithUniqueId? message, string? uniqueStoreName)
        {
            this.logger = logger;
            MessageAndId = message ?? throw new ArgumentNullException(nameof(message));

            UniqueStoreName = uniqueStoreName;
            if (uniqueStoreName != null)
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                message.Value.Message.Headers["heu-uniquestorename"] = uniqueStoreName;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }

#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8604 // Possible null reference argument.
            message.Value.Message.Subject = GetSubjectFromUniqueStoreName(UniqueStoreName);
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        }

        private T? GetMimePartForKey<T>(string key, Multipart? multipart) where T : MimeEntity
        {
            return (T?)multipart?.Where(me => me?.ContentId == key.EnsureContentIdFormat()).FirstOrDefault();
        }

        private T? GetMimePartForKey<T>(string key) where T : MimeEntity
        {
            return GetMimePartForKey<T>(key, (Multipart?)MessageAndId.Message?.Body);
        }

        public Value<T> GetOrCreateEmpty<T>(string key, Func<T>? getNonNullDefaultValue = null)
        {

            getNonNullDefaultValue ??= (() =>
            {
                if (typeof(T) == typeof(string))
                {
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                    return (T)Activator.CreateInstance(typeof(string), "".ToCharArray());
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8603 // Possible null reference return.
                }
                else
                if ((typeof(T).IsValueType))
                {
#pragma warning disable CS8603 // Possible null reference return.
                    return default;
#pragma warning restore CS8603 // Possible null reference return.
                }
                else
                {
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                    return (T)Activator.CreateInstance(typeof(T));
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8603 // Possible null reference return.
                }
            });
            var chatmetadata = GetMimePartForKey<MimePart>(key);
            if (chatmetadata != null)
            {
                using var mStream = new MemoryStream();
                chatmetadata.Content.WriteTo(mStream);
                mStream.Position = 0;
                using var reader = new StreamReader(mStream);
                var metadataString = reader.ReadToEnd();

                var deserializedObject = JsonUtils.DeserializeObject<T>(logger, metadataString);
                return new Value<T> { AsObject = deserializedObject, AsString = metadataString };
            }

            var defaultValue = getNonNullDefaultValue();
            return new Value<T> { AsObject = defaultValue, AsString = JsonConvert.SerializeObject(defaultValue) };
        }

        public Value<T?> GetOrDefault<T>(string key)
        {
            return GetOrCreateEmpty<T?>(key, () => default);
        }

        private Multipart GetInitializedMultipart()
        {
            if (MessageAndId.Message == null)
            {
                throw new ArgumentNullException(nameof(MessageAndId.Message), "Message must not be null");
            }

            if (MessageAndId.Message.Body is not Multipart multipart)
            {
                var textAndHtmlPart = new MultipartAlternative();
                textAndHtmlPart.ContentId = "textandhtmlcontent".EnsureContentIdFormat();
                // always reserve space for text and html content (could be optimized, but its easier like this)
                textAndHtmlPart.Add(new TextPart("plain") { Text = "" });
                textAndHtmlPart.Add(new TextPart("html") { Text = "" });

                multipart = new Multipart("mixed");
                multipart.Add(textAndHtmlPart);
                MessageAndId.Message.Body = multipart;
            }
            return multipart;
        }

        public MimePart? GetMultipartContent(string subType)
        {
            var multipart = GetInitializedMultipart();
            var multiPartAlternative = GetMimePartForKey<MultipartAlternative>("textandhtmlcontent");
            if (multiPartAlternative == null)
            {
                throw new TeasmCompanionException("Multipart alternativ must be set");
            }
            return multiPartAlternative.Where(value => value.ContentType.IsMimeType("text", subType)).FirstOrDefault() as MimePart;
        }

        public string? GetTextContent()
        {
            return (GetMultipartContent("plain") as TextPart)?.Text;
        }

        public string? GetHtmlContent()
        {
            return (GetMultipartContent("html") as TextPart)?.Text;
        }

        private void SetMultipartContent(string subType, string? value)
        {
            if (value == null)
            {
                return;
            }

            var multipart = GetInitializedMultipart();
            var multiPartAlternative = GetMimePartForKey<MultipartAlternative>("textandhtmlcontent");
            if (multiPartAlternative == null)
            {
                throw new TeasmCompanionException("Multipart alternativ must be set");
            }
            var textPart = multiPartAlternative.Where(value => value.ContentType.IsMimeType("text", subType)).FirstOrDefault();
            if (textPart != null)
                multiPartAlternative.Remove(textPart);
            // put text content up front to have html content displayed first (last mime part will be chosen)
            if (subType.ToLowerInvariant() == "plain")
                multiPartAlternative.Insert(0, new TextPart(subType) { Text = value }); else
                multiPartAlternative.Add(new TextPart(subType) { Text = value });
        }

        public void SetTextContent(string? value)
        {
            SetMultipartContent("plain", value);
        }

        public void SetHtmlContent(string? value)
        {
            SetMultipartContent("html", value);
        }


        public void SetJson(string key, string? jsonValue)
        {
            if (jsonValue == null)
            {
                return;
            }
            if (MessageAndId.Message == null)
            {
                throw new ArgumentNullException(nameof(MessageAndId.Message), "Message must not be null");
            }

            Multipart multipart = GetInitializedMultipart();

            string valueAsText = jsonValue;
            var mimePart = GetMimePartForKey<MimePart>(key);
            if (mimePart == null)
            {
                mimePart = new MimePart("application/json");
                multipart.Add(mimePart);
            }
            mimePart.Content = new MimeContent(new MemoryStream(Encoding.UTF8.GetBytes(valueAsText)), ContentEncoding.Default);
            mimePart.ContentId = key.EnsureContentIdFormat();
            MessageAndId.Message.Body = multipart;
        }

        public Value<T> Set<T>(string key, T value)
        {
            if (value == null)
            {
                return default;
            }

            string valueAsText = "";
            var count = 0;
            // this loop is a cheap way out of concurrency issues when user objects are modified in another thread... just try a few times
            while (true)
            {
                try
                {
                    valueAsText = JsonConvert.SerializeObject(value, Formatting.None, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    });
                    break;
                }
                catch
                {
                    if (count++ > 3)
                    {
                        break;
                    }
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
            }
            SetJson(key, valueAsText);
            return new Value<T>() { AsObject = value, AsString = valueAsText ?? "" };
        }

        public void AddAttachments(AttachmentCollection attachments)
        {
            if (MessageAndId.Message == null)
            {
                throw new ArgumentNullException(nameof(MessageAndId.Message), "Message must not be null");
            }

            Multipart multipart = GetInitializedMultipart();
            foreach (var a in attachments)
            {
                multipart.Add(a);
            }
        }
    }
}