﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using TeasmBrowserAutomation.Mfa;
using OtpNet;

#nullable enable

namespace TeasmBrowserAutomation.Automation
{
    /// <summary>
    /// Automate the Microsoft login dialog as far as possible.
    /// 
    /// If authentication needs a second factor then this needs to be entered/triggered manually.
    /// 
    /// When using mobile app codes for MFA you can send the MFA code via Signal. This needs to be set up though. Instructions currently are
    /// scarce.
    /// </summary>
    public class LoginAutomation
    {
        static readonly By emailField = By.Id("i0116");
        static readonly By passwordField = By.Id("i0118");
        static readonly By nextButton = By.Id("idSIButton9");

        readonly MfaRelay? _signalRelay = null;

        public LoginAutomation()
        {
        }

        /// <summary>
        /// Create instance capable of receiving MFA codes via Signal messenger.
        /// </summary>
        /// <param name="mobileNumberForSignalMfaRelay">Mobile number of Signal account like +4915701234567</param>
        public LoginAutomation(string? mobileNumberForSignalMfaRelay)
        {
            if (!string.IsNullOrWhiteSpace(mobileNumberForSignalMfaRelay))
            {
                _signalRelay = new(mobileNumberForSignalMfaRelay);
            }
        }

        public static bool IsLoginCorporateVsPrivatePage(IWebDriver driver)
        {
            try
            {
                var containsCorporateOption = driver.FindElements(By.Id("aadTile")).Where(b => b.Enabled && b.Displayed).Any();
                var containsContinueButton = driver.FindElements(nextButton).Where(b => b.Displayed && b.Enabled).Any();

                return containsCorporateOption && !containsContinueButton;
            }
            catch (StaleElementReferenceException)
            {
                // can happen when switching between pages
                return false;
            }
        }

        public static bool IsLoginPasswordPage(IWebDriver driver)
        {
            try
            {
                var containsNextButton = driver.FindElements(nextButton).Where(b => b.Enabled && b.Displayed).Any();
                var containsPasswordField = driver.FindElements(passwordField).Where(b => b.Enabled && b.Displayed).Any();
                var containsErrorMessage = driver.FindElements(By.Id("passwordError")).Where(b => b.Displayed).Any();

                return containsNextButton && containsPasswordField && !containsErrorMessage;
            }
            catch (StaleElementReferenceException)
            {
                // can happen when switching between pages
                return false;
            }
        }

        public static bool IsLoginPasswordErrorPage(IWebDriver driver)
        {
            try
            {
                var containsNextButton = driver.FindElements(nextButton).Where(b => b.Enabled && b.Displayed).Any();
                var containsPasswordField = driver.FindElements(passwordField).Where(b => b.Enabled && b.Displayed).Any();
                var containsErrorMessage = driver.FindElements(By.Id("passwordError")).Where(b => b.Displayed).Any();

                return containsNextButton && containsPasswordField && containsErrorMessage;
            }
            catch (StaleElementReferenceException)
            {
                // can happen when switching between pages
                return false;
            }
        }

        public static bool IsLoginEmailPage(IWebDriver driver)
        {
            try
            {
                var containsNextButton = driver.FindElements(nextButton).Where(b => b.Enabled && b.Displayed).Any();
                var containsEmailField = driver.FindElements(emailField).Where(b => b.Enabled && b.Displayed).Any();

                return containsNextButton && containsEmailField;
            }
            catch (StaleElementReferenceException)
            {
                // can happen when switching between pages
                return false;
            }
        }

        public static bool IsLoginKmsiPage(IWebDriver driver)
        {
            try
            {
                var containsNoButton = driver.FindElements(By.Id("idBtn_Back")).Where(b => b.Enabled && b.Displayed).Any();
                var containsYesButton = driver.FindElements(nextButton).Where(b => b.Enabled && b.Displayed).Any();
                var containsKeepMeSignedInHeader = driver.FindElements(By.XPath("//*[contains(@data-bind,'STR_Kmsi_Title')]")).Any();
                return containsKeepMeSignedInHeader && containsYesButton && containsNoButton;
            }
            catch (StaleElementReferenceException)
            {
                // can happen when switching between pages
                return false;
            }
        }

