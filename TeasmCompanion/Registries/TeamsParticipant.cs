using Newtonsoft.Json;
using System;
using System.Text.RegularExpressions;

#nullable enable

namespace TeasmCompanion.Registries
{
    public enum ParticipantKind
    {
        Unknown,
        User,
        TeamsChat,
        AppOrBot,
        Notification
    }

    /**
     * Represents a Teams participant.
     * 
     * Note that a Teams "participant" here in the app can also be e.g. a chat when it acts as the source of messages. Participants initially can come as plain
     * GUID, as MRI or as contact URL.
     * 
     * Sample user IDs:
     * - as GUID: 00000000-0000-beef-0000-000000000000
     * - as MRI, type user: 8:orgid:00000000-0000-beef-0000-000000000000
     * - as MRI, type bot/app: 28:358f0194-6b0e-4dd3-af35-c24fe8a9ec87
     * - as MRI, type?: 8:teamsvisitor:a111a1a111aa111a1aa1aa1a11aaaaa1
     * - as contact URL: https://emea.ng.msg.teams.microsoft.com/v1/users/ME/contacts/8:orgid:00000000-0000-beef-0000-000000000000
     * - as contact URL: https://emea.ng.msg.teams.microsoft.com/v1/users/ME/contacts/8:teamsvisitor:a111a1a111aa111a1aa1aa1a11aaaaa1"
     * - as contact URL: https://notifications.skype.net/v1/users/ME/contacts/28:integration:a1aa11aaaa
     */
    [JsonObject(MemberSerialization.OptIn)]
    public struct TeamsParticipant : IComparable
    {
        // prefixes taken from here: https://microsoftgraph.github.io/microsoft-graph-comms-samples/docs/core/Microsoft.Graph.IdentitySetExtensions.html
        public const string DodAppParticipantPrefix = "28:dod-global:";
        public const string DodDirectoryAppParticipantPrefix = "28:dod:";
        public const string DodDirectoryParticipantPrefix = "8:dod:";
        public const string DodOnPremisesParticipantPrefix = "8:dod-sfb:";
        public const string EncryptedParticipantPrefix = "29:";
        public const string GcchAppParticipantPrefix = "28:gcch-global:";
        public const string GcchDirectoryAppParticipantPrefix = "28:gcch:";
        public const string GcchDirectoryParticipantPrefix = "8:gcch:";
        public const string GcchOnPremisesParticipantPrefix = "8:gcch-sfb:";
        public const string PhoneParticipantPrefix = "4:";
        public const string PublicAppParticipantPrefix = "28:";
        public const string PublicDirectoryAppParticipantPrefix = "28:orgid:";
        public const string PublicDirectoryParticipantPrefix = "8:orgid:";
        public const string PublicOnPremisesParticipantPrefix = "8:sfb:";
        public const string SkypeParticipantPrefix = "8:";
        public const string TeamsVisitorParticipantPrefix = "8:teamsvisitor:";

        public static readonly TeamsParticipant Null = new TeamsParticipant(null);
        // user mri or chat id, possibly more
        [JsonProperty]
        public string? Mri { get; private set; }
        public string? Prefix { get; private set; }
        public string? Id { get; private set; }
        public ParticipantKind? Kind { get; private set; }
        public bool IsValid { get; private set; }
        public bool IdIsGuid { get; private set; }
        public static readonly string MriPatternClosed = @"^(?:https://.+?/ME/contacts/)?(?<prefix>(?:(?<type>[0-9]{1,4}):(?<subtype>.+?):)|(?:(?<type>[0-9]{1,4}):))?(?<id>[a-zA-Z0-9-]+)$";
        public static readonly string MriPatternOpen = @"(?:https://.+?/ME/contacts/)?(?<prefix>(?:(?<type>[0-9]{1,4}):(?<subtype>.+?):)|(?:(?<type>[0-9]{1,4}):))?(?<id>[a-zA-Z0-9-]+)";
        // catches "19:~@unq.gbl.spaces" "19:meeting_~@thread.v2" etc... a bit broad, let's see if this works
        public static readonly string ChatIdPattern = @"19:[a-zA-Z0-9-_]+?@[a-z.0-9]+";
        public static readonly string ContactUrlPattern = @"https://.+?/ME/contacts/";
        public static readonly string GuidPattern = @"[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}";
        public static readonly string PlaceholderPattern = @"(?:(?:User|Bot) )?\{\{([a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12})\}\}";

