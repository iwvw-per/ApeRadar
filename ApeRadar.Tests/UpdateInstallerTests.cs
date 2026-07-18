using ApeRadar.Utils;
using System.Reflection;
using System.Security.Cryptography;

namespace ApeRadar.Tests;

public sealed class UpdateInstallerTests : IDisposable
{
    private readonly string tempDirectory = Path.Combine(Path.GetTempPath(), $"ApeRadar.Tests.{Guid.NewGuid():N}");

    public UpdateInstallerTests()
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
    public void CopySoftwarePackage_CopiesAllowedFilesAndSkipsRuntimeFiles()
    {
        string source = Path.Combine(tempDirectory, "source");
        string destination = Path.Combine(tempDirectory, "destination");
        Directory.CreateDirectory(source);
        Directory.CreateDirectory(Path.Combine(source, "Log"));
        Directory.CreateDirectory(Path.Combine(source, "Screenshot"));

        File.WriteAllText(Path.Combine(source, "README.txt"), "readme");
        File.WriteAllText(Path.Combine(source, "ApeRadar.exe"), "exe");
        File.WriteAllText(Path.Combine(source, "WatchList.json"), "{}");
        File.WriteAllText(Path.Combine(source, "placement.config"), "placement");
        File.WriteAllText(Path.Combine(source, "Log", "Log.txt"), "log");
        File.WriteAllText(Path.Combine(source, "Screenshot", "shot.png"), "png");

        InvokePrivateStatic("CopySoftwarePackage", source, destination);

        Assert.True(File.Exists(Path.Combine(destination, "README.txt")));
        Assert.True(File.Exists(Path.Combine(destination, "ApeRadar.exe")));
        Assert.False(File.Exists(Path.Combine(destination, "WatchList.json")));
        Assert.False(File.Exists(Path.Combine(destination, "placement.config")));
        Assert.False(File.Exists(Path.Combine(destination, "Log", "Log.txt")));
        Assert.False(File.Exists(Path.Combine(destination, "Screenshot", "shot.png")));
    }

    [Fact]
    public void GetSafeDestinationPath_RejectsPathsOutsideDestination()
    {
        TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() =>
            InvokePrivateStatic("GetSafeDestinationPath", tempDirectory, "..\\evil.txt"));

        Assert.IsType<FileFormatException>(ex.InnerException);
        Assert.Equal("FileFormatIncorrect", ex.InnerException!.Message);
    }

    [Fact]
    public void ValidateSha256_ThrowsWhenHashDoesNotMatch()
    {
        string file = Path.Combine(tempDirectory, "package.zip");
        File.WriteAllText(file, "payload");

        TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() =>
            InvokePrivateStatic("ValidateSha256", file, new string('0', SHA256.HashSizeInBytes * 2)));

        Assert.IsType<FileFormatException>(ex.InnerException);
        Assert.Equal("FileHashInvalid", ex.InnerException!.Message);
    }

    private static object? InvokePrivateStatic(string methodName, params object[] arguments)
    {
        MethodInfo method = typeof(UpdateInstaller).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)!;
        return method.Invoke(null, arguments);
    }
}