        public static (bool, bool) IsMfaCodeEntryPage(IWebDriver driver)
        {
            try
            {
                var containsVerifyButton = driver.FindElements(By.Id("idSubmit_SAOTCC_Continue")).Where(b => b.Enabled && b.Displayed).Any();
                var containsCodeInputField = driver.FindElements(By.Id("idTxtBx_SAOTCC_OTC")).Where(b => b.Enabled && b.Displayed).Any();
                var containsErrorMessage = driver.FindElements(By.Id("idSpan_SAOTCC_Error_OTC")).Where(b => b.Displayed).Any();
                return (containsVerifyButton && containsCodeInputField, containsErrorMessage);
            }
            catch (StaleElementReferenceException)
            {
                // can happen when switching between pages
                return (false, false);
            }
        }

        public static bool IsPickAnAccountPageWithAccountPresent(IWebDriver driver, string accountEmail)
        {
            try
            {
                var containsAccountTile = driver.FindElements(By.CssSelector($"[data-test-id='{accountEmail}']")).Where(b => b.Enabled && b.Displayed).Any();
                var containsOtherTile = driver.FindElements(By.Id("otherTile")).Where(b => b.Enabled && b.Displayed).Any();
                return containsAccountTile && containsOtherTile;
            }
            catch (StaleElementReferenceException)
            {
                // can happen when switching between pages
                return false;
            }
        }

        public static bool IsPickAnAccountPageWithAccountMissing(IWebDriver driver, string accountEmail)
        {
            try
            {
                var containsAccountTile = driver.FindElements(By.CssSelector($"[data-test-id='{accountEmail}']")).Where(b => b.Enabled && b.Displayed).Any();
                var containsOtherTile = driver.FindElements(By.Id("otherTile")).Where(b => b.Enabled && b.Displayed).Any();
                return !containsAccountTile && containsOtherTile;
            }
            catch (StaleElementReferenceException)
            {
                // can happen when switching between pages
                return false;
            }
        }

        public static bool IsAuthenticatedInTeams(IWebDriver driver)
        {
            try
            {
                var containsSkypeBubble = driver.FindElements(By.XPath("//*[contains(@mri,'appHeaderBar.authenticatedUserMri')]")).Any();
                return containsSkypeBubble;
            }
            catch (StaleElementReferenceException)
            {
                // can happen when switching between pages
                return false;
            }
        }

        public static bool IsTroubleshootingBannerVisible(IWebDriver driver)
        {
            try
            {
                var containsTroubleshootingBanner = driver.FindElements(By.Id("debugDetailsBanner")).Where(b => b.Enabled && b.Displayed).Any();
                return containsTroubleshootingBanner;
            }
            catch (StaleElementReferenceException)
            {
                // can happen when switching between pages
                return false;
            }            
        }

        private static LoginStage GetCurrentLoginStage(IWebDriver driver, string accountEmail, double waitSecs = 60)
        {
            var isLoginEmailPage = false;
            var isLoginPasswordPage = false;
            var isLoginPasswordErrorPage = false;
            var isLoginCorporateVsPrivatePage = false;
            var isLoginKmsiPage = false;
            var isAuthenticatedInTeams = false;
            var (isMfaCodeEntryPage, hasMfaError) = (false, false);
            var isPickAnAccountWithAccountPresent = false;
            var isPickAnAccountWithAccountMissing = false;
            var isTroubleshootingBannerVisible = false;

            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(waitSecs));
            if (!wait.Until(webDriver =>
                   (isLoginEmailPage = IsLoginEmailPage(webDriver))
                || (isLoginPasswordErrorPage = IsLoginPasswordErrorPage(webDriver))
                || (isLoginPasswordPage = IsLoginPasswordPage(webDriver))
                || (isLoginCorporateVsPrivatePage = IsLoginCorporateVsPrivatePage(webDriver))
                || (isLoginKmsiPage = IsLoginKmsiPage(webDriver))
                || (isAuthenticatedInTeams = IsAuthenticatedInTeams(webDriver))
                || ((isMfaCodeEntryPage, hasMfaError) = IsMfaCodeEntryPage(webDriver)).Item1
                || (isPickAnAccountWithAccountPresent = IsPickAnAccountPageWithAccountPresent(webDriver, accountEmail))
                || (isPickAnAccountWithAccountMissing = IsPickAnAccountPageWithAccountMissing(webDriver, accountEmail))
                || (isTroubleshootingBannerVisible = IsTroubleshootingBannerVisible(webDriver))
                ))
            {
                Debug.WriteLine("Could find neither a login page nor Teams");
                return LoginStage.Unknown;
            }

