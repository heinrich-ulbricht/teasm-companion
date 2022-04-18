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
            // don't provide password right away - will be asked for when needed
            var automationContext = new AutomationContext(username, options?.TenantId, options?.TenantName);
            var loginAutomation = new LoginAutomation(options?.MobileNumberForSignalMfaRelay);
            var result = await loginAutomation.LogInToTeamsAsync(
                chromeBinaryPath: options?.ChromeBinaryPath,
                webDriverDirPath: options?.WebDriverDirPath,
                context: automationContext,
                getNewPasswordCallback: async (oldContext, discardExisting) =>
                {
                    return await passwordSource.CloneAndUpdatePasswordAsync(oldContext, discardExisting);
                },
                getNewTotpKeyCallback: async (oldContext, mfaTenant, discardExisting) =>
                {
                    return await passwordSource.CloneAndUpdateTotpKeyAsync(oldContext, mfaTenant, discardExisting);
                },
                deleteUserDataDirPathAfterLoggingIn: false);
            return (int)result;
        }
    }
}