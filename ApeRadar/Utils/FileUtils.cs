using Newtonsoft.Json.Linq;

namespace ApeRadar.Utils
{
    static internal class FileUtils
    {
        private static readonly ArenaInfoReader reader = new();

        public static string GetLatestTempArenaInfoFile(bool requireFileToBeNewer)
        {
            return reader.GetLatestTempArenaInfoFile(Properties.Settings.Default.GamePath, requireFileToBeNewer);
        }

        public static JObject ReadTempArenaInfoFile(string filename)
        {
            return reader.ReadTempArenaInfoFile(filename);
        }
    }
}