            if (isTroubleshootingBannerVisible)
            {
                return LoginStage.TroubleshootingBannerVisible;
            }

            if (isLoginEmailPage)
            {
                return LoginStage.AccountEmail;
            }

            if (isLoginCorporateVsPrivatePage)
            {
                return LoginStage.CorporateVsPrivate;
            }

            if (isLoginPasswordPage)
            {
                return LoginStage.Password;
            }

            if (isLoginPasswordErrorPage)
            {
                return LoginStage.PasswordError;
            }

            if (isLoginKmsiPage)
            {
                return LoginStage.Kmsi;
            }

            if (isAuthenticatedInTeams)
            {
                return LoginStage.Teams;
            }

            if (isPickAnAccountWithAccountPresent)
            {
                return LoginStage.PickAnAccountWithAccountPresent;
            }

            if (isPickAnAccountWithAccountMissing)
            {
                return LoginStage.PickAnAccountWithAccountMissing;
            }

            if (isMfaCodeEntryPage && !hasMfaError)
            {
                return LoginStage.MfaCodeEntry;
            }
            if (isMfaCodeEntryPage && hasMfaError)
            {
                return LoginStage.MfaCodeEntry_HasError;
            }

            return LoginStage.Unknown;
        }

        private static LoginStage WaitForLoginStageToChange(IWebDriver driver, LoginStage oldStage, string accountEmail)
        {
            var currentLoginStage = GetCurrentLoginStage(driver, accountEmail);
            var maxRetryTimeSpan = TimeSpan.FromSeconds(30);
            if (oldStage == LoginStage.MfaCodeEntry || oldStage == LoginStage.PasswordError)
            {
                maxRetryTimeSpan = TimeSpan.FromMinutes(5); // give more time for MFA code/password entry; may need to test and adjust
            }
            var startTime = DateTime.Now;
            while (currentLoginStage == oldStage)
            {
                currentLoginStage = GetCurrentLoginStage(driver, accountEmail);
                Thread.Sleep(1000);

                if (DateTime.Now - startTime >= maxRetryTimeSpan)
                {
                    return oldStage;
                }
            }
            return currentLoginStage;
        }

