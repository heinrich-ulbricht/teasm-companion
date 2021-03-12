namespace TeasmCompanion.TeamsInternal.TeamsInternalApi.api.mt.emea.beta.users
{
    public class Tenant
    {
        public string tenantId { get; set; }
        public string tenantName { get; set; }
        public string userId { get; set; }
        public bool isInvitationRedeemed { get; set; }
        public string countryLetterCode { get; set; }
        public string userType { get; set; }
        public string tenantType { get; set; }
        public string redeemUrl { get; set; }
    }
}
