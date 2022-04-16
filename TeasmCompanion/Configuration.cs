using System.Collections.Generic;

#nullable enable

namespace TeasmCompanion
{
    public class Configuration
    {
        public string ImapHostName { get; set; } = "localhost";
        public int ImapPort { get; set; } = 143;
        public string? ImapUserName { get; set; }
        public string? ImapPassword { get; set; }
        public int SlowChatRetrievalWaitTimeMin { get; set; } = 30;
        public int FastChatRetrievalWaitTimeMin { get; set; } = 5;
        public int FastChatRetrievalDays { get; set; } = 7;
        public int UpdatedChatRetrievalWaitTimeSec { get; set; } = 5;
        public List<string> TenantIdsToNotSubscribeForNotifications { get; set; } = new List<string>();
        public List<string> ChatIdIgnoreList { get; set; } = new List<string>();
        public int ResolveUnknownUserIdsJobIntervalMin { get; set; } = 10;

        public int ForceUpdateChatDays { get; set; } = 0;
        public bool DebugClearLocalCacheOnStart { get; set; } = false;
        public bool DebugDisableEmailServerCertificateCheck { get; set; } = false;
        public string LogLevel { get; set; } = "Debug";
        public List<string> ChromeNonDefaultProfileNames { get; set; } = new List<string>();
        public List<AutoLogin> AutoLogin { get; set; } = new();
        public string? ChromeBinaryPath { get; set; } = null;
        public string? WebDriverDirPath { get; set; } = null;
        // like "+4915701234567"
        public string? MobileNumberForSignalMfaRelay { get; set; } = null;
    }

    public class AutoLogin
    {
        public string? AccountEmail { get; set; }
        public string TenantId { get; set; } = "";
        public string? DisplayName { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrWhiteSpace(DisplayName)) {
                return $"account '{AccountEmail}' in tenant '{DisplayName}' (ID: {TenantId})";
            } else {
                return $"account '{AccountEmail}' in tenant {TenantId}";
            }
        }

    }
}
