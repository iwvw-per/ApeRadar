using ApeRadar.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ApeRadar.Utils
{
    static internal class SoftwareUpdateUtils
    {
        private const string UpdateInfoUrl = "https://lxdev.org/aperadar/updateinfo/";
        private static readonly UpdateInstaller installer = new();
        private static readonly SemaphoreSlim updateLock = new(1, 1);

        public static async Task<bool> CheckForUpdates(bool silentShipList = false)
        {
            await updateLock.WaitAsync();
            try
            {
                UpdateManifest manifest = UpdateManifest.FromJson(JsonUtils.Parse(await NetworkUtils.HttpGet(UpdateInfoUrl)));

                if (!manifest.UpdateServerEnabled)
                {
                    return false;
                }

                bool softwareUpdateAvailable = Convert.ToInt32(manifest.SoftwareLatestDate) > Convert.ToInt32(BuildInfo.SoftwareDate);
                bool shipListUpdateAvailable = Convert.ToInt32(manifest.ShipListLatestDate) > Convert.ToInt32(ShipInfoUtils.GetShipInfoDate());

                if (!softwareUpdateAvailable && !shipListUpdateAvailable)
                {
                    return false;
                }

                if (softwareUpdateAvailable)
                {
                    if (MessageBox.Show($"{Application.Current.FindResource("MsgBoxSoftwareUpdateFound") as string}\n{Application.Current.FindResource("MsgBoxCurrentVersion") as string} {BuildInfo.SoftwareVersion} ({BuildInfo.SoftwareDate})\n{Application.Current.FindResource("MsgBoxLatestVersion") as string} {manifest.SoftwareLatestVersion} ({manifest.SoftwareLatestDate})\n{Application.Current.FindResource("MsgBoxUpdateComfirm") as string}", Application.Current.FindResource("MsgBoxUpdate") as string, MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                    {
                        NotificationMessageUtils.CreateMessage(MessageType.INFO, Application.Current.FindResource("NotificationMessageSoftwareUpdateDownloading") as string);
                        await installer.InstallSoftwareAsync(manifest);
                        NotificationMessageUtils.CreateMessage(MessageType.INFO, Application.Current.FindResource("NotificationMessageSoftwareUpdateComplete") as string);
                        MessageBox.Show(Application.Current.FindResource("MsgBoxSoftwareUpdateComplete") as string, Application.Current.FindResource("MsgBoxUpdate") as string, MessageBoxButton.OK, MessageBoxImage.Information);
                        Application.Current.Shutdown();
                        return true;
                    }
                }

                if (shipListUpdateAvailable)
                {
                    if (silentShipList)
                    {
                        LogUtils.WriteInfo($"Silently updating ship list from {ShipInfoUtils.GetShipInfoDate()} to {manifest.ShipListLatestDate}");
                        await installer.InstallShipListAsync(manifest);
                        ShipInfoUtils.ReadShipInfoFile(AppPaths.ShipInfoFile);
                        LogUtils.WriteInfo($"Silent ship list update completed: {ShipInfoUtils.GetShipInfoVersion()} ({ShipInfoUtils.GetShipInfoDate()})");
                        return true;
                    }

                    if (MessageBox.Show($"{Application.Current.FindResource("MsgBoxShiplistUpdateFound") as string}\n{Application.Current.FindResource("MsgBoxCurrentVersion") as string} {ShipInfoUtils.GetShipInfoVersion()} ({ShipInfoUtils.GetShipInfoDate()})\n{Application.Current.FindResource("MsgBoxLatestVersion") as string} {manifest.ShipListLatestVersion} ({manifest.ShipListLatestDate})\n{Application.Current.FindResource("MsgBoxUpdateComfirm") as string}", Application.Current.FindResource("MsgBoxUpdate") as string, MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                    {
                        NotificationMessageUtils.CreateMessage(MessageType.INFO, Application.Current.FindResource("NotificationMessageShiplistUpdateDownloading") as string);
                        await installer.InstallShipListAsync(manifest);
                        ShipInfoUtils.ReadShipInfoFile(AppPaths.ShipInfoFile);
                        NotificationMessageUtils.CreateMessage(MessageType.INFO, Application.Current.FindResource("NotificationMessageShiplistUpdateComplete") as string);
                        MessageBox.Show(Application.Current.FindResource("MsgBoxShiplistUpdateComplete") as string, Application.Current.FindResource("MsgBoxUpdate") as string, MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                LogUtils.WriteError("", ex);
                _ = ex.Message switch
                {
                    "HttpRequestFailed" => NotificationMessageUtils.CreateMessage(MessageType.ERROR, Application.Current.FindResource("NotificationMessageUpdateConnectionError") as string),
                    "FileHashInvalid" => NotificationMessageUtils.CreateMessage(MessageType.ERROR, Application.Current.FindResource("NotificationMessageUpdateFileHashError") as string),
                    _ => NotificationMessageUtils.CreateMessage(MessageType.ERROR, Application.Current.FindResource("NotificationMessageOtherError") as string),
                };
                return true;
            }
            finally
            {
                installer.CleanDownloadDirectory();
                updateLock.Release();
            }
        }

        public static void CleanOldVersionFiles()
        {
            installer.CleanOldVersionFiles();
        }
    }
}
