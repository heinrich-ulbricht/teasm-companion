using System;
using Newtonsoft.Json;
using TeasmCompanion.Interfaces;

namespace TeasmCompanion.Stores
{
    public class TeamsChatIndexEntry : IChatChangeInfo
    {
        [JsonProperty("id")]
        public string ChatId { get; set; }
        [JsonProperty("v")]
        public long Version { get; set; }
        [JsonProperty("tv")]
        public long ThreadVersion { get; set; }
        [JsonProperty("lmv")]
        public long LastMessageVersion { get; set; }
        [JsonProperty("ct")]
        public DateTime? CreatedAt { get; set; }
        [JsonProperty("s")]
        public string FolderName { get; set; }
        [JsonProperty("uiv")]
        public uint UidValidity { get; set; }

        [JsonIgnore]
        public string Id => ChatId;
        [JsonIgnore]
        public string TitleOrFolderName => FolderName;
    }
}
