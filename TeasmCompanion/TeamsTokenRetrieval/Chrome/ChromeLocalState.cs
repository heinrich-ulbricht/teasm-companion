using Newtonsoft.Json;
using System.Collections.Generic;
using TeasmCompanion.Misc;
#nullable enable

namespace TeasmCompanion.TeamsTokenRetrieval.Chrome
{
    public class ChromeLocalState
    {
        public ProfileRoot? profile { get; set; }
    }

    public partial class ProfileRoot
    {
        public int? guest_profiles_created { get; set; }
        public Info_Cache? info_cache { get; set; }
        public List<string>? last_active_profiles { get; set; }
        public string? last_used { get; set; }
        public bool? picker_shown { get; set; }
        public string? profile_counts_reported { get; set; }
        public int? profiles_created { get; set; }
        public bool? show_picker_on_startup { get; set; }
    }

    [JsonConverter(typeof(StoreDynamicPropertyWithPrefixInCollection), "Profile ")]
    public class Info_Cache
    {
        public Profile? Default { get; set; }

        [JsonIgnore]
        [CollectionDictForPrefix("Profile ")]
        public Dictionary<string, Profile>? profiles { get; set; }

    }

    public partial class Profile
    {
        public int? active_time { get; set; }
        public string? avatar_icon { get; set; }
        public bool background_apps { get; set; }
        public bool force_signin_profile_locked { get; set; }
        public string? gaia_given_name { get; set; }
        public string? gaia_id { get; set; }
        public string? gaia_name { get; set; }
        public string? hosted_domain { get; set; }
        public bool is_consented_primary_account { get; set; }
        public bool is_ephemeral { get; set; }
        public bool is_using_default_avatar { get; set; }
        public bool is_using_default_name { get; set; }
        public string? managed_user_id { get; set; }
        public int metrics_bucket_index { get; set; }
        public string? name { get; set; }
        public string? shortcut_name { get; set; }
        public string? user_name { get; set; }
        public int default_avatar_fill_color { get; set; }
        public int default_avatar_stroke_color { get; set; }
        public int profile_highlight_color { get; set; }
        public bool use_gaia_picture { get; set; }
    }
}
