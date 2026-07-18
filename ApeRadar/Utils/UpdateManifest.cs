using Newtonsoft.Json.Linq;
using System;

namespace ApeRadar.Utils
{
    internal sealed class UpdateManifest
    {
        private UpdateManifest()
        {
        }

        public bool UpdateServerEnabled { get; private init; }
        public bool SoftwareHashValidateEnabled { get; private init; }
        public bool ShipListHashValidateEnabled { get; private init; }
        public string SoftwareLatestVersion { get; private init; } = "";
        public string SoftwareLatestDate { get; private init; } = "";
        public string SoftwareLatestUrl { get; private init; } = "";
        public string SoftwareLatestSha256 { get; private init; } = "";
        public string ShipListLatestVersion { get; private init; } = "";
        public string ShipListLatestDate { get; private init; } = "";
        public string ShipListLatestUrl { get; private init; } = "";
        public string ShipListLatestSha256 { get; private init; } = "";

        public string SoftwareLatestFileName => GetFileNameFromUrl(SoftwareLatestUrl);
        public string ShipListLatestFileName => GetFileNameFromUrl(ShipListLatestUrl);

        public static UpdateManifest FromJson(JObject json)
        {
            return new UpdateManifest
            {
                UpdateServerEnabled = json["update_server_enabled"]!.Value<bool>(),
                SoftwareHashValidateEnabled = json["software_hash_validate_enabled"]?.Value<bool>() ?? true,
                ShipListHashValidateEnabled = json["shiplist_hash_validate_enabled"]?.Value<bool>() ?? true,
                SoftwareLatestVersion = json["software_latest_version"]!.Value<string>()!,
                SoftwareLatestDate = json["software_latest_date"]!.Value<string>()!,
                SoftwareLatestUrl = json["software_latest_url"]!.Value<string>()!,
                SoftwareLatestSha256 = json["software_latest_sha256"]!.Value<string>()!,
                ShipListLatestVersion = json["shiplist_latest_version"]!.Value<string>()!,
                ShipListLatestDate = json["shiplist_latest_date"]!.Value<string>()!,
                ShipListLatestUrl = json["shiplist_latest_url"]!.Value<string>()!,
                ShipListLatestSha256 = json["shiplist_latest_sha256"]!.Value<string>()!,
            };
        }

        private static string GetFileNameFromUrl(string url)
        {
            return url[(url.LastIndexOf('/') + 1)..];
        }
    }
}
