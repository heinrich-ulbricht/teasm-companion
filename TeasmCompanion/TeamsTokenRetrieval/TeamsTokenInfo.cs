using System;
using TeasmCompanion.Registries;

#nullable enable

namespace TeasmCompanion.TeamsTokenRetrieval
{
    public class TeamsTokenInfo
    {
        public TeamsParticipant UserId { get; private set; }
        public TeamsTokenType TokenType { get; private set; }
        public string TokenString { get; private set; }
        public string AuthHeader { get; private set; }
        public DateTime ValidFromUtc { get; private set; }
        public DateTime ValidToUtc { get; private set; }

        public bool IsValid()
        {
            var now = DateTime.UtcNow;
            return ValidFromUtc <= now && now <= ValidToUtc;
        }

        public TeamsTokenInfo(TeamsParticipant userId, TeamsTokenType tokenType, string tokenString, string authHeader, DateTime validFromUtc, DateTime validToUtc)
        {
            UserId = userId;
            TokenType = tokenType;
            TokenString = tokenString;
            AuthHeader = authHeader;
            ValidFromUtc = validFromUtc;
            ValidToUtc = validToUtc;
        }

        public override bool Equals(object? obj)
        {
            if (obj is TeamsTokenInfo)
            {
                var other = (TeamsTokenInfo)obj;
                return other.TokenType == TokenType && other.TokenString == TokenString && other.ValidToUtc == ValidToUtc && other.ValidFromUtc == ValidFromUtc && other.AuthHeader == AuthHeader;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return $"{TokenType}{TokenString}{ValidToUtc}{ValidFromUtc}{AuthHeader}".GetHashCode();
        }
    }
}
