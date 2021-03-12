using MimeKit;
using Serilog;
using System;
using System.Collections.Generic;
using TeasmCompanion.Interfaces;
using TeasmCompanion.TeamsInternal.TeamsInternalApi.v1.users.me.conversations;

#nullable enable

namespace TeasmCompanion.Stores.Imap
{
    public class MimeMessageWrapper : IMutableChatMessage
    {
        private readonly Message? originalMessage;
        private readonly MimeMessage mimeMessage;
        private EmailBackedKeyValueStore kvStore;

        public MimeMessageWrapper(ILogger logger, MimeMessage mimeMessage)
        {
            this.mimeMessage = mimeMessage;
            kvStore = new EmailBackedKeyValueStore(logger, mimeMessage);
            try
            {
                originalMessage = kvStore.GetOrDefault<Message>("heu-originalMessage").AsObject;
            }
            catch (Exception e)
            {
                logger.Debug(e, "Cannot get original message from mime message");
            };
        }

        public string? Id => originalMessage?.id;

        public string? ChatId => mimeMessage.Headers["heu-chatid"];

        public DateTime OriginalArrivalTime => originalMessage?.originalarrivaltime ?? Utils.JavaScriptUtcMsToDateTime(long.Parse(originalMessage?.version ?? "0"));

        public List<ProcessedTeamsUser> From => throw new NotImplementedException();

        public List<ProcessedTeamsUser> To => throw new NotImplementedException();

        public Dictionary<string, ProcessedTeamsObjects.ImageInfo> ContentIds => throw new NotImplementedException();

        public string? MessageSubject
        {
            get => mimeMessage.Subject;
            set => mimeMessage.Subject = value;
        }
        public string? HtmlContent
        {
            get => kvStore.GetHtmlContent();
            set => kvStore.SetHtmlContent(value);
        }
        public string? TextContent
        {
            get => kvStore.GetTextContent();
            set => kvStore.SetTextContent(value);
        }

        public string SerializeOriginalMessageAsJson()
        {
            throw new NotImplementedException();
        }
    }
}
