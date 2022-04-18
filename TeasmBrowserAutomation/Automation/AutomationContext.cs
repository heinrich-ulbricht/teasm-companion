using System.Collections.Generic;
using System.IO;

#nullable enable

namespace TeasmBrowserAutomation.Automation
{
    public class AutomationContext
    {
        public string Username { get; }
        public string? Password { get; set; }
        public const string DefaultTenantId = "default";
        public string TenantId { get; }
        public string TenantName { get; }
        public string BaseDirectoryForUserData { get; set; }
        // multiple MFA operations might be necessary when logging in as guest (home tenant plus guest tenant)
        public Dictionary<string, string> TotpKey { get; } = new();

        public AutomationContext(string username, string? tenantId, string? tenantName, string? baseDirectoryForUserData = null)
        {
            Username = username;
            TenantId = tenantId ?? DefaultTenantId;
            TenantName = tenantName ?? TenantId;
            BaseDirectoryForUserData = baseDirectoryForUserData ?? Path.GetTempPath();
        }

        public string EnsureBasePathForScope(string scopeId)
        {
            string path = Path.Combine(BaseDirectoryForUserData, "TeasmBrowserAutomation", scopeId);;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }

        public string EnsureUserBrowserDataDirPath()
        {
            var basePath = EnsureBasePathForScope($"{Username}_{TenantId}");
            var path = Path.Combine(basePath, "browser");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }

        public AutomationContext Clone()
        {
            var result = new AutomationContext(Username, TenantId, TenantName, BaseDirectoryForUserData)
            {
                Password = this.Password
            };
            foreach (var key in TotpKey.Keys)
            {
                result.TotpKey[key] = TotpKey[key];
            }
            return result;
        }
    }
}