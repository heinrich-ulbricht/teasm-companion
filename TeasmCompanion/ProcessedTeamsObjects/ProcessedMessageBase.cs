using HtmlAgilityPack;
using MimeKit.Utils;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using TeasmCompanion.Interfaces;
using TeasmCompanion.MessageTypes;
using TeasmCompanion.Misc;
using TeasmCompanion.Registries;
using TeasmCompanion.TeamsInternal.TeamsInternalApi.v1.users.me.conversations;
using TeasmCompanion.TeamsInternal.TeamsInternalApiAccessor;

#nullable enable

namespace TeasmCompanion.ProcessedTeamsObjects
{
    public abstract class ProcessedMessageBase : IChatMessage
    {
        [JsonProperty]
        protected TeamsDataContext? ctx;
        [JsonProperty]
        public string? MessageSubject { get; protected set; }
        [JsonProperty]
        public string? HtmlContent { get; protected set; }
        [JsonProperty]
        public string? TextContent { get; protected set; }
        [JsonProperty]
        public List<ProcessedTeamsUser> From { get; protected set; }
        [JsonProperty]
        public List<ProcessedTeamsUser> To { get; protected set; }
        [JsonProperty]
        public string? Id { get; protected set; }

        [JsonProperty]
        public string? ChatId { get; protected set; }

        [JsonProperty]
        public DateTime OriginalArrivalTime { get; protected set; }

        [JsonProperty]
        public string? Messagetype { get; protected set; }

        // key: CID, value: what has been replaced (URL?)
        [JsonProperty]
        public Dictionary<string, ImageInfo> ContentIds { get; private set; } = new Dictionary<string, ImageInfo>();

        protected readonly ILogger logger;
        protected readonly ITeamsUserRegistry teamsUserRegistry;

        protected abstract string? Internal_FromContactUrl { get; }
        protected abstract string? Internal_Subject { get; }
        protected abstract string? Internal_DisplayName { get; }
        // always set, initially to empty string
        protected abstract string Internal_Content { get; }
        protected abstract List<Mention>? Internal_Mentions { get; }

        public ProcessedMessageBase(ILogger logger, ITeamsUserRegistry teamsUserRegistry)
        {
            From = new List<ProcessedTeamsUser>();
            To = new List<ProcessedTeamsUser>();
            this.logger = logger.ForContext(GetType());
            this.teamsUserRegistry = teamsUserRegistry;
        }

        public abstract string SerializeOriginalMessageAsJson();

        public abstract Task<IChatMessage> InitFromMessageAsync<T>(TeamsDataContext ctx, string chatId, T message) where T : notnull, new();

