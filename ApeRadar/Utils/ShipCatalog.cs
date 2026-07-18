using ApeRadar.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.IO;

namespace ApeRadar.Utils
{
    internal sealed class ShipCatalog
    {
        private JObject? shipInfo;

        public void Load(string filename)
        {
            using FileStream fs = new(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using StreamReader sr = new(fs);
            shipInfo = JsonUtils.Parse(sr.ReadToEnd());
        }

        public string Version => shipInfo!["version"]!.Value<string>()!;
        public string Date => shipInfo!["date"]!.Value<string>()!;

        public string GetShipNameByID(string id, Language language, string unknownShipName)
        {
            JObject ships = (JObject)shipInfo!["ships"]!;
            if (!ships.ContainsKey(id))
            {
                return unknownShipName;
            }

            string languageKey = language switch
            {
                Language.AUTO => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "zh" ? "name_zh-cn" : "name_en-us",
                Language.EN_US => "name_en-us",
                Language.ZH_CN => "name_zh-cn",
                _ => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "zh" ? "name_zh-cn" : "name_en-us",
            };

            return shipInfo["ships"]![id]![languageKey]!.Value<string>()!;
        }

        public string GetShipTypeByID(string id)
        {
            JObject ships = (JObject)shipInfo!["ships"]!;
            return ships.ContainsKey(id) ? shipInfo!["ships"]![id]!["type"]!.Value<string>()! : "Unknown";
        }

        public int GetShipTierByID(string id)
        {
            JObject ships = (JObject)shipInfo!["ships"]!;
            return ships.ContainsKey(id) ? Convert.ToInt32(shipInfo!["ships"]![id]!["tier"]!) : 0;
        }
    }
}
