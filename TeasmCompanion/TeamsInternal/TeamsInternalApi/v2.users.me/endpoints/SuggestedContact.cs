#nullable enable

namespace TeasmCompanion.TeamsInternal.TeamsInternalApi.v2.users.me.endpoints
{
    public class SuggestedContact
    {
        public string? userPrincipalName { get; set; }
        public string? email { get; set; }
        public bool? isShortProfile { get; set; }
        public string? displayName { get; set; }
        public string? mri { get; set; }
        public string? objectId { get; set; }
    }
}
