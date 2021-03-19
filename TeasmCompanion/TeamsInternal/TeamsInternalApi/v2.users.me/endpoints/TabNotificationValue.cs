using System;
using System.Numerics;

namespace TeasmCompanion.TeamsInternal.TeamsInternalApi.v2.users.me.endpoints
{
    public partial class TabNotificationValue
    {
        public string name { get; set; }
        public string id { get; set; }
        public string definitionId { get; set; }
        // "tab:"
        public string type { get; set; }
        // "tab:"
        public string tabType { get; set; }
        public Settings settings { get; set; }
        // "extension-tab"
        public string directive { get; set; }
        // this can be string or double(??) like 10000.0
        public string order { get; set; }
        public string resourceId { get; set; }
        public string replyChainId { get; set; }
        public string externalId { get; set; }
        // "True" (yes, uppercase); set for e.g. Planner tab
        public string isUnconfigured { get; set; }
    }

    public partial class Settings
    {
        // "excelpin"
        public string subtype { get; set; }
    }

    // subtype "excelpin" / definitionId "com.microsoft.teamspace.tab.file.staticviewer.excel"
    public partial class Settings
    {
        public string objectId { get; set; }
        // encoded JSON with file info
        public string file { get; set; }
    }

    // subtype "wiki-tab" / definitiondId "com.microsoft.teamspace.tab.wiki"
    public partial class Settings
    {
        public BigInteger wikiTabId { get; set; }
        public bool wikiDefaultTab { get; set; }
        public bool hasContent { get; set; }
        public bool isPrivateMeetingWiki { get; set; }
        public bool meetingNotes { get; set; }
        // "wiki_init_context"
        public string scenarioName { get; set; }
    }

    // subtype "webpage"
    public partial class Settings
    {
        public string url { get; set; }
        public string websiteUrl { get; set; }
        public DateTime dateAdded { get; set; }
    }

    // subtype "extension"; also has properties of subtype "webpage"
    public partial class Settings
    {
        public string name { get; set; }
        public string removeUrl { get; set; }
        // "/providers/Microsoft.PowerApps/apps/00000000-0000-beef-0000-000000000000"
        public string entityId { get; set; }
    }

    // subtype "sharepointfiles"
    public partial class Settings
    {
        public string siteUrl { get; set; }
        public string libraryServerRelativeUrl { get; set; }
        public string libraryId { get; set; }
        public string selectedDocumentLibraryTitle { get; set; }
        // "/sites/A_Team/_api/GroupService/GetGroupImage?id='00000000-0000-beef-0000-000000000000'&hash=11111111111111"
        public string selectedSiteImageUrl { get; set; }
        public string selectedSiteTitle { get; set; }
        // public DateTime dateAdded { get; set; }
    }

    // for meeting tabs like "tab::19:meeting_someid@thread.skype"
    public partial class Settings
    {
        public BigInteger meetingNotesPageId { get; set; }
        public string sharepointPath { get; set; }
    }
}
