using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace TeasmBrowserAutomation.Credentials
{
    public class PasswordSource
    {
        private bool IsProtectDataSupported()
        {
            try
            {
#pragma warning disable CA1416 // Validate platform compatibility
                _ = ProtectedData.Protect(Encoding.UTF8.GetBytes("dummystringtotestplatformsupport"), null, DataProtectionScope.CurrentUser);
#pragma warning restore CA1416 // Validate platform compatibility

                return true;
            }
            catch (PlatformNotSupportedException)
            {
                return false;
            }
        }


        private async Task<ShellResult?> GetPasswordViaDialogAsync(string text)
        {
            try
            {
                var result = await $"zenity --forms --title=\"Automatic Login\" --text=\"{text}\" --add-password=\"Secret:\"".Bash();
                return result;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Ask user for password or retrieve previously stored password. Furthermore return the path where the browser user profile should be stored when logging in.
        /// </summary>
        /// <param name="usernameToLogInWith">User name to ask password for</param>
        /// <param name="tenantId">Tenant ID that will be used to create a user profile directory for that tenant</param>
        /// <param name="discardExistingPassword">Ask the user for their password even if a stored password is present (can be used to update an expired password)</param>
        /// <param name="tenantLoginDataDirPath_WillBeCreatedIfDoesntExist">Use this as browser user profile directory instead of generating one</param>
        /// <returns>Password and path to directory that can be used to store the browser user profile data for the given tenant</returns>
        public async Task<(string, string)> GetPasswordAndUserDataDirForAsync(string usernameToLogInWith, string? tenantId, bool discardExistingPassword = false, string? tenantLoginDataDirPath_WillBeCreatedIfDoesntExist = null)
        {
            var isProtectDataSupported = IsProtectDataSupported();
            if (string.IsNullOrWhiteSpace(tenantLoginDataDirPath_WillBeCreatedIfDoesntExist))
            {
                tenantLoginDataDirPath_WillBeCreatedIfDoesntExist = Path.Combine(Path.GetTempPath(), $"TeasmBrowserAutomation-{usernameToLogInWith}-{(string.IsNullOrWhiteSpace(tenantId) ? "default" : tenantId)}");
            }

            string? password = null;
            if (!Directory.Exists(tenantLoginDataDirPath_WillBeCreatedIfDoesntExist))
            {
                Directory.CreateDirectory(tenantLoginDataDirPath_WillBeCreatedIfDoesntExist);
            }
            var userPasswordFileParentDirPath = Directory.GetParent(tenantLoginDataDirPath_WillBeCreatedIfDoesntExist)?.FullName ?? tenantLoginDataDirPath_WillBeCreatedIfDoesntExist;
            var passwordFilePath = Path.Combine(userPasswordFileParentDirPath, $"{usernameToLogInWith}-password{(isProtectDataSupported ? ".txt.enc" : ".txt")}");

            if (File.Exists(passwordFilePath))
            {
                if (isProtectDataSupported)
                {
                    var protectedPasswordBytesBase64 = File.ReadAllText(passwordFilePath);
                    var protectedPasswordBytes = Convert.FromBase64String(protectedPasswordBytesBase64);
                    byte[] passwordBytes;
#pragma warning disable CA1416 // Validate platform compatibility
                    passwordBytes = ProtectedData.Unprotect(protectedPasswordBytes, null, DataProtectionScope.CurrentUser);
#pragma warning restore CA1416 // Validate platform compatibility
                    password = Encoding.UTF8.GetString(passwordBytes);
                }
                else
                {
                    password = File.ReadAllText(passwordFilePath);

                }
            }
            if (password != null && !discardExistingPassword)
            {
                return (password, tenantLoginDataDirPath_WillBeCreatedIfDoesntExist);
            }

            var promptText = $"Enter{(discardExistingPassword ? " new" : "")} password for {usernameToLogInWith} {Environment.NewLine}{Environment.NewLine}(it will be stored {(isProtectDataSupported ? "encrypted" : "IN PLAIN TEXT")} in file {Environment.NewLine}'{passwordFilePath}'):";
            var pwResult = await GetPasswordViaDialogAsync(promptText);
            password = pwResult?.StdOutput.ReplaceLineEndings().Replace(Environment.NewLine, "");
            // if dialog cannot be shown: ask via termin (note: it is not very user friendly because log output interfers with password input)
            if (null == pwResult || pwResult.UserCanceled)
            {
                password = McMaster.Extensions.CommandLineUtils.Prompt.GetPassword(promptText);
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                return ("", tenantLoginDataDirPath_WillBeCreatedIfDoesntExist);
            }

            try
            {
                if (isProtectDataSupported)
                {
                    var passwordBytes = Encoding.UTF8.GetBytes(password);
#pragma warning disable CA1416 // Validate platform compatibility
                    var protectedPasswordBytes = ProtectedData.Protect(passwordBytes, null, DataProtectionScope.CurrentUser);
#pragma warning restore CA1416 // Validate platform compatibility
                    var protectedPasswordBytesBase64 = Convert.ToBase64String(protectedPasswordBytes);
                    File.WriteAllText(passwordFilePath, protectedPasswordBytesBase64);
                }
                else
                {
                    File.WriteAllText(passwordFilePath, password);
                }
            }
            catch (PlatformNotSupportedException)
            {
                Console.WriteLine("Could not store credentials; not supported on this platform");
            }

            return (password, tenantLoginDataDirPath_WillBeCreatedIfDoesntExist);
        }
    }
}