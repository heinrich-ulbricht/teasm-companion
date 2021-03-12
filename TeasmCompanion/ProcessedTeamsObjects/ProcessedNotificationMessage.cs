using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TeasmCompanion.Interfaces;
using TeasmCompanion.ProcessedTeamsObjects;
using TeasmCompanion.TeamsInternal.TeamsInternalApi.api.csa.api.v1.teams.users;
using TeasmCompanion.TeamsInternal.TeamsInternalApi.v1.users.me.conversations;
using TeasmCompanion.TeamsInternal.TeamsInternalApi.v2.users.me.endpoints;

#nullable enable

namespace TeasmCompanion.TeamsInternal.TeamsInternalApiAccessor
{
    public interface IProcessedNotificationMessageFactory
    {
        ProcessedNotificationMessage CreateProcessedNotificationMessage();
    }

    /// <summary>
    /// Represents a message retrieved via notification.
    /// </summary>
    public class ProcessedNotificationMessage : ProcessedMessageBase
    {
        [JsonProperty]
        private Resource? notificationResource;
        // like https://notifications.skype.net/v1/users/ME/contacts/8:orgid:00000000-0000-beef-0000-000000000000
        protected override string? Internal_FromContactUrl => notificationResource?.from;
        protected override string? Internal_Subject => notificationResource?.threadtopic;
        protected override string? Internal_DisplayName => notificationResource?.imdisplayname;
        protected override string Internal_Content => notificationResource?.content ?? "";
        protected override List<Mention>? Internal_Mentions => new List<Mention>() {  }; // TBD

        public ProcessedNotificationMessage(ILogger logger, ITeamsUserRegistry teamsUserRegistry) : base(logger, teamsUserRegistry)
        {
        }

        public override async Task<IChatMessage> InitFromMessageAsync<T>(TeamsDataContext ctx, string chatId, T message)
        {
            if (message is not Resource m)
            {
                throw new ArgumentException($"Cannot init {nameof(ProcessedMessage)} from type {message.GetType()}", nameof(message));
            }

            notificationResource = m;
            this.ctx = ctx;
            Messagetype = m.messagetype;
            Id = m.id; // note: this is the same ID as the one of the "real" message retrieved via the messages endpoint
            ChatId = m.to;
            OriginalArrivalTime = m.originalarrivaltime ?? Utils.JavaScriptUtcMsToDateTime(long.Parse(m.version ?? "0"));

            await ExtractSendersReceiversAndSubject(chatId);
            await GenerateTextContentExtractUsersAndUpdateSubject();
            ReplaceImageUrlsByContentIds();
            return this;
        }

        public override string SerializeOriginalMessageAsJson()
        {
            return JsonConvert.SerializeObject(notificationResource, Formatting.None, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
        }
    }
}
