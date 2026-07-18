using ApeRadar.Utils;
using Newtonsoft.Json.Linq;
using System.Text;

namespace ApeRadar.Tests;

public sealed class ArenaInfoReaderTests : IDisposable
{
    private readonly string tempDirectory = Path.Combine(Path.GetTempPath(), $"ApeRadar.Tests.{Guid.NewGuid():N}");

    public ArenaInfoReaderTests()
    {
        Directory.CreateDirectory(tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Fact]
    public void ReadTempArenaInfoFile_ReadsPlainJson()
    {
        string file = Path.Combine(tempDirectory, "tempArenaInfo.json");
        File.WriteAllText(file, SampleArenaInfoJson("plain"), Encoding.UTF8);

        JObject arenaInfo = new ArenaInfoReader().ReadTempArenaInfoFile(file);

        Assert.Equal("plain", arenaInfo["matchGroup"]!.Value<string>());
    }

    [Fact]
    public void ReadTempArenaInfoFile_ReadsBinaryHeaderFormat()
    {
        string file = Path.Combine(tempDirectory, "tempArenaInfo.json");
        byte[] jsonBytes = Encoding.UTF8.GetBytes(SampleArenaInfoJson("binary"));
        using FileStream stream = new(file, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
        stream.Write(new byte[] { 0x12, 0x32, 0x34, 0x11 });
        stream.Write(new byte[4]);
        stream.Write(BitConverter.GetBytes(jsonBytes.Length));
        stream.Write(jsonBytes);
        stream.Write(Encoding.UTF8.GetBytes("trailing-bytes"));
        stream.Flush();

        JObject arenaInfo = new ArenaInfoReader().ReadTempArenaInfoFile(file);

        Assert.Equal("binary", arenaInfo["matchGroup"]!.Value<string>());
    }

    [Fact]
    public void ReadTempArenaInfoFile_ThrowsForInvalidFormat()
    {
        string file = Path.Combine(tempDirectory, "tempArenaInfo.json");
        File.WriteAllText(file, "not-json", Encoding.UTF8);

        FileFormatException ex = Assert.Throws<FileFormatException>(() => new ArenaInfoReader().ReadTempArenaInfoFile(file));
        Assert.Equal("FileFormatIncorrect", ex.Message);
    }

    [Fact]
    public void ReadTempArenaInfoFile_CanReadWhileFileIsOpenForWriting()
    {
        string file = Path.Combine(tempDirectory, "tempArenaInfo.json");
        File.WriteAllText(file, SampleArenaInfoJson("shared"), Encoding.UTF8);
        using FileStream _ = new(file, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete);

        JObject arenaInfo = new ArenaInfoReader().ReadTempArenaInfoFile(file);

        Assert.Equal("shared", arenaInfo["matchGroup"]!.Value<string>());
    }

    [Fact]
    public void GetLatestTempArenaInfoFile_ReturnsOnlyNewerFileWhenRequired()
    {
        string replayDirectory = Path.Combine(tempDirectory, "replays", "20260707_120000");
        Directory.CreateDirectory(replayDirectory);
        string file = Path.Combine(replayDirectory, "tempArenaInfo.json");
        File.WriteAllText(file, SampleArenaInfoJson("latest"), Encoding.UTF8);

        ArenaInfoReader reader = new();

        Assert.Equal(file, reader.GetLatestTempArenaInfoFile(tempDirectory, true));
        Assert.Equal(string.Empty, reader.GetLatestTempArenaInfoFile(tempDirectory, true));
        Assert.Equal(file, reader.GetLatestTempArenaInfoFile(tempDirectory, false));
    }

    private static string SampleArenaInfoJson(string matchGroup)
    {
        return $$"""
        {"matchGroup":"{{matchGroup}}","dateTime":"01.02.2026 12:34:56","vehicles":[]}
        """;
    }
}
