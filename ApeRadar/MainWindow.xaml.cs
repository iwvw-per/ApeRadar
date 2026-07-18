using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using System.IO;
using System.Diagnostics;
using System.Windows.Threading;

using ApeRadar.Models;
using ApeRadar.Utils;
using ApeRadar.Utils.PlayerStats;
using ApeRadar.Utils.Sorters;

using Newtonsoft.Json.Linq;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Globalization;

namespace ApeRadar
{
    partial class MainWindow : Window
    {
        private readonly ArenaInfoReader arenaInfoReader = new();
        private readonly BattleLoadCoordinator battleLoadCoordinator;
        private bool isLoadingBattle;

        private void SwitchLanguage(Language language)
        {
            Application.Current.Resources.MergedDictionaries[0] = LanguageExt.GetResourceDictionaryByLanguage(language);
            LabelGamePathIsSetOrNot.GetBindingExpression(ContentProperty).UpdateTarget();

            ComboBoxLanguage.SelectionChanged -= ComboBoxLanguage_SelectionChanged;
            ComboBoxLanguage.Items.Clear();
            ComboBoxLanguage.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemLanguageAuto"), Value = "AUTO" });
            ComboBoxLanguage.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemLanguageEnglish"), Value = "EN_US" });
            ComboBoxLanguage.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemLanguageSimplifiedChinese"), Value = "ZH_CN" });
            ComboBoxLanguage.SelectedValue = Properties.Settings.Default.Language;
            ComboBoxLanguage.SelectionChanged += ComboBoxLanguage_SelectionChanged;

            ComboBoxChartType.SelectionChanged -= ComboBoxChartType_SelectionChanged;
            ComboBoxChartType.Items.Clear();
            ComboBoxChartType.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemChartTypeShipTypeAndShipTierAndWinrateDescending"), Value = "0" });
            ComboBoxChartType.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemChartTypeShipTierAndShipTypeAndWinrateDescending"), Value = "1" });
            ComboBoxChartType.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemChartTypeShipTypeAndWinrateDescending"), Value = "2" });
            ComboBoxChartType.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemChartTypeShipTierAndWinrateDescending"), Value = "3" });
            ComboBoxChartType.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemChartTypeWinrateDescending"), Value = "4" });
            ComboBoxChartType.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemChartTypeWinrateAscending"), Value = "5" });
            ComboBoxChartType.SelectedValue = Properties.Settings.Default.WinrateChartType;
            ComboBoxChartType.SelectionChanged += ComboBoxChartType_SelectionChanged;

            ComboBoxMirrored.SelectionChanged -= ComboBoxMirrored_SelectionChanged;
            ComboBoxMirrored.Items.Clear();
            ComboBoxMirrored.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemMirroredDisplayDisabled"), Value = "False" });
            ComboBoxMirrored.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemMirroredDisplayEnabled"), Value = "True" });
            ComboBoxMirrored.SelectedValue = Properties.Settings.Default.EnemiesDisplayMirrored.ToString();
            ComboBoxMirrored.SelectionChanged += ComboBoxMirrored_SelectionChanged;

            ComboBoxSortBy.SelectionChanged -= ComboBoxSortBy_SelectionChanged;
            ComboBoxSortBy.Items.Clear();
            ComboBoxSortBy.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemSortByShipTypeAndShipTierAndWinrateDescending"), Value = "0" });
            ComboBoxSortBy.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemSortByShipTierAndShipTypeAndWinrateDescending"), Value = "1" });
            ComboBoxSortBy.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemSortByShipTypeAndWinrateDescending"), Value = "2" });
            ComboBoxSortBy.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemSortByShipTierAndWinrateDescending"), Value = "3" });
            ComboBoxSortBy.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemSortByWinrateDescending"), Value = "4" });
            ComboBoxSortBy.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemSortByWinrateAscending"), Value = "5" });
            ComboBoxSortBy.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemSortByBattlesDescending"), Value = "6" });
            ComboBoxSortBy.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemSortByBattlesAscending"), Value = "7" });
            ComboBoxSortBy.SelectedValue = Properties.Settings.Default.PlayerListSortBy;
            ComboBoxSortBy.SelectionChanged += ComboBoxSortBy_SelectionChanged;

            ComboBoxPlayerNamesVisibility.SelectionChanged -= ComboBoxPlayerNamesVisibility_SelectionChanged;
            ComboBoxPlayerNamesVisibility.Items.Clear();
            ComboBoxPlayerNamesVisibility.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemPlayerNamesVisible"), Value = "True" });
            ComboBoxPlayerNamesVisibility.Items.Add(new ListItem() { Content = Application.Current.FindResource("ComboBoxItemPlayerNamesHidden"), Value = "False" });
            ComboBoxPlayerNamesVisibility.SelectedValue = Properties.Settings.Default.PlayerNamesVisibility.ToString();
            ComboBoxPlayerNamesVisibility.SelectionChanged += ComboBoxPlayerNamesVisibility_SelectionChanged;
        }

