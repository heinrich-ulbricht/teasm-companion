using System.Collections.Generic;

namespace TeasmCompanion.TeamsInternal.TeamsInternalApi.v2.users.me.endpoints
{

    public class RegisterEndpoint_ResponseBody
    {
        public string id { get; set; }
        public string endpointFeatures { get; set; }
        public List<ResponseSubscription> subscriptions { get; set; }
        public string isActiveUrl { get; set; }
        public bool longPollActiveTimeoutSupport { get; set; }
    }

    public class ResponseSubscription
    {
        public string channelType { get; set; }
        public List<string> interestedResources { get; set; }
        public string longPollUrl { get; set; }
    }

}
