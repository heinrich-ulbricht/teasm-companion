﻿namespace TeasmCompanion.TeamsInternal.TeamsApi
{
    // returned by the authsvc/v1.0/authz endpoint that is called with the bearer token gotten after authenticating with user credentials; contains endpoint URLs
    public class AuthGtmTable
    {
        public string ams { get; set; }
        public string amsV2 { get; set; }
        public string amsS2S { get; set; }
        public string appsDataLayerService { get; set; }
        public string appsDataLayerServiceS2S { get; set; }
        public string calling_callControllerServiceUrl { get; set; }
        public string calling_callStoreUrl { get; set; }
        public string calling_conversationServiceUrl { get; set; }
        public string calling_keyDistributionUrl { get; set; }
        public string calling_potentialCallRequestUrl { get; set; }
        public string calling_registrarUrl { get; set; }
        public string calling_sharedLineOptionsUrl { get; set; }
        public string calling_trouterUrl { get; set; }
        public string calling_udpTransportUrl { get; set; }
        public string calling_uploadLogRequestUrl { get; set; }
        public string callingS2S_Broker { get; set; }
        public string callingS2S_CallController { get; set; }
        public string callingS2S_CallStore { get; set; }
        public string callingS2S_ContentSharing { get; set; }
        public string callingS2S_ConversationService { get; set; }
        public string callingS2S_EnterpriseProxy { get; set; }
        public string callingS2S_MediaController { get; set; }
        public string callingS2S_PlatformMediaAgent { get; set; }
        public string chatService { get; set; }
        public string chatServiceAfd { get; set; }
        public string chatServiceS2S { get; set; }
        public string drad { get; set; }
        public string mailhookS2S { get; set; }
        public string middleTier { get; set; }
        public string middleTierS2S { get; set; }
        public string mtImageService { get; set; }
        public string powerPointStateService { get; set; }
        public string search { get; set; }
        public string searchTelemetry { get; set; }
        public string teamsAndChannelsService { get; set; }
        public string teamsAndChannelsProvisioningService { get; set; }
        public string urlp { get; set; }
        public string urlpV2 { get; set; }
        public string unifiedPresence { get; set; }
        public string userEntitlementService { get; set; }
        public string userIntelligenceService { get; set; }
        public string userProfileService { get; set; }
        public string userProfileServiceS2S { get; set; }
        public string amdS2S { get; set; }
        public string chatServiceAggregator { get; set; }
        public string ehrConnector { get; set; }
        public string regionName { get; set; }
        public string partition { get; set; }
    }
}