        private void SwitchSorting(int sorting)
        {
            if (this.DataContext is not Battlefield battlefield)
            {
                return;
            }
            ListCollectionView? alliesCollectionView = CollectionViewSource.GetDefaultView(battlefield.Allies) as ListCollectionView;
            ListCollectionView? enemiesCollectionView = CollectionViewSource.GetDefaultView(battlefield.Enemies) as ListCollectionView;

            alliesCollectionView!.CustomSort = enemiesCollectionView!.CustomSort = sorting switch
            {
                0 => new CustomSorterByShipTypeAndShipTierAndWinrateDescending(),
                1 => new CustomSorterByShipTierAndShipTypeAndWinrateDescending(),
                2 => new CustomSorterByShipTypeAndWinrateDescending(),
                3 => new CustomSorterByShipTierAndWinrateDescending(),
                4 => new CustomSorterByWinrateDescending(),
                5 => new CustomSorterByWinrateAscending(),
                6 => new CustomSorterByBattlesDescending(),
                7 => new CustomSorterByBattlesAscending(),
                _ => new CustomSorterByShipTypeAndShipTierAndWinrateDescending(),
            };
        }

        private void SwitchWinrateChartType(int chartType)
        {
            if (this.DataContext is not Battlefield battlefield)
            {
                return;
            }
            WinrateChart.Series = ChartUtils.GetWinrateChartSeries(battlefield, chartType);
            WinrateChart.Sections = ChartUtils.GetWinrateChartSections(battlefield, chartType);
        }

        private void RefreshDataGridColumns(bool mirrored)
        {
            DataGridAlliesList.Columns.Clear();
            DataGridAlliesList.Columns.Add(TryFindResource("AlliesNameColumn") as DataGridTemplateColumn);
            DataGridAlliesList.Columns.Add(TryFindResource("AlliesStatisticsColumn") as DataGridTemplateColumn);
            DataGridAlliesList.Columns.Add(TryFindResource("AlliesTagColumn") as DataGridTemplateColumn);
            if (mirrored)
            {
                DataGridEnemiesList.Columns.Clear();
                DataGridEnemiesList.Columns.Add(TryFindResource("EnemiesTagColumn") as DataGridTemplateColumn);
                DataGridEnemiesList.Columns.Add(TryFindResource("EnemiesStatisticsColumnMirrored") as DataGridTemplateColumn);
                DataGridEnemiesList.Columns.Add(TryFindResource("EnemiesNameColumnMirrored") as DataGridTemplateColumn);
            }
            else
            {
                DataGridEnemiesList.Columns.Clear();
                DataGridEnemiesList.Columns.Add(TryFindResource("EnemiesNameColumn") as DataGridTemplateColumn);
                DataGridEnemiesList.Columns.Add(TryFindResource("EnemiesStatisticsColumn") as DataGridTemplateColumn);
                DataGridEnemiesList.Columns.Add(TryFindResource("EnemiesTagColumn") as DataGridTemplateColumn);
            }
        }

