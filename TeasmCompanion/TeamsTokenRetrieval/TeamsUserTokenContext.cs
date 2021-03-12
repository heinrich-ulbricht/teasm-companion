using System.Linq;
using System.Collections.Generic;
using System.Reactive.Linq;
using TeasmCompanion.Registries;
using TeasmCompanion.Interfaces;

#nullable enable

namespace TeasmCompanion.TeamsTokenRetrieval
{
    public class TeamsUserTokenContext
    {
        public TeamsParticipant UserId { get; private set; }
        private Dictionary<TeamsTokenType, List<TeamsTokenInfo>> tokens = new Dictionary<TeamsTokenType, List<TeamsTokenInfo>>();
        // like "https://emea.ng.msg.teams.microsoft.com" or "https://de.ng.msg.teams.microsoft.com"
        private string? chatServiceUrl;
        public string? ChatServiceUrl
        {
            get
            {
                if (string.IsNullOrWhiteSpace(chatServiceUrl))
                    throw new TeasmCompanionException("Endpoint URL not set");
                return chatServiceUrl;
            }
            set
            {
                lock (this)
                {
                    chatServiceUrl = value;
                }
            }
        }

        public TeamsUserTokenContext(TeamsParticipant userId)
        {
            UserId = userId;
        }

        private void PurgeInvalidTokens()
        {
            lock (tokens)
            {
                foreach (var tokenList in tokens.Values)
                {
                    tokenList.RemoveAll(t => !t.IsValid());
                }
            }
        }

        public void AddOrReplaceTokenInfo(TeamsTokenType tokenType, TeamsTokenInfo tokenInfo)
        {
            PurgeInvalidTokens();

            lock (tokens)
            {
                if (!tokens.TryGetValue(tokenType, out var tokenInfoList))
                {
                    tokenInfoList = new List<TeamsTokenInfo>();
                    tokens[tokenType] = tokenInfoList;
                }
                tokenInfoList.RemoveAll(t => t.Equals(tokenInfo));
                tokenInfoList.Add(tokenInfo);
            }
        }

        public TeamsTokenInfo? this[TeamsTokenType tokenType]
        {
            get
            {
                PurgeInvalidTokens();
                lock (tokens)
                {
                    return tokens.Where(c => c.Key == tokenType).SelectMany(c => c.Value.Where(v => v.IsValid())).FirstOrDefault();
                }
            }
        }
    }
}
