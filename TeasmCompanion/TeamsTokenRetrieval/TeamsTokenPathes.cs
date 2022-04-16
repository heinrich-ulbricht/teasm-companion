using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TeasmCompanion.TeamsTokenRetrieval.Chrome;

namespace TeasmCompanion.TeamsTokenRetrieval
{

    public abstract class TeamsTokenPathes
    {
        private static string LevelDbLdbFileSearchMask = "*.ldb";
        private static string LevelDbLogFileSearchMask = "*.log";
        // Environment.SpecialFolder.ApplicationData is AppData\Roaming on Windows and ~/.config on Linux
        protected Configuration config;

        public TeamsTokenPathes(Configuration config)
        {
            this.config = config;
        }

        protected abstract IEnumerable<string> GetAllPathesToSearchForLevelDbFiles();

        private List<string> GetLevelDbFilePathes(string fileSearchMask)
        {
            var ldbFiles = new List<string>();

            var pathesToSearchForLdbFiles = GetAllPathesToSearchForLevelDbFiles();
            foreach (var pathToSearchIn in pathesToSearchForLdbFiles)
            {
                if (Directory.Exists(pathToSearchIn))
                {
                    ldbFiles.AddRange(Directory.EnumerateFiles(pathToSearchIn, fileSearchMask, SearchOption.TopDirectoryOnly));
                }
            }

            return ldbFiles;
        }

        public static Dictionary<string, string> GetChromeProfilePathes(string userDataDirPath)
        {

            if (Directory.Exists(userDataDirPath))
            {
                var chromeConfigFilePath = Path.Combine(userDataDirPath, "Local State");
                if (File.Exists(chromeConfigFilePath))
                {
                    var chromeConfigJson = File.ReadAllText(chromeConfigFilePath);
                    return GetChromeProfilePathesFromString(userDataDirPath, chromeConfigJson);
                }
            }

            return new Dictionary<string, string>();
        }

        public static Dictionary<string, string> GetChromeProfilePathesFromString(string userDataDirPath, string chromeConfigJson)
        {
            var result = new Dictionary<string, string>();
            var chromeConfig = JsonConvert.DeserializeObject<ChromeLocalState>(chromeConfigJson);
            if (chromeConfig.profile?.info_cache?.profiles?.Any() ?? false)
            {
                foreach (var profile in chromeConfig.profile?.info_cache?.profiles)
                {
                    var path = Path.Combine(userDataDirPath, profile.Key);
                    result.Add(profile.Value.name, path);
                }
            }

            return result;
        }

        public List<string> GetLevelDbLdbFilePathes()
        {
            return GetLevelDbFilePathes(LevelDbLdbFileSearchMask);
        }

        public List<string> GetLevelDbLogFilePathes()
        {
            return GetLevelDbFilePathes(LevelDbLogFileSearchMask);
        }
    }
}
