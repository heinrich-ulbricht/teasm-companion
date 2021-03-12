using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace TeasmCompanion.ProcessedTeamsObjects
{
    public class ProcessedTeamsUserComparer : IEqualityComparer<ProcessedTeamsUser>
    {
        public bool Equals([AllowNull] ProcessedTeamsUser x, [AllowNull] ProcessedTeamsUser y)
        {
            return x?.UserId == y?.UserId;
        }

        public int GetHashCode([DisallowNull] ProcessedTeamsUser obj)
        {
            return obj.UserId.GetHashCode();
        }
    }
}