        [JsonConstructor]
        public TeamsParticipant(string? mri)
        {
            IsValid = false;
            if (mri != null)
            {
                // remove contact url part (e.g. "https://emea.ng.msg.teams.microsoft.com/v1/users/ME/contacts/")
                Mri = Regex.Replace(mri.Trim(), ContactUrlPattern, "");
            } else
            {
                Mri = null;
            }

            // sample for skypeId contained: 8:orgid:00000000-0000-beef-0000-000000000000;skypeid=orgid:00000000-0000-beef-0000-000000000000
            if ((Mri?.ToLowerInvariant().Contains(";skype") ?? false) && ((!Mri?.ToLowerInvariant().EndsWith("skype")) ?? false))
            {
               //Debugger.Break(); <-- enable to discover potentially unknown ID formats
            }

            Prefix = "";
            IdIsGuid = false;
            Kind = ParticipantKind.Unknown;

            Id = Mri;
            if (!string.IsNullOrEmpty(Id))
            {
                var mriMatches = Regex.Matches(Id, MriPatternClosed); // use closed pattern, otherwise chat IDs would match partially
                if (mriMatches.Count > 0)
                {
                    // type is 8, 28 etc.; can be missing
                    var participantType = mriMatches[0].Groups["type"].ToString();
                    // subtype is "orgid", "teamsvisitor" etc.; can be empty
                    // var participantSubType = mriMatches[0].Groups["subtype"].ToString();
                    Id = mriMatches[0].Groups["id"].ToString();
                    Prefix = mriMatches[0].Groups["prefix"].ToString();

                    // without prefix we only accept GUIds, no arbitrary strings; WITH prefix the id can be an arbitrary string
                    IdIsGuid = Regex.Match(Id, GuidPattern).Success;
                    if (!string.IsNullOrWhiteSpace(participantType) || IdIsGuid)
                    {
                        IsValid = true;
                    }

                    switch (participantType)
                    {
                        case "4": // phone
                        case "8":
                            Kind = ParticipantKind.User;
                            break;
                        case "19":
                            Kind = ParticipantKind.TeamsChat;
                            break;
                        case "28":
                            Kind = ParticipantKind.AppOrBot;
                            break;
                        case "48":
                            Kind = ParticipantKind.Notification;
                            break;
                        default:
                            Kind = ParticipantKind.Unknown;
                            break;
                    }
                }
                if (Kind == ParticipantKind.Unknown)
                {
                    // still unknown? let's see
                    // something like "19:meeting_MW~Yz@thread.v2"
                    if (Mri?.StartsWith("19:") ?? false)
                    {
                        Kind = ParticipantKind.TeamsChat;
                        IsValid = true;
                    } else if ((mri?.StartsWith("48:") ?? false) || (mri?.Contains("contacts/28:") ?? false))
                    {
                        // something like "https://notifications.skype.net/v1/users/ME/conversations/48:notifications" - not sure if this is possible as sender, but it exists
                        Kind = ParticipantKind.Notification;
                    }
                }
            }
        }

        public static TeamsParticipant FromFirstValid(params string[] userIdCandidates)
        {
            foreach (string idString in userIdCandidates)
            {
                TeamsParticipant id = new TeamsParticipant(idString);
                if (id.IsValid)
                    return id;
            }

            return Null;
        }

        public override bool Equals(object? obj)
        {
            if (obj is TeamsParticipant participant)
            {
                var result = Mri?.Equals(participant.Mri, StringComparison.InvariantCultureIgnoreCase) ?? false;
                if (!result)
                {
                    // check IDs but only if they are GUIDs
                    result = (Id?.Equals(participant.Id, StringComparison.InvariantCultureIgnoreCase) ?? false) && IdIsGuid && participant.IdIsGuid;
                }
                return result;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            if (Id == null)
            {
                return 0;
            }
            return Id.GetHashCode();
        }

        public override string? ToString()
        {
            return Id?.ToString() ?? "null";
        }

        public int CompareTo(object? obj)
        {
            if (obj == null && Id != null)
            {
                return 1;
            } else
            if (obj != null && Id == null)
            {
                return -1;
            } else
            if (obj == null && Id == null)
            {
                return 0;
            }
            else
            if (obj is TeamsParticipant)
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                return Id.CompareTo(((TeamsParticipant)obj).Id);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }
            else
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                return Id.CompareTo(obj.ToString());
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }
        }

        public static implicit operator string?(TeamsParticipant userId) => userId.Id;
        public static explicit operator TeamsParticipant(string? s) => new TeamsParticipant(s);
    }
}
