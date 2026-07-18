using ApeRadar.Models;
using ApeRadar.Utils;

namespace ApeRadar.Tests;

public sealed class WatchListRepositoryTests : IDisposable
{
    private readonly string tempDirectory = Path.Combine(Path.GetTempPath(), $"ApeRadar.Tests.{Guid.NewGuid():N}");

    public WatchListRepositoryTests()
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
    public void Read_CreatesDefaultWatchListWhenMissing()
    {
        WatchListRepository repository = new(Path.Combine(tempDirectory, "WatchList.json"));

        var watchList = repository.Read();

        Assert.NotNull(watchList["RU"]);
        Assert.NotNull(watchList["EU"]);
        Assert.NotNull(watchList["NA"]);
        Assert.NotNull(watchList["ASIA"]);
        Assert.NotNull(watchList["CN"]);
    }

    [Fact]
    public void Save_AddsUpdatesAndRemovesPlayer()
    {
        string file = Path.Combine(tempDirectory, "WatchList.json");
        WatchListRepository repository = new(file);
        Player player = new("tester", "123", Server.ASIA, WatchStatus.POSITIVE);

        repository.Save(player);
        Assert.Equal("Positive", repository.Read()["ASIA"]!["123"]!["status"]!.Value<string>());

        player.WatchStatus = WatchStatus.CHEATER;
        repository.Save(player);
        Assert.Equal("Cheater", repository.Read()["ASIA"]!["123"]!["status"]!.Value<string>());

        player.WatchStatus = WatchStatus.NONE;
        repository.Save(player);
        Assert.Null(repository.Read()["ASIA"]!["123"]);
    }

    [Fact]
    public void Read_RecreatesWatchListWhenJsonIsInvalid()
    {
        string file = Path.Combine(tempDirectory, "WatchList.json");
        File.WriteAllText(file, "{bad-json");

        var watchList = new WatchListRepository(file).Read();

        Assert.NotNull(watchList["ASIA"]);
        Assert.DoesNotContain("bad-json", File.ReadAllText(file));
    }
}
