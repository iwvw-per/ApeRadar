using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace ApeRadar.Utils
{
    internal sealed class UpdateInstaller
    {
        private static readonly HashSet<string> RuntimeDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Log",
            "Screenshot",
            "Download"
        };

        private static readonly HashSet<string> RuntimeFileNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "placement.config",
            "WatchList.json"
        };

        private static readonly HashSet<string> AllowedSoftwareFileExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".exe",
            ".dll",
            ".config",
            ".json",
            ".txt"
        };

        private static readonly HashSet<string> OccupiedFileNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "ApeRadar.exe",
            "libSkiaSharp.dll"
        };

        public async Task InstallSoftwareAsync(UpdateManifest manifest)
        {
            string packageFile = await DownloadAndValidateAsync(
                manifest.SoftwareLatestUrl,
                manifest.SoftwareLatestFileName,
                manifest.SoftwareLatestSha256,
                manifest.SoftwareHashValidateEnabled);

            string extractDirectory = ExtractPackage(packageFile);
            string packageRoot = ResolvePackageRoot(extractDirectory);

            CopySoftwarePackage(packageRoot, AppPaths.BaseDirectory);
        }

        public async Task InstallShipListAsync(UpdateManifest manifest)
        {
            string packageFile = await DownloadAndValidateAsync(
                manifest.ShipListLatestUrl,
                manifest.ShipListLatestFileName,
                manifest.ShipListLatestSha256,
                manifest.ShipListHashValidateEnabled);

            string extractDirectory = ExtractPackage(packageFile);
            Directory.CreateDirectory(AppPaths.ShipJsonDirectory);

            foreach (string jsonFile in Directory.GetFiles(extractDirectory, "*.json", SearchOption.AllDirectories))
            {
                string destination = Path.Combine(AppPaths.ShipJsonDirectory, Path.GetFileName(jsonFile));
                File.Copy(jsonFile, destination, true);
            }
        }

        public void CleanDownloadDirectory()
        {
            string fullDownloadDirectory = Path.GetFullPath(AppPaths.DownloadDirectory);
            if (Directory.Exists(fullDownloadDirectory) && fullDownloadDirectory.StartsWith(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase))
            {
                Directory.Delete(fullDownloadDirectory, true);
            }
        }

        public void CleanOldVersionFiles()
        {
            foreach (string filename in OccupiedFileNames)
            {
                string backupFile = Path.Combine(AppPaths.BaseDirectory, $"{filename}.bak");
                if (File.Exists(backupFile))
                {
                    File.Delete(backupFile);
                }
            }
        }

        private static async Task<string> DownloadAndValidateAsync(string url, string fileName, string expectedSha256, bool validateHash)
        {
            Directory.CreateDirectory(AppPaths.DownloadDirectory);
            string packageFile = Path.Combine(AppPaths.DownloadDirectory, fileName);
            await NetworkUtils.HttpDownloadFile(url, packageFile);

            if (validateHash)
            {
                ValidateSha256(packageFile, expectedSha256);
            }

            return packageFile;
        }

        private static void ValidateSha256(string filename, string expectedSha256)
        {
            using SHA256 sha = SHA256.Create();
            using FileStream fs = new(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            string actualSha256 = BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "");
            if (!actualSha256.Equals(expectedSha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new FileFormatException("FileHashInvalid");
            }
        }

        private static string ExtractPackage(string packageFile)
        {
            string extractDirectory = Path.Combine(AppPaths.DownloadDirectory, "Extracted");
            if (Directory.Exists(extractDirectory))
            {
                Directory.Delete(extractDirectory, true);
            }

            Directory.CreateDirectory(extractDirectory);
            ZipFile.ExtractToDirectory(packageFile, extractDirectory, true);
            return extractDirectory;
        }

        private static string ResolvePackageRoot(string extractDirectory)
        {
            string apeRadarDirectory = Path.Combine(extractDirectory, "ApeRadar");
            return Directory.Exists(apeRadarDirectory) ? apeRadarDirectory : extractDirectory;
        }

        private static void CopySoftwarePackage(string sourceDirectory, string destinationDirectory)
        {
            foreach (string sourceFile in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
                if (ShouldSkipSoftwareFile(relativePath))
                {
                    continue;
                }

                string destinationFile = GetSafeDestinationPath(destinationDirectory, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
                if (OccupiedFileNames.Contains(Path.GetFileName(destinationFile)) && File.Exists(destinationFile))
                {
                    File.Move(destinationFile, $"{destinationFile}.bak", true);
                }

                File.Copy(sourceFile, destinationFile, true);
            }
        }

        private static bool ShouldSkipSoftwareFile(string relativePath)
        {
            string[] segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (segments.Any(segment => RuntimeDirectoryNames.Contains(segment)))
            {
                return true;
            }

            string fileName = Path.GetFileName(relativePath);
            if (RuntimeFileNames.Contains(fileName))
            {
                return true;
            }

            return !AllowedSoftwareFileExtensions.Contains(Path.GetExtension(relativePath));
        }

        private static string GetSafeDestinationPath(string destinationDirectory, string relativePath)
        {
            string destinationRoot = Path.GetFullPath(destinationDirectory);
            string destinationPath = Path.GetFullPath(Path.Combine(destinationRoot, relativePath));
            if (!destinationPath.StartsWith(destinationRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new FileFormatException("FileFormatIncorrect");
            }

            return destinationPath;
        }
    }
}
