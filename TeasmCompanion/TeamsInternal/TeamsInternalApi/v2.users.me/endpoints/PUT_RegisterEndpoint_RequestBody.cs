using System.Collections.Generic;

namespace TeasmCompanion.TeamsInternal.TeamsInternalApi.v2.users.me.endpoints
{

    public class PUT_RegisterEndpoint_RequestBody
    {
        public long startingTimeSpan { get; set; }
        public string endpointFeatures { get; set; }
        public List<RequestSubscription> subscriptions { get; set; }
    }

    public class RequestSubscription
    {
        public string channelType { get; set; }
        public List<string> interestedResources { get; set; }
    }

}
