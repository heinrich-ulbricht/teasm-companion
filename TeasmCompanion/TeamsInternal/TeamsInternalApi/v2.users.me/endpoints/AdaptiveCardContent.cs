﻿using System.Collections.Generic;

namespace TeasmCompanion.TeamsInternal.TeamsInternalApi.v2.users.me.endpoints
{
    public partial class AdaptiveCardBody
    {
        public List<Column> columns { get; set; }
        public string spacing { get; set; }
        public bool separator { get; set; }
        // "Container", ...
        public string type { get; set; }
    }

    public class Column
    {
        public string width { get; set; }
        public List<Item> items { get; set; }
        public string type { get; set; }
        public string spacing { get; set; }
        public string verticalContentAlignment { get; set; }
    }

    public partial class Item
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

    public partial class Item
    {
        public string width { get; set; }
        public string height { get; set; }
        public List<Column> columns { get; set; }
        public string id { get; set; }
    }

    public partial class AdaptiveCardBody
    {
        public List<BodyElement> items { get; set; }
        public SelectAction selectAction { get; set; }
        public string id { get; set; }
    }

    public class SelectAction
    {
        public string url { get; set; }
        public string title { get; set; }
        public string type { get; set; }
    }


    public class BodyElement
    {
        public List<Column> columns { get; set; }
        public SelectAction selectAction { get; set; }
        public string id { get; set; }
        public string spacing { get; set; }
        public bool separator { get; set; }
        public string height { get; set; }
        public string type { get; set; }
        public List<Item> items { get; set; }
        public string text { get; set; }
        public bool wrap { get; set; }
    }
}