        /// <summary>
        /// Start login automation for Teams
        /// </summary>
        /// <param name="chromeBinaryPath">Path to chrome binary; falls back to web driver defaults when empty</param>
        /// <param name="webDriverDirPath">Path to directory containing Chrome web driver; web driver needs to match the chrome binary version</param>
        /// <param name="userDataDirPath">Path to directory where the chrome profile will be saved</param>
        /// <param name="username">User name for login, e.g. 'megan.bowen@contoso.com'</param>
        /// <param name="password">Password for user</param>
        /// <param name="getNewPasswordCallback">Will be called when the password is invalid; use this to ask the user for the current password</param>
        /// <param name="tenantId">GUID or name ('contoso.onmicrosoft.com') of tenant; skipping this might log in the user to an unexpecteed tenant, if they are guests in multiple tenants</param>
        /// <param name="deleteUserDataDirPathAfterLoggingIn">Should the user directory be deleted after logging in?</param>
        /// <returns>Login stage that has been reached; a successful login is marked by reaching LoginStage.Teams</returns>
        public async Task<LoginStage> LogInToTeamsAsync(
            string? chromeBinaryPath,
            string? webDriverDirPath,
            AutomationContext context,
            Func<AutomationContext, bool, Task<AutomationContext>> getNewPasswordCallback,
            Func<AutomationContext, string, bool, Task<AutomationContext>> getNewTotpKeyCallback,
            bool deleteUserDataDirPathAfterLoggingIn = true)
        {
            var finalLoginStage = LoginStage.Unknown;
            if (null != _signalRelay)
            {
                await _signalRelay.RegisterMessageListenerAsync();
            }
            var userDataDirPath = context.EnsureUserBrowserDataDirPath();
            try
            {
                var options = new ChromeOptions
                {
                    BinaryLocation = chromeBinaryPath ?? ""
                };
                options.AddArgument($"--user-data-dir={userDataDirPath}");
                // disable images to speed up loading; not using this as the image indicates which tenant we are signing in to
                // 1 = enabled, 2 = disabled
                // options.AddUserProfilePreference("profile.default_content_setting_values.images", 2);
                options.UseSpecCompliantProtocol = true;
                try
                {
                    using IWebDriver driver = new ChromeDriver(webDriverDirPath, options);
                    WebDriverWait wait10 = new(driver, TimeSpan.FromSeconds(10));
                    WebDriverWait wait60 = new(driver, TimeSpan.FromSeconds(60));
                    // note: a set login_hint used to skip the user name entry page of the login experience; lately it just pre-fills the user name field
                    var teamsUrl = $"https://teams.microsoft.com/_#/apps?login_hint={context.Username}";
                    if (!string.IsNullOrEmpty(context.TenantId) && context.TenantId != AutomationContext.DefaultTenantId)
                    {
                        teamsUrl = $"{teamsUrl}&tenantId={context.TenantId}";
                    }
                    driver.Navigate().GoToUrl(teamsUrl);
                    finalLoginStage = await TryAutomatedLoginAsync(context, driver, getNewPasswordCallback, getNewTotpKeyCallback);

                    if (finalLoginStage == LoginStage.Teams)
                    {
                        Debug.WriteLine("Successfully logged into Teams");
                    }
                    else
                    {
                        Debug.WriteLine($"Logging into Teams stuck with login stage == {finalLoginStage}");
                    }
                }
                catch (WebDriverException e)
                {
                    if (e.Message.Contains("user data directory is already in use", StringComparison.InvariantCultureIgnoreCase))
                    {
                        Debug.WriteLine($"Cannot start login automation, please close any open browsers for account {context.Username}");
                    }
                }
            }
            finally
            {
                if (deleteUserDataDirPathAfterLoggingIn)
                {
                    // remove user data dir
                    Directory.Delete(userDataDirPath, true);
                }
            }
            return finalLoginStage;
        }