        protected async Task ExtractSendersReceiversAndSubject(string chatId)
        {
            if (ctx == null || !ctx.HasValue)
            {
                return;
            }
            var nonNullContext = ctx.Value;


            var messageFromTeams = false;
            var messageFromMe = false;
            if (Internal_FromContactUrl?.EndsWith(chatId, StringComparison.InvariantCultureIgnoreCase) ?? false)
            {
                messageFromTeams = true;
            }

#pragma warning disable CS8604 // Possible null reference argument.
            if (Internal_FromContactUrl?.ToString().EndsWith(nonNullContext.Tenant.UserId, StringComparison.InvariantCultureIgnoreCase) ?? false)
#pragma warning restore CS8604 // Possible null reference argument.
            {
                messageFromMe = true;
            }

            // determine senders
            if (messageFromTeams)
            {
                var teamsChatUser = new ProcessedTeamsUser(nonNullContext, (TeamsParticipant)chatId);
                teamsChatUser.RegisterAlternateDisplayName(Constants.MicrosoftTeamsChatSenderName, OriginalArrivalTime);
                teamsChatUser.RegisterAlternateEmailAddress(Constants.MicrosoftTeamsChatSenderEmailAddress, OriginalArrivalTime);
                From.Add(teamsChatUser);
            }
            else
            {
                var userId = (TeamsParticipant)Internal_FromContactUrl;
                if (userId.IsValid)
                {
                    await teamsUserRegistry.RegisterDisplayNameForUserIdAsync(nonNullContext, userId, Internal_DisplayName, OriginalArrivalTime);
                    var user = await teamsUserRegistry.GetUserByIdOrDummyAsync(nonNullContext, userId);
                    if (user.HasDisplayName && user.HasEmailAddress)
                    {
                        From.Add(user);
                    }
                }
                else
                {
                    if (From.Count == 0)
                    {
                        // no valid user id? phew...
                        var dummyUser = new ProcessedTeamsUser(nonNullContext, userId);
                        dummyUser.RegisterAlternateDisplayName(Internal_DisplayName, OriginalArrivalTime);
                        From.Add(dummyUser);
                    }
                }
            }

            // determine receivers
            if (Internal_Mentions?.Count > 0)
            {
                foreach (var mention in Internal_Mentions)
                {
                    var userId = (TeamsParticipant)mention.mri;
                    var user = await teamsUserRegistry.GetUserByIdAsync(nonNullContext, userId, true);
                    if (user != null && user.HasDisplayName && user.HasEmailAddress)
                    {
                        To.Add(user);
                    }
                    else
                    {
                        var toUser = new ProcessedTeamsUser(nonNullContext, userId);
                        toUser.RegisterAlternateDisplayName(mention.displayName, OriginalArrivalTime);
                        toUser.RegisterAlternateEmailAddress(mention.IsChannelMention ? Constants.ChannelMentionEmailAddress : Constants.UnknownUserEmailAddress, OriginalArrivalTime);
                        To.Add(toUser);
                    }
                }
            }
            else
            {
                var toUser = new ProcessedTeamsUser(nonNullContext, TeamsParticipant.Null);
                toUser.RegisterAlternateDisplayName(Constants.UnknownReceiversDisplayName, OriginalArrivalTime);
                toUser.RegisterAlternateEmailAddress(Constants.UnknownUserEmailAddress, OriginalArrivalTime);
                To.Add(toUser);
            }

            // determine subject
            if (!string.IsNullOrWhiteSpace(Internal_Subject))
            {
                MessageSubject = Internal_Subject;
            }
            else
            {
                string displayName = Internal_DisplayName ?? "";
                if (displayName == null && (Messagetype != MessageType.Text || Messagetype != MessageType.RichText_Html))
                {
                    displayName = "Microsoft Teams";
                }

                await teamsUserRegistry.RegisterDisplayNameForUserIdAsync(nonNullContext, (TeamsParticipant)Internal_FromContactUrl, displayName, OriginalArrivalTime);
                displayName = await teamsUserRegistry.ReplaceUserIdsWithDisplayNamesAsync(nonNullContext, displayName);

                var messagePreview = "Message";
                if (Messagetype == MessageType.RichText_Html)
                {
                    // not sure if needed
                    string html = string.Format("<html><head></head><body>{0}</body></html>", Internal_Content);
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(html);
                    var body = doc.DocumentNode.SelectSingleNode("//body");
                    messagePreview = body.InnerText ?? "";
                }
                else if (Messagetype == MessageType.Text)
                {
                    messagePreview = Internal_Content ?? "";
                }
                messagePreview = HttpUtility.HtmlDecode(messagePreview).Trim(); // need to replace &nbsp; etc.
                if (messagePreview.Length > 100)
                {
                    messagePreview = messagePreview.Truncate(100) + "...";
                }
                else
                if (messagePreview.Length == 0)
                {
                    messagePreview = $"Message";
                }
                var truncatedDisplayName = displayName.Truncate(10)?.Trim() ?? "";
                MessageSubject = $"[{(messageFromMe ? "ME" : truncatedDisplayName)}] {messagePreview}";
            }
        }