        private void ForceUpdateDataGridColumnWidth()
        {
            foreach (DataGridColumn col in DataGridAlliesList.Columns)
            {
                col.Width = 0;
            }
            foreach (DataGridColumn col in DataGridEnemiesList.Columns)
            {
                col.Width = 0;
            }
            DataGridAlliesList.UpdateLayout();
            DataGridEnemiesList.UpdateLayout();
            foreach (DataGridColumn col in DataGridAlliesList.Columns)
            {
                col.Width = DataGridLength.Auto;
            }
            foreach (DataGridColumn col in DataGridEnemiesList.Columns)
            {
                col.Width = DataGridLength.Auto;
            }
        }

        private void ApplyChartTheme()
        {
            SKColor chartTextColor = ThemeManager.GetSkColor("AppChartTextColor", SKColors.Black);
            SKColor chartSeparatorColor = ThemeManager.GetSkColor("AppChartSeparatorColor", SKColors.LightGray);
            SKColor chartCrosshairColor = ThemeManager.GetSkColor("AppChartCrosshairColor", SKColors.Gray);
            SKColor chartSurfaceColor = ThemeManager.GetSkColor("AppSurfaceColor", new SKColor(255, 255, 255));
            bool hasBattleData = DataContext is Battlefield battlefield && battlefield.Allies.Count + battlefield.Enemies.Count > 0;

            WinrateChart.TooltipTextPaint = new SolidColorPaint
            {
                Color = chartTextColor,
                SKTypeface = SKTypeface.FromFamilyName(WinrateChart.FontFamily.Source)
            };
            WinrateChart.TooltipBackgroundPaint = new SolidColorPaint(chartSurfaceColor);
            WinrateChart.DrawMarginFrame = new DrawMarginFrame
            {
                Fill = new SolidColorPaint(chartSurfaceColor),
                Stroke = new SolidColorPaint(chartSeparatorColor) { StrokeThickness = 1 }
            };
            KDEChart.TooltipTextPaint = new SolidColorPaint { Color = chartTextColor };
            KDEChart.TooltipBackgroundPaint = new SolidColorPaint(chartSurfaceColor);
            KDEChart.DrawMarginFrame = new DrawMarginFrame
            {
                Fill = new SolidColorPaint(chartSurfaceColor),
                Stroke = new SolidColorPaint(chartSeparatorColor) { StrokeThickness = 1 }
            };
            WinrateChart.XAxes = new Axis[] { new Axis { IsVisible = false } };
            WinrateChart.YAxes = new Axis[]
            {
                new Axis
                {
                    MinLimit = hasBattleData ? null : 0.4,
                    MaxLimit = hasBattleData ? null : 0.7,
                    Labeler = d => { return d.ToString("p1"); },
                    LabelsPaint = new SolidColorPaint(chartTextColor),
                    SeparatorsPaint = new SolidColorPaint(chartSeparatorColor),
                    CrosshairPaint = new SolidColorPaint(chartCrosshairColor)
                }
            };
            KDEChart.XAxes = new Axis[]
            {
                new Axis
                {
                    MinLimit = hasBattleData ? null : 0.4,
                    MaxLimit = hasBattleData ? null : 0.7,
                    Labeler = d => { return d.ToString("p1"); },
                    LabelsPaint = new SolidColorPaint(chartTextColor),
                    SeparatorsPaint = new SolidColorPaint(chartSeparatorColor),
                    CrosshairPaint = new SolidColorPaint(chartCrosshairColor)
                }
            };
            KDEChart.YAxes = new Axis[] { new Axis { IsVisible = false } };
        }

