using System;
using System.Collections.Generic;
using System.IO;

namespace TeasmCompanion.TeamsTokenRetrieval
{
    public class TeamsTokenPathes
    {
        private static string LevelDbLdbFileSearchMask = "*.ldb";
        private static string LevelDbLogFileSearchMask = "*.log";
        // Environment.SpecialFolder.ApplicationData is AppData\Roaming on Windows and ~/.config on Linux
        private string TeamsLocalStoragePath_Windows { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Teams", "Local Storage", "leveldb");
        
        // Environment.SpecialFolder.LocalApplicationData is AppData\Local on Windows and  ~/.local/share on Linux
        private string ChromeLocalStoragePath_Windows { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "User Data", "Default", "Local Storage", "leveldb");
        private string EdgeLocalStoragePath_Windows { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Edge", "User Data", "Default", "Local Storage", "leveldb");
        private string ChromiumLocalStoragePath_Linux { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "chromium", "Default", "Local Storage", "leveldb");

        private List<string> GetLevelDbFilePathes(string fileSearchMask)
        {
            List<string> ldbFiles = new List<string>();
            if (Directory.Exists(TeamsLocalStoragePath_Windows))
            {
                ldbFiles.AddRange(Directory.EnumerateFiles(TeamsLocalStoragePath_Windows, fileSearchMask, SearchOption.TopDirectoryOnly));
            }
            if (Directory.Exists(ChromeLocalStoragePath_Windows))
            {
                ldbFiles.AddRange(Directory.EnumerateFiles(ChromeLocalStoragePath_Windows, fileSearchMask, SearchOption.TopDirectoryOnly));
            }
            if (Directory.Exists(EdgeLocalStoragePath_Windows))
            {
                ldbFiles.AddRange(Directory.EnumerateFiles(EdgeLocalStoragePath_Windows, fileSearchMask, SearchOption.TopDirectoryOnly));
            }
            if (Directory.Exists(ChromiumLocalStoragePath_Linux))
            {
                ldbFiles.AddRange(Directory.EnumerateFiles(ChromiumLocalStoragePath_Linux, fileSearchMask, SearchOption.TopDirectoryOnly));
            }
            return ldbFiles;
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
