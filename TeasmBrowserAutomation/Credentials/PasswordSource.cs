using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TeasmBrowserAutomation.Automation;

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


        private async Task<ShellResult?> AskUserForSecretViaDialogAsync(string text)
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

        private async Task<string?> GetSecretAsync(AutomationContext context, string monikerOfSecret, bool discardExistingSecret, string scopeId)
        {
            var isProtectDataSupported = IsProtectDataSupported();
            var secretStorageFileName = $"{scopeId}-{monikerOfSecret.Trim().Replace(" ", "+")}{(isProtectDataSupported ? ".txt.enc" : ".txt")}";;
            var secretValueFilePath = Path.Combine(context.EnsureBasePathForScope(scopeId), secretStorageFileName);

            string? secret = null;
            if (File.Exists(secretValueFilePath))
            {
                if (isProtectDataSupported)
                {
                    var protectedSecretBytesBase64 = File.ReadAllText(secretValueFilePath);
                    if (protectedSecretBytesBase64 != "")
                    {
                        var protectedSecretBytes = Convert.FromBase64String(protectedSecretBytesBase64);
                        byte[] secretBytes;
#pragma warning disable CA1416 // Validate platform compatibility
                        secretBytes = ProtectedData.Unprotect(protectedSecretBytes, null, DataProtectionScope.CurrentUser);
#pragma warning restore CA1416 // Validate platform compatibility
                        secret = Encoding.UTF8.GetString(secretBytes);
                    } else 
                    {
                        secret = "";
                    }
                }
                else
                {
                    secret = File.ReadAllText(secretValueFilePath);

                }
            }
            if (null != secret && !discardExistingSecret)
            {
                return secret;
            }

            var promptText = $"Destination tenant '{context.TenantName}': Enter{(discardExistingSecret ? " new" : "")} {monikerOfSecret} for {context.Username} {Environment.NewLine}{Environment.NewLine}(it will be stored {(isProtectDataSupported ? "encrypted" : "IN PLAIN TEXT")} in file {Environment.NewLine}'{secretValueFilePath}'):";
            var secretResult = await AskUserForSecretViaDialogAsync(promptText);
            secret = secretResult?.StdOutput.ReplaceLineEndings().Replace(Environment.NewLine, "");
            // if dialog cannot be shown: ask via termin (note: it is not very user friendly because log output interfers with value input)
            if (null == secretResult || secretResult.UserCanceled)
            {
                secret = McMaster.Extensions.CommandLineUtils.Prompt.GetPassword(promptText);
            }
            if (null == secret)
            {
                return null;
            }
 
            // note: explicitly store empty input
            try
            {
                if (isProtectDataSupported && !string.IsNullOrEmpty(secret))
                {
                    var secretBytes = Encoding.UTF8.GetBytes(secret);
#pragma warning disable CA1416 // Validate platform compatibility
                    var protectedSecretBytes = ProtectedData.Protect(secretBytes, null, DataProtectionScope.CurrentUser);
#pragma warning restore CA1416 // Validate platform compatibility
                    var protectedSecretBytesBase64 = Convert.ToBase64String(protectedSecretBytes);
                    File.WriteAllText(secretValueFilePath, protectedSecretBytesBase64);
                }
                else
                {
                    File.WriteAllText(secretValueFilePath, secret);
                }
            }
            catch (PlatformNotSupportedException)
            {
                Console.WriteLine("Could not store credentials; not supported on this platform");
            }

            return secret;
        }

        public async Task<AutomationContext> GetAutomationContextAndAskForPasswordAsync(string usernameToLogInWith, string? tenantId, string? tenantName, bool discardExistingPassword = false)
        {
            var context = new AutomationContext(usernameToLogInWith, tenantId, tenantName);
            var password = await GetSecretAsync(context, "password", discardExistingPassword, context.Username);
            context.Password = password;

            return context;
        }

        public async Task<AutomationContext> CloneAndUpdatePasswordAsync(AutomationContext context, bool discardExisting)
        {
            var result = context.Clone();
            result.Password = await GetSecretAsync(context, "password", discardExisting, context.Username);
            return result;
        }

        public async Task<AutomationContext> CloneAndUpdateTotpKeyAsync(AutomationContext context, string mfaTenant, bool discardExisting)
        {
            var result = context.Clone();
            var totpKey = await GetSecretAsync(context, "TOTP key (can be empty)", discardExisting, $"{context.Username}_{mfaTenant}");
            if (null != totpKey)
            {
                result.TotpKey[mfaTenant] = totpKey;
            }
            return result;
        }
    }
}