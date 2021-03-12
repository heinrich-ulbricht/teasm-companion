using System.Collections.Generic;

namespace TeasmCompanion.TeamsInternal.TeamsInternalApi.v2.users.me.endpoints
{
    public class Userpinnedapps
    {
        public string version { get; set; }
        public List<Userpinnedapp> pinnedAndCoreApps { get; set; }
        public List<Userpinnedapp> userPinnedAppBarOrder { get; set; }
        public bool setupPolicyOverridesUserPinnedOrder { get; set; }
        public bool userPinningAllowed { get; set; }
        public string lastComputedUserEntitlenmentHash { get; set; }
    }

    public class Userpinnedapp
    {
        public string id { get; set; }
        public string name { get; set; }
        public string smallImageUrl { get; set; }
        public bool isCoreApp { get; set; }
        public bool isAppBarPinned { get; set; }
        public int appBarOrder { get; set; }
        public string state { get; set; }
    }
}
