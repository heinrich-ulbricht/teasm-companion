using System;
using System.Collections.Generic;

namespace TeasmCompanion.TeamsInternal.TeamsInternalApi._shared
{
    /*
     * Sample payload:
     * 
        "quickReplyAugmentation": {
            "suggestedActivities": [
                {
                    "type": "suggestion",
                    "suggestedActions": {
                        "actions": [
                            {
                                "type": "imBack",
                                "title": "I try",
                                "value": "I try",
                                "channelData": {
                                    "type": "reply",
                                    "device": "Undefined",
                                    "id": "00000000-0000-0000-0000-000000000000",
                                    "utcTime": "2021-08-00T00:00:00.0000000Z",
                                    "targetMessageId": "0000000000000"
                                }
                            },
                            {
                                "type": "imBack",
                                "title": "I love it",
                                "value": "I love it",
                                "channelData": {
                                    "type": "reply",
                                    "device": "Undefined",
                                    "id": "00000000-0000-0000-0000-000000000000",
                                    "utcTime": "2021-08-00T00:00:00.0000000Z",
                                    "targetMessageId": "0000000000000"
                                }
                            },
                            {
                                "type": "imBack",
                                "title": "You know it",
                                "value": "You know it",
                                "channelData": {
                                    "type": "reply",
                                    "device": "Undefined",
                                    "id": "00000000-0000-0000-0000-000000000000",
                                    "utcTime": "2021-08-00T00:00:00.0000000Z",
                                    "targetMessageId": "0000000000000"
                                }
                            }
                        ]
                    },
                    "id": "00000000-0000-0000-0000-000000000000",
                    "timestamp": "2021-08-00T00:00:00.0000000+00:00",
                    "from": {
                        "id": "28:30005246-e706-444f-ba61-8647ef09db75",
                        "name": "Cortana"
                    },
                    "conversation": {
                        "isGroup": false,
                        "id": "19:00000000-0000-0000-0000-000000000000_00000000-0000-0000-0000-000000000000@unq.gbl.spaces"
                    },
                    "replyToId": "0000000000009"
                }
            ]
        }
    */


    public class Quickreplyaugmentation
    {
        public List<Suggestedactivity> suggestedActivities { get; set; }
    }

    public class Suggestedactivity
    {
        public string type { get; set; }
        public Suggestedactions suggestedActions { get; set; }
        public string id { get; set; }
        public DateTime timestamp { get; set; }
        public From from { get; set; }
        public Conversation conversation { get; set; }
        public string replyToId { get; set; }
    }

    public class Suggestedactions
    {
        public List<Action> actions { get; set; }
    }

    public class Action
    {
        public string type { get; set; }
        public string title { get; set; }
        public string value { get; set; }
        public Channeldata channelData { get; set; }
    }

    public class Channeldata
    {
        public string type { get; set; }
        public string device { get; set; }
        public string id { get; set; }
        public DateTime utcTime { get; set; }
        public string targetMessageId { get; set; }
    }

    public class From
    {
        public string id { get; set; }
        public string name { get; set; }
    }

    public class Conversation
    {
        public bool isGroup { get; set; }
        public string id { get; set; }
    }
}
