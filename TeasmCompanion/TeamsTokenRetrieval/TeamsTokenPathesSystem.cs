using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TeasmCompanion.TeamsTokenRetrieval
{

    // TODO: Split into parent class and two sub classes, one that loads from default locations and one from a given one (for auto-login browsers)
    public class TeamsTokenPathesSystem : TeamsTokenPathes
    {
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

        public TeamsTokenPathesSystem(Configuration config) : base(config)
        {
        }

        protected override IEnumerable<string> GetAllPathesToSearchForLevelDbFiles()
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
    }
}