        private void ApplyTheme()
        {
            ThemeManager.ApplyWindowTheme(this);
            ApplyChartTheme();

            if (DataContext is Battlefield battlefield)
            {
                SwitchWinrateChartType(Properties.Settings.Default.WinrateChartType);
                KDEChart.Series = ChartUtils.GetKDEChartSeries(battlefield);
            }

            DataGridAlliesList.Items.Refresh();
            DataGridEnemiesList.Items.Refresh();
            DataGridNotificationMessages.Items.Refresh();
        }

        private void ThemeManager_ThemeChanged(object? sender, EventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(ApplyTheme);
                return;
            }

            ApplyTheme();
        }

        public MainWindow()
        {
            battleLoadCoordinator = new BattleLoadCoordinator(
                arenaInfoReader,
                new ServerResolver(),
                new WatchListRepository(),
                new BattlefieldFactory(),
                new IPlayerStatsAdapter[]
                {
                    new VortexPlayerStatsAdapter(),
                    new WgPublicPlayerStatsAdapter(),
                    new YuyukoProxyPlayerStatsAdapter()
                });

            InitializeComponent();
            ThemeManager.RegisterWindow(this);
            ThemeManager.ThemeChanged += ThemeManager_ThemeChanged;
            Closed += (_, _) => ThemeManager.ThemeChanged -= ThemeManager_ThemeChanged;

            if (Properties.Settings.Default.DebugMode)
            {
                LogUtils.SetLogLevel(log4net.Core.Level.Debug);
            }
            else
            {
                LogUtils.SetLogLevel(log4net.Core.Level.Info);
            }

            LogUtils.WriteDebug("Debug Mode");
            LogUtils.WriteInfo($"SoftwareVersion: {BuildInfo.SoftwareVersion}");
            LogUtils.WriteInfo($"SoftwareDate: {BuildInfo.SoftwareDate}");

            LogUtils.WriteInfo($"OS Version: {Environment.OSVersion.VersionString}");
            Version currentVersion = Environment.OSVersion.Version;
            if (currentVersion.CompareTo(new Version("6.2")) < 0)
            {
                LogUtils.WriteInfo($"OS is Windows 7 or earlier. Some emoji chars may be displayed incorrectly.");
            }

            if (!Properties.Settings.Default.SettingsUpgradeDone)
            {
                LogUtils.WriteInfo($"Settings Upgrade Done");
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.SettingsUpgradeDone = true;
            }

            //solve old version settings migration problem
            try
            {
                LanguageExt.GetLanguageByName(Properties.Settings.Default.Language);
            }
            catch
            {
                Properties.Settings.Default.Language = LanguageExt.GetNameByLanguage(Models.Language.AUTO);
            }

            try
            {
                LanguageExt.GetLanguageByName(Properties.Settings.Default.ShipNameLanguage);
            }
            catch
            {
                Properties.Settings.Default.ShipNameLanguage = LanguageExt.GetNameByLanguage(Models.Language.AUTO);
            }

            try
            {
                ServerExt.GetServerByName(Properties.Settings.Default.Server);
            }
            catch
            {
                Properties.Settings.Default.Server = ServerExt.GetNameByServer(Server.AUTO);
            }

            try
            {
                ServerExt.GetServerByName(Properties.Settings.Default.SecondaryServer);
            }
            catch
            {
                Properties.Settings.Default.SecondaryServer = ServerExt.GetNameByServer(Server.RU);
            }

            Properties.Settings.Default.Save();

            ((App)Application.Current).WindowPlace.Register(this);

            NotificationMessageUtils.InitializeNotificationMessageDataGrid(DataGridNotificationMessages);
            NetworkUtils.InitializeHttpClient();

            AppPaths.MigrateUserFilesFromBaseDirectory();
            ShipInfoUtils.ReadShipInfoFile(AppPaths.ShipInfoFile);
            LogUtils.WriteInfo($"ShipInfoFileVersion: {ShipInfoUtils.GetShipInfoVersion()}");
            LogUtils.WriteInfo($"ShipInfoFileDate: {ShipInfoUtils.GetShipInfoDate()}");

            SoftwareUpdateUtils.CleanOldVersionFiles();

            if (Properties.Settings.Default.CheckForUpdatesOnStartup)
            {
                _ = SoftwareUpdateUtils.CheckForUpdates(silentShipList: true);
            }

            ApplyTheme();

            SwitchLanguage(LanguageExt.GetLanguageByName(Properties.Settings.Default.Language));
            RefreshDataGridColumns(Properties.Settings.Default.EnemiesDisplayMirrored);

            DispatcherTimer timer = new()
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += new EventHandler(Timer_Tick);
            timer.Start();
            LogUtils.WriteInfo("Timer Start");
        }

