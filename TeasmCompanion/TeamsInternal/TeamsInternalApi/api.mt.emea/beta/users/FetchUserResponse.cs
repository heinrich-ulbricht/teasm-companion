using System;
using System.Collections.Generic;
using System.Text;

namespace TeasmCompanion.TeamsInternal.TeamsInternalApi.api.mt.emea.beta.users
{
    public class FetchUserResponse
    {
        public string type { get; set; }
        public List<TeamsUser> value { get; set; }
    }

    // Microsoft.SkypeSpaces.MiddleTier.Models.User
    public class TeamsUser
    {
        public bool showInAddressList { get; set; }
        public string department { get; set; }
        public string mobile { get; set; }
        public string physicalDeliveryOfficeName { get; set; }
        public string userLocation { get; set; }
        public bool accountEnabled { get; set; }
        public string mail { get; set; }
        // User, 
        public string objectType { get; set; }
        public string telephoneNumber { get; set; }
        public Skypeteamsinfo skypeTeamsInfo { get; set; }
        public Featuresettings featureSettings { get; set; }
        public string sipProxyAddress { get; set; }
        public List<string> smtpAddresses { get; set; }
        public bool isSipDisabled { get; set; }
        public bool isShortProfile { get; set; }
        public List<Phone> phones { get; set; }
        // "AAD"
        public string responseSourceInformation { get; set; }
        public string userPrincipalName { get; set; }
        public string givenName { get; set; }
        public string surname { get; set; }
        public string jobTitle { get; set; }
        public string email { get; set; }
        public string displayName { get; set; }
        // "ADUser"
        public string type { get; set; }
        public string mri { get; set; }
        public string objectId { get; set; }
        // "member", 
        public string userType { get; set; }
        // "Guest"
        public string role { get; set; }
        // "en-US"
        public string preferredLanguage { get; set; }
        public string alias { get; set; }
    }

    public class Skypeteamsinfo
    {
        public bool isSkypeTeamsUser { get; set; }
    }

    public class Featuresettings
    {
        public bool isPrivateChatEnabled { get; set; }
        public bool enableShiftPresence { get; set; }
        // "TeamsOnly"
        public string coExistenceMode { get; set; }
        public bool enableScheduleOwnerPermissions { get; set; }
    }

    public class Phone
    {
        public string type { get; set; }
        public string number { get; set; }
    }
}
