using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using TeasmCompanion.Interfaces;
using TeasmCompanion.Misc;
using TeasmCompanion.Registries;
using TeasmCompanion.TeamsInternal.TeamsInternalApi.api.csa.api.v1.teams.users;

namespace TeasmCompanion
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ProcessedChat : IChatChangeInfo
    {
        [JsonProperty]
        private Chat chat { get; set; }
        [JsonProperty]
        public string ChatTitle { get; set; }
        [JsonProperty]
        public List<TeamsParticipant> UserIds { get; private set; }
        [JsonProperty]
        public long Version { get; private set; }
        [JsonProperty]
        public long ThreadVersion { get; private set; }
        [JsonProperty]
        public long LastMessageVersion { get; private set; }
        [JsonProperty]
        public string Id { get; private set; }
        public string TitleOrFolderName => ChatTitle;

        public IOrderedEnumerable<IChatMessage> OrderedMessages { get; set; }
        public DateTime? CreatedAt => ((IChatChangeInfo)chat).CreatedAt;

        public ProcessedChat(Chat chat)
        {
            UserIds = new List<TeamsParticipant>();
            this.chat = chat;
            if (!long.TryParse(chat.LastMessage?.version, out var lastMessageVersion))
            {
                lastMessageVersion = Constants.MissingVersionIndicator;
            }
            Version = chat.version;
            ThreadVersion = chat.threadVersion;
            LastMessageVersion = lastMessageVersion;
            Id = chat.id;
        }

        public object GetDebugLogSummary()
        {
            return new
            {
                ChatTitle = ChatTitle,
                ThreadVersion = Utils.JavaScriptUtcMsToDateTime(ThreadVersion),
                Version = Utils.JavaScriptUtcMsToDateTime(Version),
                LastMessageTime = chat.LastMessage?.composetime,
                MessageCount = OrderedMessages.Count()
            };
        }
    }
}
