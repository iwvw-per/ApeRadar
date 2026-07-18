using Microsoft.Win32;
using SkiaSharp;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace ApeRadar.Utils
{
    internal static class ThemeManager
    {
        private const string ThemeRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private const string AppsUseLightThemeValue = "AppsUseLightTheme";
        private const string ThemeDictionaryPath = "/Resources/Themes/";
        private const int DwmwaUseImmersiveDarkMode = 20;
        private const int DwmwaUseImmersiveDarkModeBefore20H1 = 19;

        private static Application? application;
        private static bool initialized;
        private static bool? currentIsDarkTheme;

        public static event EventHandler? ThemeChanged;

        public static bool IsDarkTheme { get; private set; }

        public static void Initialize(Application app)
        {
            if (initialized)
            {
                return;
            }

            application = app;
            initialized = true;
            ApplySystemTheme();
            SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
        }

        public static void Shutdown()
        {
            if (!initialized)
            {
                return;
            }

            SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
            initialized = false;
            application = null;
        }

        public static void RegisterWindow(Window window)
        {
            window.SourceInitialized += (_, _) => ApplyWindowTheme(window);
            window.Loaded += (_, _) => ApplyWindowTheme(window);
            ApplyWindowTheme(window);
        }

        public static void ApplyWindowTheme(Window window)
        {
            IntPtr hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            SetImmersiveDarkMode(hwnd, IsDarkTheme);
        }

        public static Brush GetBrush(string resourceKey, Brush fallback)
        {
            return Application.Current?.TryFindResource(resourceKey) as Brush ?? fallback;
        }

        public static Color GetColor(string resourceKey, Color fallback)
        {
            object? resource = Application.Current?.TryFindResource(resourceKey);
            return resource switch
            {
                Color color => color,
                SolidColorBrush brush => brush.Color,
                _ => fallback
            };
        }

        public static SKColor GetSkColor(string resourceKey, SKColor fallback)
        {
            Color color = GetColor(resourceKey, Color.FromArgb(fallback.Alpha, fallback.Red, fallback.Green, fallback.Blue));
            return new SKColor(color.R, color.G, color.B, color.A);
        }

        private static void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category != UserPreferenceCategory.Color && e.Category != UserPreferenceCategory.General)
            {
                return;
            }

            Application? app = application ?? Application.Current;
            app?.Dispatcher.BeginInvoke(new Action(ApplySystemTheme));
        }

        private static void ApplySystemTheme()
        {
            Application? app = application ?? Application.Current;
            if (app is null)
            {
                return;
            }

            bool isDarkTheme = IsWindowsAppsDarkTheme();
            bool changed = currentIsDarkTheme != isDarkTheme;

            IsDarkTheme = isDarkTheme;
            currentIsDarkTheme = isDarkTheme;
            ApplyResourceDictionary(app, isDarkTheme);

            foreach (Window window in app.Windows)
            {
                ApplyWindowTheme(window);
            }

            if (changed)
            {
                ThemeChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        private static void ApplyResourceDictionary(Application app, bool isDarkTheme)
        {
            ResourceDictionary[] themeDictionaries = app.Resources.MergedDictionaries
                .Where(IsThemeDictionary)
                .ToArray();

            foreach (ResourceDictionary dictionary in themeDictionaries)
            {
                app.Resources.MergedDictionaries.Remove(dictionary);
            }

            string themeName = isDarkTheme ? "DarkTheme.xaml" : "LightTheme.xaml";
            app.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri($"{ThemeDictionaryPath}{themeName}", UriKind.Relative)
            });
            app.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri($"{ThemeDictionaryPath}ControlStyles.xaml", UriKind.Relative)
            });
        }

        private static bool IsThemeDictionary(ResourceDictionary dictionary)
        {
            string source = dictionary.Source?.OriginalString.Replace('\\', '/') ?? string.Empty;
            return source.Contains(ThemeDictionaryPath, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsWindowsAppsDarkTheme()
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(ThemeRegistryPath);
                object? value = key?.GetValue(AppsUseLightThemeValue);

                return value switch
                {
                    int intValue => intValue == 0,
                    string stringValue when int.TryParse(stringValue, out int intValue) => intValue == 0,
                    _ => false
                };
            }
            catch
            {
                return false;
            }
        }

        private static void SetImmersiveDarkMode(IntPtr hwnd, bool enabled)
        {
            try
            {
                int useDarkMode = enabled ? 1 : 0;
                int result = DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref useDarkMode, Marshal.SizeOf<int>());
                if (result != 0)
                {
                    _ = DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkModeBefore20H1, ref useDarkMode, Marshal.SizeOf<int>());
                }
            }
            catch
            {
                // Older Windows builds can ignore this; the WPF content theme still applies.
            }
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int attributeValue, int attributeSize);
    }
}