        /// <summary>
        /// Generate placeholders for embedded images. The cid scheme is used to reference images (https://tools.ietf.org/html/rfc2392).
        /// </summary>
        /// <returns>Image info for retrieval</returns>
        protected Dictionary<string, ImageInfo> GenerateContentIdsForImageUrlsFromHtmlContent()
        {
            var result = new Dictionary<string, ImageInfo>();
            // cid:{0}
            if (HtmlContent != null)
            {
                var originalMessageContent = HtmlContent; // TODO: save this somewhere
                // not sure if needed
                string html = string.Format("<html><head></head><body>{0}</body></html>", HtmlContent);

                List<Tuple<HtmlNode, string>> nodesToReplaceWithTextSmiley = new List<Tuple<HtmlNode, string>>();

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(html);
                var imageNodes = doc.DocumentNode.SelectNodes("//img");
                if (imageNodes != null)
                {
                    foreach (var imageNode in imageNodes)
                    {
                        var imgUrl = imageNode.GetAttributeValue("src", "");
                        var textSmiley = MessageUtils.GetTextReplacementForImageUrl(imgUrl);
                        if (!string.IsNullOrWhiteSpace(textSmiley))
                        {
                            nodesToReplaceWithTextSmiley.Add(Tuple.Create(imageNode, textSmiley));
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(imgUrl))
                        {
                            continue;
                        }

                        var cid = MimeUtils.GenerateMessageId();
                        imageNode.SetAttributeValue("src", $"cid:{cid}");

                        var imageInfo = new ImageInfo(imgUrl, imgUrl, ImageType.Public);

                        // Handles URLs like this: src =\"https://eu-api.asm.skype.com/v1/objects/0-weu-d11-00000000000000000000000000000000/views/imgo\"
                        var teamsImageUrlPattern = @"http.*?/objects/([0-9a-zA-Z-]+?)/views/imgo";
                        var match = Regex.Match(HtmlContent, teamsImageUrlPattern);
                        if (match.Success)
                        {
                            imageInfo.CacheKey = match.Value;
                            imageInfo.ImageType = ImageType.TeamsWithAuthentication;
                        }

                        result.Add(cid, imageInfo);
                    }

                    foreach (var textReplacement in nodesToReplaceWithTextSmiley)
                    {
                        try
                        {
                            logger.Debug("Replacing image node with smiley {0} (HTML was: {1})", textReplacement.Item2, textReplacement.Item1.OuterHtml);
                            textReplacement.Item1.ParentNode.ReplaceChild(HtmlNode.CreateNode($"<span>{textReplacement.Item2}</span>"), textReplacement.Item1);
                        }
                        catch (Exception e)
                        {
                            logger.Error(e, "Exception while replacing image node with smiley");
                            continue;
                        }
                    }
                }
                var bodyElement = doc.DocumentNode.SelectSingleNode("//body");
                HtmlContent = bodyElement.InnerHtml;
            }
            return result;
        }

        protected void ReplaceImageUrlsByContentIds()
        {
            var replacedImageUrlInfo = GenerateContentIdsForImageUrlsFromHtmlContent();
            foreach (var e in replacedImageUrlInfo)
            {
                ContentIds.Add(e.Key, e.Value);
            }
        }

