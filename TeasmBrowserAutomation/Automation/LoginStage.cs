#nullable enable

namespace TeasmBrowserAutomation.Automation
{
    public enum LoginStage : ushort
    {
        // successfully logged into Teams
        Teams,
        // "choose an account" page that is sometimes shown, presenting the previously logged in account
        PickAnAccountWithAccountPresent,
        // like PickAnAccountWithAccountPresent but without the account we need
        PickAnAccountWithAccountMissing,
        // enter email address to log in with
        AccountEmail,
        // choose whether this is a corporate or private account
        CorporateVsPrivate,
        // enter password
        Password,
        // password entry page with error
        PasswordError,
        // keep me signed in?
        Kmsi,
        // enter MFA code
        MfaCodeEntry,
        // MFA page with error
        MfaCodeEntry_HasError,
        // "Sorry, but we've trouble signing you in" - with troubleshooting banner
        TroubleshootingBannerVisible,
        Unknown = 65535,
    }
}
