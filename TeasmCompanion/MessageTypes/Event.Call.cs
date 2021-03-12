using Newtonsoft.Json;
using System.Collections.Generic;

namespace TeasmCompanion.MessageTypes
{

    public class EventCallWrapper
    {
        public EventCall root { get; set; }
    }

    public class EventCall
    {
        public object ended { get; set; }
        public Partlist partlist { get; set; }
    }

    public class Partlist
    {
        [JsonProperty("@alt")]
        public string alt { get; set; }
        [JsonProperty("@count")]
        public string count { get; set; }
        public List<Part> part { get; set; }
    }

    public class Part
    {
        [JsonProperty("@identity")]
        public string identity { get; set; }
        public string name { get; set; }
        public string displayName { get; set; }
        public string duration { get; set; }
    }

}