        /// <summary>
        /// Generate e-mail message content based on Teams message type and content. TODO: refactor into different methods or classes.
        /// </summary>
        protected async Task GenerateTextContentExtractUsersAndUpdateSubject()
        {
            if (ctx == null || !ctx.HasValue)
            {
                return;
            }
            var nonNullContext = ctx.Value;

            try {
                var textContent = new StringBuilder();
                if (Messagetype == MessageType.RichText_Html)
                {
                    HtmlContent = Internal_Content;
                }
                else
                if (Messagetype == MessageType.ThreadActivity_AddMember 
                        || (Messagetype == MessageType.ThreadActivity_MemberJoined && Internal_Content.StartsWith("<addmember>")) // there seem to be (old?) addmember messages that come with the wrong message type of ThreadActivity_MemberJoined... handle this here
                    )
                {
                    XmlDocument doc = new XmlDocument();
                    // need to force single XML values into array type since we know it also can be an array
                    doc.LoadXml($"<root xmlns:json='http://james.newtonking.com/projects/json'>{Internal_Content.Replace("<target", "<target json:Array='true'").Replace("<detailedtargetinfo", "<detailedtargetinfo json:Array='true'") ?? ""}</root>");
                    var json = JsonConvert.SerializeXmlNode(doc);
                    var data = JsonUtils.DeserializeObject<ThreadActivityAddMemberWrapper>(logger, json);

                    await Task.WhenAll(data.root.addmember.detailedtargetinfo?.Select(targetInfo => teamsUserRegistry.RegisterDisplayNameForUserIdAsync(nonNullContext, (TeamsParticipant)targetInfo.id, targetInfo.friendlyName, OriginalArrivalTime)) ?? new List<Task>());
                    if (!string.IsNullOrWhiteSpace(data.root.addmember.initiator))
                    {
                        await teamsUserRegistry.RegisterDisplayNameForUserIdAsync(nonNullContext, (TeamsParticipant)data.root.addmember.initiator, data.root.addmember.detailedinitiatorinfo?.friendlyName, OriginalArrivalTime);
                    }

                    // TODO: process alternate user display name
                    var memberNames = await Task.WhenAll(data.root.addmember.target.Select(t => teamsUserRegistry.GetDisplayNameForUserIdAsync(nonNullContext, (TeamsParticipant)t)));
                    MessageSubject = $"✈️ {await teamsUserRegistry.GetDisplayNameForUserIdAsync(nonNullContext, (TeamsParticipant)data.root.addmember.initiator)} added: " + string.Join(", ", memberNames);
                    textContent.Append(MessageSubject);
                }
                else
                if (Messagetype == MessageType.ThreadActivity_DeleteMember)
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml($"<root xmlns:json='http://james.newtonking.com/projects/json'>{Internal_Content.Replace("<target", "<target json:Array='true'").Replace("<detailedtargetinfo", "<detailedtargetinfo json:Array='true'")}</root>");
                    var json = JsonConvert.SerializeXmlNode(doc);
                    var data = JsonUtils.DeserializeObject<ThreadActivityDeleteMemberWrapper>(logger, json);

                    await Task.WhenAll(data.root.deletemember.detailedtargetinfo?.Select(targetInfo => teamsUserRegistry.RegisterDisplayNameForUserIdAsync(nonNullContext, (TeamsParticipant)targetInfo.id, targetInfo.friendlyName, OriginalArrivalTime)) ?? new List<Task>());
                    if (!string.IsNullOrWhiteSpace(data.root.deletemember.initiator))
                    {
                        await teamsUserRegistry.RegisterDisplayNameForUserIdAsync(nonNullContext, (TeamsParticipant)data.root.deletemember.initiator, data.root.deletemember.detailedinitiatorinfo?.friendlyName, OriginalArrivalTime);
                    }

                    var memberNames = await Task.WhenAll(data.root.deletemember.target.Select(t => teamsUserRegistry.GetDisplayNameForUserIdAsync(nonNullContext, (TeamsParticipant)t)));
                    MessageSubject = $"✈️ {await teamsUserRegistry.GetDisplayNameForUserIdAsync(nonNullContext, new TeamsParticipant(data.root.deletemember?.initiator))} removed: " + string.Join(", ", memberNames);
                    textContent.Append(MessageSubject);
                }
                else
                if (Messagetype == MessageType.Event_Call)
                {
                    XmlDocument doc = new XmlDocument();
                    // prepare XML to JSON conversion; force "part" being a list which cannot be infered from XML if there is only one element
                    doc.LoadXml($"<root xmlns:json='http://james.newtonking.com/projects/json'>{Internal_Content.Replace("<part ", "<part json:Array='true' ")}</root>");
                    var json = JsonConvert.SerializeXmlNode(doc);
                    var data = JsonUtils.DeserializeObject<EventCallWrapper>(logger, json);

                    await Task.WhenAll(data.root.partlist?.part?.Select(member => teamsUserRegistry.RegisterDisplayNameForUserIdAsync(nonNullContext, (TeamsParticipant)member.identity, member.displayName, OriginalArrivalTime)) ?? new List<Task>());
                    // sometimes p.name contains the user id and p.identity is empty
                    var memberNames = await Task.WhenAll(data.root.partlist?.part?.Select(p => teamsUserRegistry.GetDisplayNameForUserIdAsync(nonNullContext, TeamsParticipant.FromFirstValid(p.identity, p.name))) ?? new List<Task<string>>());
                    var callEnded = Internal_Content.Contains("<ended/>");
                    if (callEnded)
                    {
                        MessageSubject = "☎️ Call ended for: " + string.Join(", ", memberNames);
                    }
                    else
                    {
                        var from = (TeamsParticipant)Internal_FromContactUrl;
                        var displayName = await teamsUserRegistry.GetDisplayNameForUserIdAsync(nonNullContext, from);
                        MessageSubject = $"☎️ Call started by {displayName}";
                    }
                    textContent.Append(MessageSubject);
                }
                else
                if (Messagetype == MessageType.ThreadActivity_MemberJoined)
                {
                    var data = JsonUtils.DeserializeObject<ThreadEventMemberJoined>(logger, Internal_Content);

                    await Task.WhenAll(data.members.Select(member => teamsUserRegistry.RegisterDisplayNameForUserIdAsync(nonNullContext, (TeamsParticipant)member.id, member.friendlyname, OriginalArrivalTime)));
                    var memberNames = (await Task.WhenAll(data.members.Select(member => teamsUserRegistry.GetDisplayNameForUserIdAsync(nonNullContext, (TeamsParticipant)member.id))));
                    MessageSubject = "✈️ Member(s) joined: " + string.Join(", ", memberNames); // note: friendlyname is sometimes empty; second note: für ehemalige Mitarbeiter kann ein ID-Lookup fehlschlagen, aber der friendlyName dennoch gesetzt sein
                    textContent.Append(MessageSubject);
                }
                else
                if (Messagetype == MessageType.ThreadActivity_MemberLeft)
                {
                    var data = JsonUtils.DeserializeObject<ThreadEventMemberLeft>(logger, Internal_Content);

                    await Task.WhenAll(data.members.Select(member => teamsUserRegistry.RegisterDisplayNameForUserIdAsync(nonNullContext, (TeamsParticipant)member.id, member.friendlyname, OriginalArrivalTime)));
                    var memberNames = await Task.WhenAll(data.members.Select(member => teamsUserRegistry.GetDisplayNameForUserIdAsync(nonNullContext, (TeamsParticipant)member.id)));
                    MessageSubject = "✈️ Member(s) left: " + string.Join(", ", memberNames);
                    textContent.Append(MessageSubject);
                }
                else
                if (Messagetype == MessageType.RichText_Media_CallRecording)
                {
                    XmlDocument doc = new XmlDocument();
                    // prepare XML to JSON conversion; force "part" being a list which cannot be infered from XML if there is only one element
                    doc.LoadXml($"<root xmlns:json='http://james.newtonking.com/projects/json'>{Internal_Content.Replace("<Identifiers>", "<Identifiers json:Array='true'>").Replace("<RecordingContent ", "<RecordingContent json:Array='true' ").Replace("<RequestedExports ", "<RequestedExports json:Array='true' ")}</root>");
                    var json = JsonConvert.SerializeXmlNode(doc);
                    var data = JsonUtils.DeserializeObject<RichTextMedia_CallRecordingWrapper>(logger, json);

                    MessageSubject = $"✍️ Recording started by {await teamsUserRegistry.GetDisplayNameForUserIdAsync(nonNullContext, new TeamsParticipant(data.root?.URIObject?.RecordingInitiatorId?.value))}";
                    textContent.Append(MessageSubject);
                }
                else if (Messagetype == MessageType.Text)
                {
                    HtmlContent = Internal_Content;
                }
                else if (Messagetype == MessageType.ThreadActivity_TopicUpdate)
                {
                    XmlDocument doc = new XmlDocument();
                    // <topicupdate><eventtime>0000000000000</eventtime><initiator>8:orgid:00000000-0000-beef-0000-000000000000</initiator><value>New topic</value></topicupdate>
                    doc.LoadXml($"<root xmlns:json='http://james.newtonking.com/projects/json'>{Internal_Content}</root>");
                    var json = JsonConvert.SerializeXmlNode(doc);
                    var data = JsonUtils.DeserializeObject<ThreadActivityTopicUpdateWrapper>(logger, json);

                    var user = await teamsUserRegistry.GetUserByIdAsync(nonNullContext, (TeamsParticipant)data.root.topicupdate.initiator, false);
                    if (user != null && user.HasDisplayName)
                    {
                        if (From.Count == 1 && (From[0].UserId.Kind == ParticipantKind.TeamsChat || From[0].UserId.Kind == ParticipantKind.Unknown))
                        {
                            From.Clear();
                            From.Add(user);
                        }
                    }

                    MessageSubject = $"®️ Topic set to '{data.root.topicupdate.value}' by {await teamsUserRegistry.GetDisplayNameForUserIdAsync(nonNullContext, new TeamsParticipant(data.root.topicupdate.initiator))}";
                    textContent.Append(MessageSubject);
                }
                else
                {
                    textContent.Append("Unknown message type, don't know how to render: " + Messagetype);
                }

                if (string.IsNullOrWhiteSpace(HtmlContent))
                {
                    HtmlContent = MessageSubject;
                }
                TextContent = textContent.ToString();
            } catch (Exception e)
            {
                // exceptions here will cancel the whole chat from being parsed; log the message content to analyze it later
                logger.Error(e, "[{TenantName}] Exception while processing message content in method {MethodName}; Original message content: {MessageContent}", nonNullContext.Tenant.TenantName, nameof(GenerateTextContentExtractUsersAndUpdateSubject), SerializeOriginalMessageAsJson());
                // don't just skip a failed message, but cancel chat retrieval and fix the underlying issue, then try again
                throw;
            }
        }
    }
}