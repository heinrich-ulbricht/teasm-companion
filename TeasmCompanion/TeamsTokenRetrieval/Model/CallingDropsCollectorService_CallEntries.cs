using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeasmCompanion.TeamsTokenRetrieval.Model
{
    public class CallingDropsCollectorService_CallEntries
    {
        public Ids ids { get; set; }
        public Current current { get; set; }
        public Aggregate aggregate { get; set; }
        public string[] stateChanges { get; set; }
        public string[] events { get; set; }
        public bool isBroadcast { get; set; }
        public bool isMinimalMeeting { get; set; }
        public bool ndiEnabled { get; set; }
        public bool ndiWasCaptured { get; set; }
        public string callType { get; set; }
        public string signalingScenarioStatus { get; set; }
    }

    public class Ids
    {
        public string id { get; set; }
        public string sessionId { get; set; }
        public string callId { get; set; }
        public string endpointId { get; set; }
        public string participantId { get; set; }
    }

    public class Current
    {
        public int state { get; set; }
        public bool isComplete { get; set; }
        public bool isScreenSharing { get; set; }
        public bool isBatteryStatusDangerous { get; set; }
        public bool isNetworkReconnectOngoing { get; set; }
    }

    public class Aggregate
    {
        public bool hadCriticalBattery { get; set; }
        public bool hadOfflineState { get; set; }
        public bool hadScreenSharing { get; set; }
        public bool hadSuspendEvent { get; set; }
        public bool hadNetworkReconnect { get; set; }
        public bool hadDialInShown { get; set; }
        public bool hadDialOutShown { get; set; }
        public bool wasChildWindowShown { get; set; }
    }
}
