using ApeRadar.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace ApeRadar.Utils
{
    internal sealed class WatchListRepository
    {
        private readonly string filename;

        public WatchListRepository(string? filename = null)
        {
            this.filename = filename ?? AppPaths.WatchListFile;
        }

        public JObject Read()
        {
            if (!File.Exists(filename))
            {
                CreateNewWatchList();
            }

            try
            {
                using FileStream fs = new(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using StreamReader sr = new(fs);
                return JsonUtils.Parse(sr.ReadToEnd());
            }
            catch
            {
                CreateNewWatchList();
                using FileStream fs = new(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using StreamReader sr = new(fs);
                return JsonUtils.Parse(sr.ReadToEnd());
            }
        }

        public void Save(Player player)
        {
            JObject watchList = Read();
            string serverName = ServerExt.GetNameByServer(player.Server);

            if (watchList[serverName]!.SelectToken(player.ID) != null)
            {
                if (player.WatchStatus == WatchStatus.NONE)
                {
                    ((JObject)watchList[serverName]!).Remove(player.ID);
                }
                else
                {
                    watchList[serverName]![player.ID]!["status"] = WatchStatusExt.GetNameByStatus(player.WatchStatus);
                }
            }
            else if (player.WatchStatus != WatchStatus.NONE)
            {
                JObject playerJson = new()
                {
                    ["name"] = player.Name,
                    ["status"] = WatchStatusExt.GetNameByStatus(player.WatchStatus)
                };
                ((JObject)watchList[serverName]!).Add(player.ID, playerJson);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(filename)!);
            using FileStream fs = new(filename, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
            using StreamWriter sw = new(fs);
            sw.WriteLine(JsonConvert.SerializeObject(watchList, Formatting.Indented));
        }

        private void CreateNewWatchList()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filename)!);
            JObject watchList = JsonUtils.Parse("{\"RU\":{},\"EU\":{},\"NA\":{},\"ASIA\":{},\"CN\":{}}");
            using FileStream fs = new(filename, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
            using StreamWriter sw = new(fs);
            sw.WriteLine(JsonConvert.SerializeObject(watchList, Formatting.Indented));
        }
    }
}