        private async void Timer_Tick(object? sender, EventArgs e)
        {
            string latestFileName = arenaInfoReader.GetLatestTempArenaInfoFile(Properties.Settings.Default.GamePath, true);
            if (latestFileName != "")
            {
                await ReadPlayersListAndGetDataFromServer(latestFileName);
            }
        }

        private async Task ReadPlayersListAndGetDataFromServer(string filename)
        {
            LogUtils.WriteInfo("Reading Players List");
            LogUtils.WriteInfo($"gamePath={Properties.Settings.Default.GamePath}");
            LogUtils.WriteInfo($"filename={filename}");

            if (isLoadingBattle)
            {
                return;
            }

            try
            {
                isLoadingBattle = true;
                BtnRefresh.IsEnabled = false;
                BtnOpen.IsEnabled = false;

                NotificationMessageUtils.CreateMessage(MessageType.INFO, FindResource("NotificationMessageRetrivingData") as string);

                BattleLoadOptions options = BattleLoadOptions.FromSettings(
                    filename,
                    attempt => NotificationMessageUtils.CreateMessage(MessageType.INFO, $"{FindResource("NotificationMessageRetrying")}{attempt}{FindResource("NotificationMessageAttempt")}"));

                BattleLoadResult result = await battleLoadCoordinator.LoadAsync(options);

                LogUtils.WriteInfo($"server={ServerExt.GetNameByServer(result.Server)}");
                LogUtils.WriteInfo($"secondaryServer={ServerExt.GetNameByServer(result.SecondaryServer)}");
                LogUtils.WriteInfo($"playerCount={result.PlayerCount}");

                Battlefield battlefield = result.Battlefield;
                DataContext = battlefield;
                ApplyChartTheme();

                if (Properties.Settings.Default.YuyukoAPIPushEnabled)
                {
                    ApiUtils.YuyukoApiPushBattlefieldInfo(battlefield);
                }

                TxtOutputText.Text = result.OutputText;
                if (Properties.Settings.Default.OutputTextAutoCopy && Properties.Settings.Default.OutputTextUnlock)
                {
                    Clipboard.SetDataObject(TxtOutputText.Text);
                }

                SwitchSorting(Properties.Settings.Default.PlayerListSortBy);
                SwitchWinrateChartType(Properties.Settings.Default.WinrateChartType);
                KDEChart.Series = ChartUtils.GetKDEChartSeries(battlefield);

                NotificationMessageUtils.CreateMessage(MessageType.INFO, FindResource("NotificationMessageDataRetrieved") as string);
            }
            catch (Exception ex)
            {
                LogUtils.WriteError("", ex);
                _ = ex.Message switch
                {
                    "FileFormatIncorrect" => NotificationMessageUtils.CreateMessage(MessageType.ERROR, FindResource("NotificationMessageFileError") as string),
                    "ServerAutoDetectionFailed" => NotificationMessageUtils.CreateMessage(MessageType.ERROR, FindResource("NotificationMessageServerAutoDetectionFailed") as string),
                    "HttpRequestFailed" => NotificationMessageUtils.CreateMessage(MessageType.ERROR, FindResource("NotificationMessageConnectionError") as string),
                    "JsonStringNotValid" => NotificationMessageUtils.CreateMessage(MessageType.ERROR, FindResource("NotificationMessageJsonError") as string),
                    _ => NotificationMessageUtils.CreateMessage(MessageType.ERROR, FindResource("NotificationMessageOtherError") as string),
                };
                if (ex.Message != "FileFormatIncorrect" && ex.Message != "ServerAutoDetectionFailed" && Properties.Settings.Default.MaximumRetryAttemptsOnError > 0)
                {
                    NotificationMessageUtils.CreateMessage(MessageType.INFO, FindResource("NotificationMessageMaximumAttemptsReached") as string);
                }
            }
            finally
            {
                BtnRefresh.IsEnabled = true;
                BtnOpen.IsEnabled = true;
                isLoadingBattle = false;
            }
        }

