using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using TeasmCompanion.Interfaces;
using TeasmCompanion.ProcessedTeamsObjects;
using TeasmCompanion.TeamsInternal.TeamsInternalApi.v1.users.me.conversations;
using TeasmCompanion.TeamsInternal.TeamsInternalApiAccessor;

#nullable enable

namespace TeasmCompanion
{
    /// <summary>
    /// Ninject factory
    /// </summary>
    public interface IProcessedMessageFactory
    {
        ProcessedMessage CreateProcessedMessage();
    }

    /// <summary>
    /// Represents a message retrieved via the message retrieval endpoint.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class ProcessedMessage : ProcessedMessageBase
    {
        [JsonProperty]
        private Message? originalMessage;

        protected override string? Internal_FromContactUrl => originalMessage?.from;
        protected override string? Internal_Subject => originalMessage?.properties?.subject;
        protected override string? Internal_DisplayName => originalMessage?.imdisplayname;
        protected override string Internal_Content => originalMessage?.content ?? "";
        protected override List<Mention>? Internal_Mentions => originalMessage?.properties?.mentions;

        public ProcessedMessage(ILogger logger, ITeamsUserRegistry teamsUserRegistry) : base(logger, teamsUserRegistry)
        {
        }

        public override async Task<IChatMessage> InitFromMessageAsync<T>(TeamsDataContext ctx, string chatId, T message)
        {
            if (message is not Message m)
            {
                throw new ArgumentException($"Cannot init {nameof(ProcessedMessage)} from type {message.GetType()}", nameof(message));
            }

            originalMessage = m;
            this.ctx = ctx;
            Messagetype = m.messagetype;
            Id = m.id;
            ChatId = m.conversationid;
            OriginalArrivalTime = m.originalarrivaltime ?? Utils.JavaScriptUtcMsToDateTime(long.Parse(m.version));

            await ExtractSendersReceiversAndSubject(chatId);
            await GenerateTextContentExtractUsersAndUpdateSubject();
            ConvertFileCardsToHtmlImages();
            ReplaceImageUrlsByContentIds();
            return this;
        }

        protected void ConvertFileCardsToHtmlImages()
        {
            if (originalMessage?.properties?.files?.Count > 0)
            {
                foreach (var f in originalMessage?.properties?.files ?? new List<MessageFile>())
                {
                    var fileUrl = f.fileInfo?.fileUrl;
                    var previewUrl = f.filePreview?.previewUrl;
                    var fileType = f.fileType; // json, png ...
                    var fileName = f.title;

                    var attachmentHtml = "";
                    if ((previewUrl?.Contains("/views/imgo") /* <- teams image retrievable with authentication */ ?? false) || fileType == "png" || fileType == "gif" || fileType == "jpeg" || fileType == "jpg")
                    {
                        attachmentHtml = $"<div><a href=\"{fileUrl}\"><img src=\"{previewUrl}\" alt=\"{fileName ?? "unspecified file name"}\" data-filetype=\"{fileType ?? "unspecified file type"}\"></a><div>";
                        // teams image retrievable with authentication
                    }
                    else
                    {
                        attachmentHtml = $"<div><a href=\"{fileUrl}\" data-filetype=\"{fileType ?? "unspecified file type"}\">{fileName}</a><div>";
                    }

                    if (!string.IsNullOrWhiteSpace(attachmentHtml))
                    {
                        if (Messagetype == MessageType.RichText_Html)
                        {
                            HtmlContent += attachmentHtml;
                        }
                        else if (Messagetype == MessageType.Text)
                        {
                            if (string.IsNullOrWhiteSpace(HtmlContent))
                            {
                                HtmlContent += "<div>" + HttpUtility.HtmlEncode(originalMessage?.content ?? "") + "</div>";
                            }
                            HtmlContent += attachmentHtml;
                            Messagetype = MessageType.RichText_Html; // TODO: think about this - is changing the original message type a good idea?
                        }
                        else
                        {
                            throw new TeasmCompanionException("Unknown condition, handle me!");
                        }
                    }
                }
            }
        }

        public override string SerializeOriginalMessageAsJson()
        {
            return JsonConvert.SerializeObject(originalMessage, Formatting.None, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
        }
    }
}
