using System.Collections.Generic;

#nullable enable

namespace TeasmCompanion.Registries
{
    public static class KnownBots
    {
        // contains _some_ known bots; sources: 3.1.5-config-prod.min.js, ...
        public static Dictionary<TeamsParticipant, string> KnownBotNames { get; }

        static KnownBots()
        {
            KnownBotNames = new Dictionary<TeamsParticipant, string>
            {
                { (TeamsParticipant)"28:6d2635c7-5f7b-4274-b0b5-e44d64e20dd9", "Interceptor Bot" },
                { (TeamsParticipant)"28:f7206cef-8727-438b-9724-e1c508e3e54b", "Interceptor Bot" },
                { (TeamsParticipant)"28:22e50a9b-80cc-4eab-a092-ce64796d28d7", "Interceptor Bot" },
                { (TeamsParticipant)"28:b1902c3e-b9f7-4650-9b23-5772bd429747", "Closed Captions Bot" },
                { (TeamsParticipant)"28:123425f9-0c72-4bd8-8814-7cb6b02dfc3f", "Voice Collection Bot" },
                { (TeamsParticipant)"28:72679538-1a2c-4dde-9484-3c2dc6b4d88e", "Teams Echo Bot" },
                { (TeamsParticipant)"28:a8d7def2-47c1-4367-94da-240a880a85b7", "T-Bot Int" },
                { (TeamsParticipant)"28:c8f1cda6-5af5-40bf-a9f3-2fa21b72adde", "WhoBot" },
                { (TeamsParticipant)"28:8d88f59b-ae61-4300-bec0-caace7d28446", "Chebyshev" },
                { (TeamsParticipant)"28:a0d491ca-3b80-4855-bdf5-4559a348ad21", "Glue Bot" },
                { (TeamsParticipant)"28:bdd75849-e0a6-4cce-8fc1-d7c0d4da43e5", "Recording Bot" },
                { (TeamsParticipant)"28:4be36d18-a394-4f94-ad18-fb20df412d7a", "Broadcast Prod Bot" },
                { (TeamsParticipant)"28:8cf0f6d9-65dc-464b-a2cb-8f66a9767358", "Grid View Bot" },
                { (TeamsParticipant)"28:3d59cb08-f597-4e49-9add-a05f9735152b", "Audience Bot" },
                { (TeamsParticipant)"28:358f0194-6b0e-4dd3-af35-c24fe8a9ec87", "Flow" }, // note: don't call this "Flow Bot" as the bot's display name in messages is just "Flow"
                { (TeamsParticipant)"28:0af95b67-5890-4306-9c1c-a8591cead09e", "Forms" },
                { (TeamsParticipant)"28:817c2506-de4a-4795-971e-371ea75a03ed", "Polly" }
            };
        }
    }
}
