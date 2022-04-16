using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Linq;
using TeasmCompanion.Registries;
using TeasmCompanion.TeamsTokenRetrieval;

namespace TeasmCompanion.Test
{
    [TestClass]
    public class TestTeamsTokenPathes : TestBase
    {
        [TestMethod]
        public void TestParticipantCreation()
        {
            var json = @"
{
    ""autofill"": {
    },
    ""browser"": {
        ""last_redirect_origin"": """",
        ""shortcut_migration_version"": ""89.0.4343.0""
    },
    ""chrome_cleaner"": {
        ""scan_completion_time"": ""0""
    },
    ""data_use_measurement"": {
        ""data_used"": {
            ""services"": {
                ""background"": {},
                ""foreground"": {}
            },
            ""user"": {
                ""background"": {},
                ""foreground"": {}
            }
        }
    },
    ""hardware_acceleration_mode_previous"": true,
    ""intl"": {
        ""app_locale"": ""de""
    },
    ""legacy"": {
        ""profile"": {
            ""name"": {
                ""migrated"": true
            }
        }
    },
    ""network_time"": {
        ""network_time_mapping"": {
        }
    },
    ""origin_trials"": {
        ""disabled_features"": [
        ]
    },
    ""os_crypt"": {
    },
    ""password_manager"": {
    ""os_password_blank"": false,
        ""os_password_last_changed"": ""0""
    },
    ""plugins"": {
    ""metadata"": {
        ""x-version"": 62
        },
        ""resource_cache_update"": ""0""
    },
    ""policy"": {
    ""last_statistics_update"": ""0""
    },
    ""privacy_budget"": {
    ""generation"": 0,
        ""randomizer_seed"": ""0""
    },
    ""profile"": {
    ""guest_profiles_created"": 5,
        ""info_cache"": {
        ""Default"": {
            ""active_time"": 0,
                ""avatar_icon"": ""chrome://theme/IDR_PROFILE_AVATAR_26"",
                ""background_apps"": false,
                ""force_signin_profile_locked"": false,
                ""gaia_given_name"": """",
                ""gaia_id"": """",
                ""gaia_name"": """",
                ""hosted_domain"": """",
                ""is_consented_primary_account"": false,
                ""is_ephemeral"": false,
                ""is_using_default_avatar"": true,
                ""is_using_default_name"": true,
                ""managed_user_id"": """",
                ""metrics_bucket_index"": 1,
                ""name"": ""Profil 1"",
                ""shortcut_name"": ""Profil 1"",
                ""user_name"": """"
            },
            ""Profile 1"": {
            ""active_time"": 0,
                ""avatar_icon"": ""chrome://theme/IDR_PROFILE_AVATAR_52"",
                ""background_apps"": false,
                ""default_avatar_fill_color"": -1,
                ""default_avatar_stroke_color"": -1,
                ""force_signin_profile_locked"": false,
                ""gaia_given_name"": """",
                ""gaia_id"": """",
                ""gaia_name"": """",
                ""hosted_domain"": """",
                ""is_consented_primary_account"": false,
                ""is_ephemeral"": false,
                ""is_using_default_avatar"": false,
                ""is_using_default_name"": false,
                ""managed_user_id"": """",
                ""metrics_bucket_index"": 2,
                ""name"": ""Profile 1 Name"",
                ""profile_highlight_color"": -9629166,
                ""shortcut_name"": ""Profile 1 Shortcut Name"",
                ""use_gaia_picture"": false,
                ""user_name"": """"
            },
            ""Profile 10"": {
                ""active_time"": 0,
                ""avatar_icon"": ""chrome://theme/IDR_PROFILE_AVATAR_46"",
                ""background_apps"": false,
                ""default_avatar_fill_color"": -1,
                ""default_avatar_stroke_color"": -1,
                ""force_signin_profile_locked"": false,
                ""gaia_given_name"": """",
                ""gaia_id"": """",
                ""gaia_name"": """",
                ""hosted_domain"": """",
                ""is_consented_primary_account"": false,
                ""is_ephemeral"": false,
                ""is_using_default_avatar"": true,
                ""is_using_default_name"": false,
                ""managed_user_id"": """",
                ""metrics_bucket_index"": 11,
                ""name"": ""Profile 10 Name"",
                ""profile_highlight_color"": -21696,
                ""shortcut_name"": ""Profile 10 Shortcut Name"",
                ""user_name"": """"
            }
    },
        ""last_active_profiles"": [
            ""Profile 26"",
            ""Profile 19"",
            ""Profile 23"",
            ""Profile 25"",
            ""Profile 20"",
            ""Profile 32"",
            ""Profile 1""
        ],
        ""last_used"": ""Profile 32"",
        ""metrics"": {
        ""next_bucket_index"": 34
        },
        ""picker_shown"": true,
        ""profile_counts_reported"": ""0"",
        ""profiles_created"": 33,
        ""show_picker_on_startup"": false
    },
    ""profile_network_context_service"": {
},
    ""shutdown"": {
    ""num_processes"": 0,
        ""num_processes_slow"": 0,
        ""type"": 0
    },
    ""software_reporter"": {
    ""last_time_triggered"": ""0""
    },
    ""subresource_filter"": {
    ""ruleset_version"": {
    }
},
    ""tab_stats"": {
},
    ""ukm"": {
    ""persisted_logs"": []
    },
    ""uninstall_metrics"": {
    ""installation_date2"": ""0""
    },
    ""user_experience_metrics"": {
    ""default_opt_in"": 2,
        ""low_entropy_source3"": 0,
        ""machine_id"": 0,
        ""pseudo_low_entropy_source"": 0,
        ""session_id"": 0,
        ""stability"": {
        ""browser_last_live_timestamp"": ""0"",
            ""child_process_crash_count"": 0,
            ""crash_count"": 0,
            ""exited_cleanly"": false,
            ""extension_renderer_crash_count"": 0,
            ""extension_renderer_failed_launch_count"": 0,
            ""extension_renderer_launch_count"": 0,
            ""gpu_crash_count"": 0,
            ""incomplete_session_end_count"": 0,
            ""launch_count"": 0,
            ""page_load_count"": 0,
            ""plugin_stats2"": [],
            ""renderer_crash_count"": 0,
            ""renderer_failed_launch_count"": 0,
            ""renderer_hang_count"": 0,
            ""renderer_launch_count"": 0,
            ""session_end_completed"": true,
            ""stats_buildtime"": ""0"",
            ""stats_version"": ""94.0.0.0-64"",
            ""system_crash_count"": 0
        }
},
    ""was"": {
    ""restarted"": false
    }
}
";
            var tokenPathes = new TeamsTokenPathesSystem(new Configuration());
            var basePath = @"C:\Temp";
            var profilePathes = TeamsTokenPathes.GetChromeProfilePathesFromString(basePath, json);
            Assert.AreEqual(2, profilePathes.Count, "Unexpected number of custom profiles");
            Assert.AreEqual("Profile 1 Name", profilePathes.First().Key);
            Assert.AreEqual(@"C:\Temp\Profile 1", profilePathes.First().Value);
            Assert.AreEqual("Profile 10 Name", profilePathes.Last().Key);
            Assert.AreEqual(@"C:\Temp\Profile 10", profilePathes.Last().Value);
        }
    }
}
