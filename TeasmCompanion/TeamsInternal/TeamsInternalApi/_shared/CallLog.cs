using System;

#nullable enable

namespace TeasmCompanion.TeamsInternal.TeamsInternalApi._shared
{
    public class CallLog
    {
        public DateTime? startTime { get; set; }
        // seen to be null
        public DateTime? connectTime { get; set; }
        public DateTime? endTime { get; set; }
        // "incoming"
        public string? callDirection { get; set; }
        // "twoParty"
        public string? callType { get; set; }
        // "accepted", "missed"
        public string? callState { get; set; }
        // NO user mri!?
        public string? userParticipantId { get; set; }
        // user mri
        public string? originator { get; set; }
        // user mri
        public string? target { get; set; }
        public CallParticipant? originatorParticipant { get; set; }
        public CallParticipant? targetParticipant { get; set; }
        public string? callId { get; set; }
        public object? callAttributes { get; set; }
        public object? forwardingInfo { get; set; }
        public object? transferInfo { get; set; }
        public object? participants { get; set; }
        public object? participantList { get; set; }
        public object? threadId { get; set; }
        // "default"
        public string? sessionType { get; set; }
        public string? sharedCorrelationId { get; set; }
        public object? messageId { get; set; }
    }

    public class CallParticipant
    {
        // user mri
        public string? id { get; set; }
        // "default"
        public string? type { get; set; }
        // is empty for self
        public string? displayName { get; set; }
    }
}
