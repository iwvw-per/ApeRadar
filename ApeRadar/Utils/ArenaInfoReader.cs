using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;

namespace ApeRadar.Utils
{
    internal sealed class ArenaInfoReader
    {
        private DateTimeOffset latestFileWriteTime = DateTimeOffset.MinValue;

        public string GetLatestTempArenaInfoFile(string gamePath, bool requireFileToBeNewer)
        {
            if (string.IsNullOrWhiteSpace(gamePath))
            {
                return string.Empty;
            }

            string replayDirectory = Path.Combine(gamePath, "replays");
            if (!Directory.Exists(replayDirectory))
            {
                latestFileWriteTime = DateTimeOffset.MinValue;
                return string.Empty;
            }

            string[] tempArenaInfoPaths = Directory.GetFiles(replayDirectory, "tempArenaInfo.json", SearchOption.AllDirectories);
            if (tempArenaInfoPaths.Length == 0)
            {
                LogUtils.WriteDebug("tempArenaInfo file not found. ");
                latestFileWriteTime = DateTimeOffset.MinValue;
                return string.Empty;
            }

            foreach (string path in tempArenaInfoPaths)
            {
                LogUtils.WriteDebug($"tempArenaInfo file path={path}");
            }

            string latestFileName = tempArenaInfoPaths
                .Select(filename => new FileInfo(filename))
                .OrderByDescending(file => file.LastWriteTime)
                .First()
                .FullName;

            DateTimeOffset latestWriteTime = new FileInfo(latestFileName).LastWriteTime;
            bool isNewer = latestWriteTime > latestFileWriteTime;
            latestFileWriteTime = latestWriteTime;

            return isNewer || !requireFileToBeNewer ? latestFileName : string.Empty;
        }

        public JObject ReadTempArenaInfoFile(string filename)
        {
            string tempArenaInfo;
            using FileStream fs = new(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using StreamReader sr = new(fs);
            byte[] buffer = new byte[4];

            int firstByte = sr.Peek();

            if (firstByte == 0x7B)
            {
                tempArenaInfo = sr.ReadToEnd();
            }
            else if (firstByte == 0x12)
            {
                fs.Seek(0, SeekOrigin.Begin);
                fs.ReadExactly(buffer, 0, 4);
                if (Enumerable.SequenceEqual(buffer, new byte[] { 0x12, 0x32, 0x34, 0x11 }))
                {
                    fs.Seek(8, SeekOrigin.Begin);
                    fs.ReadExactly(buffer, 0, 4);
                    sr.DiscardBufferedData();
                    tempArenaInfo = sr.ReadToEnd()[..BitConverter.ToInt32(buffer, 0)];
                }
                else
                {
                    throw new FileFormatException("FileFormatIncorrect");
                }
            }
            else
            {
                throw new FileFormatException("FileFormatIncorrect");
            }

            LogUtils.WriteInfo($"tempArenaInfo:{tempArenaInfo}");
            try
            {
                return JsonUtils.Parse(tempArenaInfo);
            }
            catch (Exception ex)
            {
                throw new FileFormatException("FileFormatIncorrect", ex);
            }
        }
    }
}
