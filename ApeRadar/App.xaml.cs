using System.Windows;
using RestoreWindowPlace;
using ApeRadar.Utils;

namespace ApeRadar
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public WindowPlace WindowPlace { get; }

        public App()
        {
            AppPaths.EnsureUserDataDirectories();
            this.WindowPlace = new WindowPlace(AppPaths.WindowPlacementFile);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            ThemeManager.Initialize(this);
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            ThemeManager.Shutdown();
            base.OnExit(e);
            this.WindowPlace.Save();
        }
    }
}
