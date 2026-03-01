using Maui.CdvSchedule.Domain.Enums;

namespace Maui.CdvSchedule.Application.Abstractions;

public interface IAppThemeService
{
    event EventHandler? ThemeChanged;

    AppThemeKind CurrentTheme { get; }

    void Initialize();

    void SetTheme(AppThemeKind theme);
}
