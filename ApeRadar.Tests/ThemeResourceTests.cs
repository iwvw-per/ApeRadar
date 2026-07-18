using System.Xml.Linq;

namespace ApeRadar.Tests;

public sealed class ThemeResourceTests
{
    private static readonly string[] SharedColorKeys =
    [
        "AppBackgroundColor",
        "AppSurfaceColor",
        "AppSurfaceElevatedColor",
        "AppSurfaceAltColor",
        "AppInputBackgroundColor",
        "AppForegroundColor",
        "AppMutedForegroundColor",
        "AppDisabledForegroundColor",
        "AppBorderColor",
        "AppSelectionColor",
        "AppAccentColor",
        "AppSuccessColor",
        "AppWarningColor",
        "AppDangerColor",
        "AppInfoColor",
        "AppChartTextColor",
        "AppChartSeparatorColor"
    ];

    [Fact]
    public void LightAndDarkThemesExposeTheSameSemanticColorKeys()
    {
        HashSet<string> light = LoadKeys("LightTheme.xaml");
        HashSet<string> dark = LoadKeys("DarkTheme.xaml");

        foreach (string key in SharedColorKeys)
        {
            Assert.Contains(key, light);
            Assert.Contains(key, dark);
        }
    }

    [Fact]
    public void FluentTokensExposeSharedTypographyAndGeometry()
    {
        HashSet<string> tokens = LoadKeys("FluentTokens.xaml");

        Assert.Contains("AppFontFamily", tokens);
        Assert.Contains("AppFontSizeBody", tokens);
        Assert.Contains("AppControlCornerRadius", tokens);
        Assert.Contains("AppControlPadding", tokens);
    }

    private static HashSet<string> LoadKeys(string filename)
    {
        string path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "ApeRadar", "Resources", "Themes", filename));
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        return XDocument.Load(path)
            .Descendants()
            .Select(element => (string?)element.Attribute(x + "Key"))
            .OfType<string>()
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .ToHashSet(StringComparer.Ordinal);
    }
}
