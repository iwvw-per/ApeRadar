using ApeRadar.Models;
using Newtonsoft.Json.Linq;

namespace ApeRadar.Utils
{
    static internal class WatchListUtils
    {
        public static void CreateNewWatchList(string filename)
        {
            _ = new WatchListRepository(filename).Read();
        }

        public static JObject ReadWatchList(string filename)
        {
            return new WatchListRepository(filename).Read();
        }

        public static void SaveWatchList(Player p, string filename)
        {
            new WatchListRepository(filename).Save(p);
        }
    }
}
