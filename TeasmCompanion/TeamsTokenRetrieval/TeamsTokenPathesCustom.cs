using System.Collections.Generic;

namespace TeasmCompanion.TeamsTokenRetrieval
{
    public class TeamsTokenPathesCustom : TeamsTokenPathes
    {
        private List<string> pathes { get; } = new();
        public TeamsTokenPathesCustom(Configuration config, IEnumerable<string> pathes) : base(config)
        {
            this.pathes.AddRange(pathes);
        }

        protected override IEnumerable<string> GetAllPathesToSearchForLevelDbFiles()
        {
            var result = new List<string>();
            result.AddRange(pathes);
            return result;
        }
    }
}