        private void BtnConfig_Click(object sender, RoutedEventArgs e)
        {
            ConfigWindow configWindow = new()
            {
                Owner = this
            };
            configWindow.ShowDialog();
            ForceUpdateDataGridColumnWidth();
        }

        private void ComboBoxLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Properties.Settings.Default.Language = ComboBoxLanguage.SelectedValue.ToString()!;
            Properties.Settings.Default.Save();
            SwitchLanguage(LanguageExt.GetLanguageByName(Properties.Settings.Default.Language));
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            string latestFileName = arenaInfoReader.GetLatestTempArenaInfoFile(Properties.Settings.Default.GamePath, false);
            if (latestFileName != "")
            {
                await ReadPlayersListAndGetDataFromServer(latestFileName);
            }
        }

        private async void BtnOpen_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog dialog = new();
            dialog.Filter = "*.json, *.wowsreplay|*.json;*.wowsreplay";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                await ReadPlayersListAndGetDataFromServer(dialog.FileName);
            }
        }

        private void ContextMenuCheckOnWoWSNumbers_Click(object sender, RoutedEventArgs e)
        {
            MenuItem? menu = sender as MenuItem;
            Player? p = menu!.DataContext as Player;
            Process.Start("explorer.exe", $"{ServerExt.GetWoWSNumbersUrlStringByServer(p!.Server)}/player/{p.ID}%2C{p.Name}/");
        }

        private void ContextMenuCheckOnWoWSOfficialSite_Click(object sender, RoutedEventArgs e)
        {
            MenuItem? menu = sender as MenuItem;
            Player? p = menu!.DataContext as Player;
            Process.Start("explorer.exe", $"https://profile.{ServerExt.GetFullUrlStringByServer(p!.Server)}/statistics/{p.ID}/");
        }

        private void BtnScreenshot_Click(object sender, RoutedEventArgs e)
        {
            int gridH = (int)MainWindowGrid.ActualHeight;
            int gridW = (int)MainWindowGrid.ActualWidth;
            RenderTargetBitmap bitmap = new(gridW, gridH, 96, 96, PixelFormats.Default);
            Rect rect = new(0, 0, gridW, gridH);
            DrawingVisual drawingVisual = new();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawRectangle(Background, null, rect);
            }
            bitmap.Render(drawingVisual);
            bitmap.Render(MainWindowGrid);
            PngBitmapEncoder encoder = new();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            string datetimestr = DateTimeOffset.Now.ToString("yyyyMMdd_HHmmss_ffff");
            Directory.CreateDirectory(AppPaths.ScreenshotDirectory);
            string filePath = Path.Combine(AppPaths.ScreenshotDirectory, $"{datetimestr}.png");
            using FileStream fs = new(filePath, FileMode.Create);
            encoder.Save(fs);
            NotificationMessageUtils.CreateMessage(MessageType.INFO, $"{FindResource("NotificationMessageScreenshotIsSavedTo") as string}{filePath}{FindResource("NotificationMessagePeriod") as string}");
        }

        //drag and drop a replay file on the window to open it
        private async void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                object? files = e.Data.GetData(DataFormats.FileDrop);
                if (files is not null)
                {
                    if ((files as string[])!.Length == 1)
                    {
                        await ReadPlayersListAndGetDataFromServer((files as string[])![0]);
                    }
                }
            }
        }

        private void ContextMenuCopyPlayerStatistics_Click(object sender, RoutedEventArgs e)
        {
            MenuItem? menu = sender as MenuItem;
            Player? p = menu!.DataContext as Player;
            Clipboard.SetDataObject(TextUtils.GenerateParticularPlayerStatisticsOutputText(p!));
        }

        private void ContextMenuAddToWatchListPositive_Click(object sender, RoutedEventArgs e)
        {
            MenuItem? menu = sender as MenuItem;
            Player? p = menu!.DataContext as Player;
            p!.WatchStatus = WatchStatus.POSITIVE;
            WatchListUtils.SaveWatchList(p, AppPaths.WatchListFile);
        }

        private void ContextMenuAddToWatchListNegtive_Click(object sender, RoutedEventArgs e)
        {
            MenuItem? menu = sender as MenuItem;
            Player? p = menu!.DataContext as Player;
            p!.WatchStatus = WatchStatus.NEGTIVE;
            WatchListUtils.SaveWatchList(p, AppPaths.WatchListFile);
        }

        private void ContextMenuAddToWatchListCheater_Click(object sender, RoutedEventArgs e)
        {
            MenuItem? menu = sender as MenuItem;
            Player? p = menu!.DataContext as Player;
            p!.WatchStatus = WatchStatus.CHEATER;
            WatchListUtils.SaveWatchList(p, AppPaths.WatchListFile);
        }

        private void ContextMenuRemoveFromWatchList_Click(object sender, RoutedEventArgs e)
        {
            MenuItem? menu = sender as MenuItem;
            Player? p = menu!.DataContext as Player;
            p!.WatchStatus = WatchStatus.NONE;
            WatchListUtils.SaveWatchList(p, AppPaths.WatchListFile);
        }

        //the ToolTipPlayerDetails.LayoutTransform is bind to the ViewboxPlayerList.Tag
        //so the tooltip will scale to fit the size of the datagrid when changing window size
        //a dumb way but it works
        private void ViewboxPlayerList_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (VisualTreeHelper.GetChild(ViewboxPlayerList, 0) is ContainerVisual cv)
            {
                ViewboxPlayerList.Tag = cv.Transform;
            }
        }

        private void ComboBoxChartType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Properties.Settings.Default.WinrateChartType = Convert.ToInt32(ComboBoxChartType.SelectedValue);
            Properties.Settings.Default.Save();
            SwitchWinrateChartType(Properties.Settings.Default.WinrateChartType);
        }

        private void ComboBoxMirrored_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Properties.Settings.Default.EnemiesDisplayMirrored = Convert.ToBoolean(ComboBoxMirrored.SelectedValue);
            Properties.Settings.Default.Save();
            RefreshDataGridColumns(Properties.Settings.Default.EnemiesDisplayMirrored);
        }

        private void ComboBoxSortBy_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Properties.Settings.Default.PlayerListSortBy = Convert.ToInt32(ComboBoxSortBy.SelectedValue);
            Properties.Settings.Default.Save();
            SwitchSorting(Properties.Settings.Default.PlayerListSortBy);
        }

        private void ComboBoxPlayerNamesVisibility_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Properties.Settings.Default.PlayerNamesVisibility = Convert.ToBoolean(ComboBoxPlayerNamesVisibility.SelectedValue);
            Properties.Settings.Default.Save();

            //force the datagrid to refresh. a dumb way but it works
            object tmpDataContext = this.DataContext;
            this.DataContext = null;
            this.DataContext = tmpDataContext;
        }

        private void HyperLinkApeRadarWebsite_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start("explorer.exe", e.Uri.AbsoluteUri);
        }
    }
}
