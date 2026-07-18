using System;
using System.IO;

namespace ApeRadar.Utils
{
    internal static class AppPaths
    {
        public static string BaseDirectory { get; } = AppContext.BaseDirectory;

        public static string UserDataDirectory { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ApeRadar");

        public static string ShipJsonDirectory => Path.Combine(BaseDirectory, "Resources", "Json");
        public static string ShipInfoFile => Path.Combine(ShipJsonDirectory, "ships.json");
        public static string WatchListFile => Path.Combine(UserDataDirectory, "WatchList.json");
        public static string WindowPlacementFile => Path.Combine(UserDataDirectory, "placement.config");
        public static string ScreenshotDirectory => Path.Combine(UserDataDirectory, "Screenshot");
        public static string DownloadDirectory => Path.Combine(Path.GetTempPath(), "ApeRadar", "Download");

        public static void EnsureUserDataDirectories()
        {
            Directory.CreateDirectory(UserDataDirectory);
            Directory.CreateDirectory(ScreenshotDirectory);
        }

        public static void MigrateUserFilesFromBaseDirectory()
        {
            EnsureUserDataDirectories();
            CopyIfMissing(Path.Combine(BaseDirectory, "WatchList.json"), WatchListFile);
            CopyIfMissing(Path.Combine(BaseDirectory, "placement.config"), WindowPlacementFile);
        }

        private static void CopyIfMissing(string source, string destination)
        {
            if (!File.Exists(source) || File.Exists(destination))
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(source, destination);
        }
    }
}
