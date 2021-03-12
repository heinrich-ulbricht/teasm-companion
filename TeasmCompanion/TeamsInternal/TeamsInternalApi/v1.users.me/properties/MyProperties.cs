using Newtonsoft.Json;
using System.Collections.Generic;
using TeasmCompanion.TeamsInternal.TeamsInternalApi.v2.users.me.endpoints;
using TeasmCompanion.v1.users.me.properties;

namespace TeasmCompanion.TeamsInternal.TeamsInternalApi.v1.users.me.properties
{
    public class MyProperties
    {
        [JsonConverter(typeof(EmbeddedLiteralConverter<Userpinnedapps>))]
        public Userpinnedapps userPinnedApps { get; set; }
        public string freSent { get; set; }
        [JsonConverter(typeof(EmbeddedLiteralConverter<PersonalFileSite>))]
        public PersonalFileSite personalFileSite { get; set; }
        [JsonConverter(typeof(EmbeddedLiteralConverter<UserPersonalSettings>))]
        public UserPersonalSettings userPersonalSettings { get; set; }
        [JsonConverter(typeof(EmbeddedLiteralConverter<List<TeamOrder>>))]
        public List<TeamOrder> teamsOrder { get; set; }
        public string readReceiptsEnabled { get; set; }
        [JsonConverter(typeof(EmbeddedLiteralConverter<PersonalSharedNotebook>))]
        public PersonalSharedNotebook personalSharedNoteBook { get; set; }
        public string isSkypeTeamsUserSetInSettingsStore { get; set; }
        [JsonConverter(typeof(EmbeddedLiteralConverter<UserDetails>))]
        public UserDetails userDetails { get; set; }
        public string mobileNudgeSent { get; set; }
        public string suggestedRepliesSettings { get; set; }
        [JsonConverter(typeof(EmbeddedLiteralConverter<Dictionary<string, dynamic>>))]
        public Dictionary<string, dynamic> favorites { get; set; }
        public string firstLoginInformation { get; set; }
        public string cortanaSettings { get; set; }
        public string locale { get; set; }
        public string contactsTabLastVisitTime { get; set; }
        public string autoUnfavoriteTeams { get; set; }
        public string bannerDismissalCounts { get; set; }
        public string cid { get; set; }
        public string cidHex { get; set; }
        public bool dogfoodUser { get; set; }
        public string primaryMemberName { get; set; }
        public string skypeName { get; set; }
    }

}
