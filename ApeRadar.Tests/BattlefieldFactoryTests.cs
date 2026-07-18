using ApeRadar.Models;
using ApeRadar.Utils;

namespace ApeRadar.Tests;

public sealed class BattlefieldFactoryTests : IDisposable
{
    private readonly string tempDirectory = Path.Combine(Path.GetTempPath(), $"ApeRadar.Tests.{Guid.NewGuid():N}");

    public BattlefieldFactoryTests()
    {
        Directory.CreateDirectory(tempDirectory);
        ShipInfoUtils.ReadShipInfoFile(WriteShipsJson());
        Properties.Settings.Default.ShipNameLanguage = "EN_US";
        Properties.Settings.Default.WinrateTypeUsed = 0;
    }

    public void Dispose()
    {
        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Fact]
    public void Create_MapsShipDataAndSplitsTeams()
    {
        List<Player> players =
        [
            CreatePlayer("ally", "0", "100", 0.52, 0.51, 1000),
            CreatePlayer("enemy", "2", "200", 0.48, 0.49, 900)
        ];

        var battlefield = new BattlefieldFactory().Create("PVP", DateTimeOffset.Parse("2026-02-01T12:34:56Z"), players);

        Assert.Single(battlefield.Allies);
        Assert.Single(battlefield.Enemies);
        Assert.Equal("Des Moines", battlefield.Allies[0].ShipName);
        Assert.Equal("Destroyer", battlefield.Enemies[0].ShipType);
        Assert.Equal(0.52, battlefield.AllyAvgAccountWinrate);
        Assert.Equal(0.48, battlefield.EnemyAvgAccountWinrate);
    }

    [Fact]
    public void Create_HandlesNoValidWinrateWithoutNan()
    {
        List<Player> players =
        [
            CreatePlayer("hidden-ally", "0", "100", -1, -1, -1),
            CreatePlayer("hidden-enemy", "2", "200", -1, -1, -1)
        ];

        var battlefield = new BattlefieldFactory().Create("PVP", DateTimeOffset.UtcNow, players);

        Assert.Equal(0, battlefield.AllyAvgAccountWinrate);
        Assert.Equal(0, battlefield.AllyAvgWeightedWinrate);
        Assert.Equal(0, battlefield.AllyAvgBattleCount);
        Assert.Equal(0, battlefield.EnemyAvgAccountWinrate);
        Assert.Equal(0, battlefield.EnemyAvgWeightedWinrate);
        Assert.Equal(0, battlefield.EnemyAvgBattleCount);
    }

    private static Player CreatePlayer(string name, string relation, string shipId, double accountWinrate, double weightedWinrate, double battles)
    {
        Player player = new(name, Server.ASIA, relation, shipId)
        {
            AccountWinrate = accountWinrate,
            WeightedWinrate = weightedWinrate,
            Battles = battles
        };
        return player;
    }

    private string WriteShipsJson()
    {
        string shipsFile = Path.Combine(tempDirectory, "ships.json");
        File.WriteAllText(shipsFile, """
        {
          "version": "15.3",
          "date": "20260422",
          "ships": {
            "100": {
              "name_en-us": "Des Moines",
              "name_zh-cn": "得梅因",
              "type": "Cruiser",
              "tier": 10
            },
            "200": {
              "name_en-us": "Shimakaze",
              "name_zh-cn": "岛风",
              "type": "Destroyer",
              "tier": 10
            }
          }
        }
        """);
        return shipsFile;
    }
}
