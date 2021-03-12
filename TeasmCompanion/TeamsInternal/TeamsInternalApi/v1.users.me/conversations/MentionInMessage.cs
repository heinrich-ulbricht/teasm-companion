using Newtonsoft.Json;
using System.Numerics;

namespace TeasmCompanion.TeamsInternal.TeamsInternalApi.v1.users.me.conversations
{
    // http://schema.skype.com/Mention
    public class Mention
    {
        [JsonProperty("@type")]
        public string type { get; set; }
        public BigInteger itemid { get; set; }
        public string mri { get; set; }
        // channel, person, team
        public string mentionType { get; set; }
        public string displayName { get; set; }

        [JsonIgnore]
        public bool IsPersonMention { get { return type == "person"; } }
        [JsonIgnore]
        public bool IsChannelMention { get { return type == "channel"; } }
    }

}
