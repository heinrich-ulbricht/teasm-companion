using CommandLine;

#nullable enable

namespace TeasmBrowserAutomation.Credentials
{
    public class CommandlineOptions
    {
        [Option('u', "username", Required = true, HelpText = "Username")]
        public string? Username { get; set; }
        [Option('d', "user-data-dir-path", Required = false, HelpText = "Absolute path to user data dir")]
        public string? UserDataDirPath { get; set; }
        [Option('c', "chrome-binary-path", Required = true, HelpText = "Absolute path to chrome binary")]
        public string? ChromeBinaryPath { get; set; }
        [Option('w', "web-driver-dir-path", Required = true, HelpText = "Absolute path to the directory where the web driver is located")]
        public string? WebDriverDirPath { get; set; }
        [Option('t', "tenant-id", Required = false, HelpText = "Tenant ID (GUID or tenant.onmicrosoft.com)")]
        public string? TenantId { get; set; }
        [Option('n', "tenant-name", Required = false, HelpText = "Tenant name")]
        public string? TenantName { get; set; }

        [Option('m', "mobile-number", Required = false, HelpText = "Mobile number for MFA Relay via Signal Messenger ('+4915701234567')")]
        public string? MobileNumberForSignalMfaRelay {get; set;}
    }
}
