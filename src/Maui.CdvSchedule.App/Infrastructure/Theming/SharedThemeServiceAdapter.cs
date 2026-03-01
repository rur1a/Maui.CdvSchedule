using Maui.CdvSchedule.Application.Abstractions;
using Maui.CdvSchedule.Domain.Enums;
using Shared.Theming;
using Shared.Theming.Abstractions;

namespace Maui.CdvSchedule.App.Infrastructure.Theming;

public sealed class SharedThemeServiceAdapter : IAppThemeService
{
    private readonly IThemeService _themeService;

    public SharedThemeServiceAdapter(IThemeService themeService)
    {
        _themeService = themeService;
        _themeService.ThemePreferenceChanged += OnThemePreferenceChanged;
    }

    public event EventHandler? ThemeChanged;

    public AppThemeKind CurrentTheme => _themeService.CurrentPreference switch
    {
        AppThemePreference.Light => AppThemeKind.Light,
        AppThemePreference.Dark => AppThemeKind.Dark,
        AppThemePreference.Amoled => AppThemeKind.Amoled,
        _ => AppThemeKind.System,
    };

    public void Initialize()
    {
        _themeService.Initialize();
    }

    public void SetTheme(AppThemeKind theme)
    {
        var preference = theme switch
        {
            AppThemeKind.Light => AppThemePreference.Light,
            AppThemeKind.Dark => AppThemePreference.Dark,
            AppThemeKind.Amoled => AppThemePreference.Amoled,
            _ => AppThemePreference.System,
        };

        _themeService.SetPreference(preference);
    }

    private void OnThemePreferenceChanged(object? sender, EventArgs e)
    {
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }
}
