using ApeRadar.Models;
using ApeRadar.Utils;

namespace ApeRadar.Tests;

public sealed class ShipCatalogTests : IDisposable
{
    private readonly string tempDirectory = Path.Combine(Path.GetTempPath(), $"ApeRadar.Tests.{Guid.NewGuid():N}");

    public ShipCatalogTests()
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
    public void Load_ReadsVersionDateAndLocalizedShipData()
    {
        string shipsFile = WriteShipsJson();
        ShipCatalog catalog = new();

        catalog.Load(shipsFile);

        Assert.Equal("15.3", catalog.Version);
        Assert.Equal("20260422", catalog.Date);
        Assert.Equal("Des Moines", catalog.GetShipNameByID("100", Language.EN_US, "Unknown"));
        Assert.Equal("得梅因", catalog.GetShipNameByID("100", Language.ZH_CN, "Unknown"));
        Assert.Equal("Cruiser", catalog.GetShipTypeByID("100"));
        Assert.Equal(10, catalog.GetShipTierByID("100"));
    }

    [Fact]
    public void UnknownShip_ReturnsFallbackNameTypeAndTier()
    {
        string shipsFile = WriteShipsJson();
        ShipCatalog catalog = new();

        catalog.Load(shipsFile);

        Assert.Equal("Unknown", catalog.GetShipNameByID("404", Language.EN_US, "Unknown"));
        Assert.Equal("Unknown", catalog.GetShipTypeByID("404"));
        Assert.Equal(0, catalog.GetShipTierByID("404"));
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
            }
          }
        }
        """);
        return shipsFile;
    }
}
