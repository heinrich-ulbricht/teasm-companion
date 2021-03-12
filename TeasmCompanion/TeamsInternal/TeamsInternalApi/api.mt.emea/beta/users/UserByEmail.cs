using System.Collections.Generic;

namespace TeasmCompanion.TeamsInternal.TeamsInternalApi.api.mt.emea.beta.users
{

    public class UserByEmail
    {
        public Value value { get; set; }
        public string type { get; set; }
    }

    public class Value
    {
        public bool accountEnabled { get; set; }
        public bool showInAddressList { get; set; }
        public string mail { get; set; }
        public string objectType { get; set; }
        public string role { get; set; }
        public Skypeteamsinfo skypeTeamsInfo { get; set; }
        public Featuresettings featureSettings { get; set; }
        public List<string> smtpAddresses { get; set; }
        public bool isSipDisabled { get; set; }
        public bool isShortProfile { get; set; }
        public object[] phones { get; set; }
        public string responseSourceInformation { get; set; }
        public string userPrincipalName { get; set; }
        public string givenName { get; set; }
        public string email { get; set; }
        public string userType { get; set; }
        public string displayName { get; set; }
        public string type { get; set; }
        public string mri { get; set; }
        public string objectId { get; set; }
    }
}
