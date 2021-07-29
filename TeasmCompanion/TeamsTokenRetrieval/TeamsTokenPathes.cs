using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TeasmCompanion.TeamsTokenRetrieval.Chrome;

namespace TeasmCompanion.TeamsTokenRetrieval
{
    public class TeamsTokenPathes
    {
        private static string LevelDbLdbFileSearchMask = "*.ldb";
        private static string LevelDbLogFileSearchMask = "*.log";
        // Environment.SpecialFolder.ApplicationData is AppData\Roaming on Windows and ~/.config on Linux
        private static string TeamsLocalStoragePath_Windows { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Teams", "Local Storage", "leveldb");

        // Environment.SpecialFolder.LocalApplicationData is AppData\Local on Windows and  ~/.local/share on Linux
        private static string ChromeUserDataPath_Windows { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "User Data");
        private static string ChromeLocalStoragePath_Windows { get; } = Path.Combine(ChromeUserDataPath_Windows, "Default", "Local Storage", "leveldb");
        // Chrome SxS = Chrome Canary Build
        private static string ChromeSxsUserDataPath_Windows { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome SxS", "User Data");
        private static string ChromeSxsLocalStoragePath_Windows { get; } = Path.Combine(ChromeSxsUserDataPath_Windows, "Default", "Local Storage", "leveldb");


        private static string EdgeLocalStoragePath_Windows { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Edge", "User Data", "Default", "Local Storage", "leveldb");
        private static string ChromiumLocalStoragePath_Linux { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "chromium", "Default", "Local Storage", "leveldb");

        private Configuration config;

        public TeamsTokenPathes(Configuration config)
        {
            this.config = config;
        }

        private List<string> GetAllPathesToSearchForLdbFiles()
        {
            var result = new List<string>();
            result.Add(TeamsLocalStoragePath_Windows);
            result.Add(ChromeLocalStoragePath_Windows);
            result.Add(ChromeSxsLocalStoragePath_Windows);
            result.Add(EdgeLocalStoragePath_Windows);
            result.Add(ChromiumLocalStoragePath_Linux);

            var chromeProfilePathes = GetChromeProfilePathes(ChromeSxsUserDataPath_Windows).Union(GetChromeProfilePathes(ChromeUserDataPath_Windows));
            foreach (var chromeProfilePath in chromeProfilePathes)
            {
                if (config.ChromeNonDefaultProfileNames.Contains(chromeProfilePath.Key, StringComparer.InvariantCultureIgnoreCase))
                {
                    result.Add(Path.Combine(chromeProfilePath.Value, "Local Storage", "leveldb"));
                }
            }

            return result;
        }

        private List<string> GetLevelDbFilePathes(string fileSearchMask)
        {
            var ldbFiles = new List<string>();

            var pathesToSearchForLdbFiles = GetAllPathesToSearchForLdbFiles();
            foreach (var pathToSearchIn in pathesToSearchForLdbFiles)
            {
                if (Directory.Exists(pathToSearchIn))
                {
                    ldbFiles.AddRange(Directory.EnumerateFiles(pathToSearchIn, fileSearchMask, SearchOption.TopDirectoryOnly));
                }
            }

            return ldbFiles;
        }

        public Dictionary<string, string> GetChromeProfilePathes(string userDataDirPath)
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

        public Dictionary<string, string> GetChromeProfilePathesFromString(string userDataDirPath, string chromeConfigJson)
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