        private async Task<LoginStage> TryAutomatedLoginAsync(
            AutomationContext context, 
            IWebDriver driver, 
            Func<AutomationContext, bool, Task<AutomationContext>> getNewPasswordCallback,
            Func<AutomationContext, string, bool, Task<AutomationContext>> getNewTotpKeyCallback)
        {
            LoginStage currentLoginStage = GetCurrentLoginStage(driver, context.Username, 60);
            while (currentLoginStage != LoginStage.Teams)
            {
                try
                {
                    switch (currentLoginStage)
                    {
                        case LoginStage.PickAnAccountWithAccountPresent:
                            // the tile should be present or we wouldn't be in this stage
                            var accountEmailTile = driver.FindElements(By.CssSelector($"[data-test-id='{context.Username}']")).Where(b => b.Enabled && b.Displayed).SingleOrDefault();
                            if (null == accountEmailTile)
                            {
                                continue;
                            }
                            accountEmailTile.Click();
                            break;
                        case LoginStage.PickAnAccountWithAccountMissing:
                            throw new NotImplementedException("Implement handling of PickAnAccountWithAccountMissing stage, likely by clicking the 'use another account' tile");
                        case LoginStage.AccountEmail:
                            var emailFieldInstance = driver.FindElement(emailField);
                            emailFieldInstance.Clear(); // email address might be set depending on query parameters; ignore and enter anew
                            emailFieldInstance.SendKeys(context.Username);
                            var submitButton = driver.FindElement(nextButton);
                            submitButton.Click();
                            break;
                        case LoginStage.Password:
                            if (null == context.Password)
                            {
                                context = await getNewPasswordCallback(context, false);
                            }

                            var passwordFieldInstance = driver.FindElement(passwordField);
                            passwordFieldInstance.Clear();
                            passwordFieldInstance.SendKeys(context.Password);
                            submitButton = driver.FindElement(nextButton);
                            submitButton.Click();
                            break;
                        case LoginStage.CorporateVsPrivate:
                            var aadTileInstance = driver.FindElement(By.Id("aadTile"));
                            aadTileInstance.Click();
                            break;
                        case LoginStage.Kmsi:
                            // "Don't show this again"
                            var dontShowThisAgainCheckbox = driver.FindElement(By.Id("KmsiCheckboxField"));
                            dontShowThisAgainCheckbox.Click();
                            submitButton = driver.FindElement(nextButton);
                            submitButton.Click();
                            break;
                        case LoginStage.MfaCodeEntry:
                            var url = new Uri(driver.Url);
                            // "common" for home tenant, otherwise other tenant; note: multiple MFAs might be necessary, e.g. first home tenant then external tenant
                            var mfaTenant = url.AbsolutePath.Split("/", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).First();

                            string? totpKey = null;
                            context.TotpKey.TryGetValue(mfaTenant, out totpKey);
                            if (null == totpKey)
                            {
                                context = await getNewTotpKeyCallback(context, mfaTenant, false);
                            }

                            var mfaCode = "";
                            
                            if (context.TotpKey.TryGetValue(mfaTenant, out totpKey))
                            {
                                var base32Bytes = Base32Encoding.ToBytes(totpKey);
                                var totp = new Totp(base32Bytes);
                                mfaCode = totp.ComputeTotp();
                            }

                            if (string.IsNullOrEmpty(mfaCode))
                            {
                                if (null != _signalRelay && (await _signalRelay.IsRelayAvailableAsync()))
                                {
                                    mfaCode = await _signalRelay.SendMessageAndWaitForReplyAsync($"Enter MFA code for account '{context.Username}' and tenant '{context.TenantName}' ('{context.TenantId}')", m => true);
                                }
                                else
                                {
                                    // don't ask in console - just let the user enter the code via UI
                                    //mfaCode = McMaster.Extensions.CommandLineUtils.Prompt.GetPassword($"Enter MFA code for account '{username}' and tenant '{(string.IsNullOrEmpty(tenantId) ? "default" : tenantId)}':");
                                }
                            }
                            var mfaCodeInputField = driver.FindElement(By.Id("idTxtBx_SAOTCC_OTC"));

                            if (!string.IsNullOrWhiteSpace(mfaCode))
                            {
                                mfaCodeInputField.SendKeys(mfaCode);
                                submitButton = driver.FindElement(By.Id("idSubmit_SAOTCC_Continue"));
                                submitButton.Click();
                            }
                            break;
                        case LoginStage.MfaCodeEntry_HasError:
                            // clear input and try again in stage MfaCodeEntry
                            var codeInputFieldToClear = driver.FindElement(By.Id("idTxtBx_SAOTCC_OTC"));
                            codeInputFieldToClear.Clear();
                            break;
                        case LoginStage.PasswordError:
                            if (null != getNewPasswordCallback)
                            {
                                Debug.WriteLine("Wrong password, please enter valid one");
                                var newContext = await getNewPasswordCallback(context, true);
                                if (newContext.Password != context.Password && !string.IsNullOrEmpty(newContext.Password))
                                {
                                    context = newContext;
                                    passwordFieldInstance = driver.FindElement(passwordField);
                                    passwordFieldInstance.Clear();
                                    passwordFieldInstance.SendKeys(context.Password);
                                    submitButton = driver.FindElement(nextButton);
                                    submitButton.Click();
                                }
                            }
                            break;
                    }
                }
                catch (NoSuchElementException)
                {
                    // sometimes the stage detection gets confused by page transitions - just retry after a short pause
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                    continue;
                }
                var newLoginStage = WaitForLoginStageToChange(driver, currentLoginStage, context.Username);
                if (newLoginStage == currentLoginStage)
                {
                    Debug.WriteLine("Could not change login stage, something is stuck");
                    break;
                }
                currentLoginStage = newLoginStage;
                if (currentLoginStage == LoginStage.Unknown)
                {
                    Debug.WriteLine("Cannot determine current login stage, something is new");
                    break;
                }

                if (currentLoginStage == LoginStage.TroubleshootingBannerVisible)
                {
                    Debug.WriteLine("There was an error, cannot log in automatically; see troubleshooting banner for details");
                    break;
                }
            }

            return currentLoginStage;
        }
    }
}