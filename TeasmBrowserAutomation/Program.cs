using System;
using System.Threading.Tasks;
using CommandLine;
using TeasmBrowserAutomation.Credentials;
using TeasmBrowserAutomation.Automation;

#nullable enable

namespace TeasmBrowserAutomation
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            CommandlineOptions? options = null;
            Parser.Default.ParseArguments<CommandlineOptions?>(args).MapResult(value => options = value, errors => null);
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options), "No options provided to application");
            }

            var username = options?.Username ?? string.Empty;
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentOutOfRangeException(nameof(username), "Username parameter needs to be non-null and non-whitespace");
            }

            var passwordSource = new PasswordSource();
            var (password, userDataDirPath) = await passwordSource.GetPasswordAndUserDataDirForAsync(username, options?.TenantId);
            var loginAutomation = new LoginAutomation(options?.MobileNumberForSignalMfaRelay);
            var result = await loginAutomation.LogInToTeamsAsync(
                chromeBinaryPath: options?.ChromeBinaryPath,
                webDriverDirPath: options?.WebDriverDirPath,
                userDataDirPath: userDataDirPath,
                username: username,
                password: password,
                getNewPasswordCallback: async () =>
                {
                    var (newPassword, _) = await passwordSource.GetPasswordAndUserDataDirForAsync(username, options?.TenantId, true);
                    return newPassword;
                },
                tenantId: options?.TenantId,
                deleteUserDataDirPathAfterLoggingIn: false);
            return (int)result;
        }
    }
}