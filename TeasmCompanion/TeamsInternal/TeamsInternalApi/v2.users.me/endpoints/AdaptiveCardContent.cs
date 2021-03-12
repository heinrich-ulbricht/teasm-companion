using System.Collections.Generic;

namespace TeasmCompanion.TeamsInternal.TeamsInternalApi.v2.users.me.endpoints
{
    public class Body
    {
        public List<Column> columns { get; set; }
        public string spacing { get; set; }
        public bool separator { get; set; }
        public string type { get; set; }
    }

    public class Column
    {
        public string width { get; set; }
        public List<Item> items { get; set; }
        public string type { get; set; }
        public string spacing { get; set; }
    }

    public class Item
    {
        public string altText { get; set; }
        public string horizontalAlignment { get; set; }
        public string url { get; set; }
        public string type { get; set; }
        public bool isSubtle { get; set; }
        public string size { get; set; }
        public string text { get; set; }
        public bool wrap { get; set; }
        public string weight { get; set; }
        public string spacing { get; set; }
    }
}
