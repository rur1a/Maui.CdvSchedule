using Microsoft.Maui.ApplicationModel;
using Shared.Theming;
using Shared.Theming.Abstractions;
using Shared.Theming.Maui;
#if ANDROID
using Android.OS;
using Android.Views;
using AndroidX.Core.View;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Platform;
#endif

namespace Maui.CdvSchedule.App;

public partial class App : Microsoft.Maui.Controls.Application
{
    private readonly IThemeService _themeService;
    private bool _isApplyingThemeResources;
    private bool _themeApplyPending;

    public App(AppShell appShell, IThemeService themeService)
    {
        InitializeComponent();
        ThemeRuntime.EnsureThemeResources(Resources);

        _themeService = themeService;
        _themeService.Initialize();

        RequestedThemeChanged += OnRequestedThemeChanged;
        _themeService.ThemePreferenceChanged += OnThemePreferenceChanged;

        MainPage = appShell;
        RequestApplyThemeResources();
    }

    private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        RequestApplyThemeResources();
    }

    private void OnThemePreferenceChanged(object? sender, EventArgs e)
    {
        RequestApplyThemeResources();
    }

    private void RequestApplyThemeResources()
    {
        if (!MainThread.IsMainThread)
        {
            MainThread.BeginInvokeOnMainThread(RequestApplyThemeResources);
            return;
        }

        if (_isApplyingThemeResources)
        {
            _themeApplyPending = true;
            return;
        }

        do
        {
            _themeApplyPending = false;
            _isApplyingThemeResources = true;
            try
            {
                ApplyThemeResourcesCore();
            }
            finally
            {
                _isApplyingThemeResources = false;
            }
        } while (_themeApplyPending);
    }

    private void ApplyThemeResourcesCore()
    {
        if (Resources is null)
        {
            return;
        }

#if ANDROID
        ThemeRuntime.ApplyPlatformThemeColors(Resources, RequestedTheme, _themeService.CurrentPreference);
#endif

        SyncCompatibilityThemeKeys(Resources);

#if ANDROID
        ApplyAndroidSystemBarColors(Resources);
#endif
    }

    private void SyncCompatibilityThemeKeys(ResourceDictionary resources)
    {
        var isDark = IsDarkPalette();

        var background = ResolveColor(resources, ThemeColorKeys.Background, Colors.White);
        var surface = ResolveColor(resources, ThemeColorKeys.Surface, Color.FromArgb("#F2F2F2"));
        var primary = ResolveColor(resources, ThemeColorKeys.Primary, Color.FromArgb("#512BD4"));
        var onPrimary = ResolveColor(resources, ThemeColorKeys.OnPrimary, Colors.White);
        var textPrimary = ResolveColor(resources, ThemeColorKeys.TextPrimary, Color.FromArgb("#111111"));
        var textSecondary = ResolveColor(resources, ThemeColorKeys.TextSecondary, Color.FromArgb("#555555"));

        var neutral100 = ResolveColor(resources, ThemeColorKeys.Neutral100, Color.FromArgb("#E1E1E1"));
        var neutral200 = ResolveColor(resources, ThemeColorKeys.Neutral200, Color.FromArgb("#C8C8C8"));
        var neutral600 = ResolveColor(resources, ThemeColorKeys.Neutral600, Color.FromArgb("#404040"));
        var neutral900 = ResolveColor(resources, ThemeColorKeys.Neutral900, Color.FromArgb("#212121"));
        var neutral950 = ResolveColor(resources, ThemeColorKeys.Neutral950, Color.FromArgb("#141414"));

        var surfaceAlt = _themeService.CurrentPreference == AppThemePreference.Amoled
            ? neutral950
            : (isDark ? neutral900 : neutral100);
        var border = isDark ? neutral600 : neutral200;
        var tabBarBackground = _themeService.CurrentPreference == AppThemePreference.Amoled
            ? background
            : surface;

        SetColor(resources, "AppBackgroundColor", background);
        SetColor(resources, "SurfaceColor", surface);
        SetColor(resources, "SurfaceAltColor", surfaceAlt);
        SetColor(resources, "CardColor", surfaceAlt);
        SetColor(resources, "TextPrimaryColor", textPrimary);
        SetColor(resources, "TextMutedColor", textSecondary);
        SetColor(resources, "AccentColor", primary);
        SetColor(resources, "AccentContrastColor", onPrimary);
        SetColor(resources, "BorderColor", border);
        SetColor(resources, "DangerColor", Colors.Red);
        SetColor(resources, "TabBarBackgroundColor", tabBarBackground);
        SetColor(resources, "TabBarSelectedColor", primary);
        SetColor(resources, "TabBarUnselectedColor", textSecondary);
    }

    private bool IsDarkPalette()
    {
        return _themeService.CurrentPreference switch
        {
            AppThemePreference.Dark => true,
            AppThemePreference.Amoled => true,
            AppThemePreference.Light => false,
            _ => RequestedTheme == AppTheme.Dark
        };
    }

    private static Color ResolveColor(ResourceDictionary resources, string key, Color fallback)
    {
        return resources.TryGetValue(key, out var value) && value is Color color
            ? color
            : fallback;
    }

    private static void SetColor(ResourceDictionary resources, string key, Color value)
    {
        resources[key] = value;
    }

#if ANDROID
    private void ApplyAndroidSystemBarColors(ResourceDictionary resources)
    {
        var activity = Platform.CurrentActivity;
        var window = activity?.Window;
        var decorView = window?.DecorView;
        if (window is null || decorView is null)
        {
            return;
        }

        var background = ResolveColor(resources, "AppBackgroundColor", Colors.Black);
        var platformColor = background.ToPlatform();

        window.SetStatusBarColor(platformColor);
        window.SetNavigationBarColor(platformColor);

        var useDarkIcons = !IsDarkPalette();
        var controller = WindowCompat.GetInsetsController(window, decorView);
        if (controller is not null)
        {
            controller.AppearanceLightStatusBars = useDarkIcons;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                controller.AppearanceLightNavigationBars = useDarkIcons;
            }
        }
        else if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
        {
            var visibility = (StatusBarVisibility)decorView.SystemUiVisibility;
            if (useDarkIcons)
            {
                visibility |= (StatusBarVisibility)SystemUiFlags.LightStatusBar;
            }
            else
            {
                visibility &= ~(StatusBarVisibility)SystemUiFlags.LightStatusBar;
            }

            decorView.SystemUiVisibility = visibility;
        }
    }
#endif
}
