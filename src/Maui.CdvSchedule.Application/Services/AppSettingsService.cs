using Maui.CdvSchedule.Application.Abstractions;
using Maui.CdvSchedule.Application.State;
using Maui.CdvSchedule.Domain.Enums;

namespace Maui.CdvSchedule.Application.Services;

public sealed class AppSettingsService
{
    private readonly AppSessionState _sessionState;
    private readonly ISettingsStore _settingsStore;
    private readonly IAppThemeService _themeService;
    private readonly INotificationScheduler _notificationScheduler;

    public AppSettingsService(
        AppSessionState sessionState,
        ISettingsStore settingsStore,
        IAppThemeService themeService,
        INotificationScheduler notificationScheduler)
    {
        _sessionState = sessionState;
        _settingsStore = settingsStore;
        _themeService = themeService;
        _notificationScheduler = notificationScheduler;
    }

    public async Task UpdateLocaleAsync(string locale, CancellationToken cancellationToken = default)
    {
        var next = _sessionState.Settings with { Locale = locale };
        await _settingsStore.SaveSettingsAsync(next, cancellationToken);
        _sessionState.UpdateSettings(next);
    }

    public async Task UpdateThemeAsync(AppThemeKind theme, CancellationToken cancellationToken = default)
    {
        var next = _sessionState.Settings with { Theme = theme };
        _themeService.SetTheme(theme);
        await _settingsStore.SaveSettingsAsync(next, cancellationToken);
        _sessionState.UpdateSettings(next);
    }

    public async Task UpdateNotificationsEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        var next = _sessionState.Settings with { NotificationsEnabled = enabled };
        await _settingsStore.SaveSettingsAsync(next, cancellationToken);
        _sessionState.UpdateSettings(next);

        if (enabled)
        {
            await _notificationScheduler.RescheduleAsync(_sessionState.ScheduleLessons, next, cancellationToken);
        }
        else
        {
            await _notificationScheduler.CancelAllAsync(cancellationToken);
        }
    }

    public async Task UpdateNotificationOffsetAsync(int seconds, CancellationToken cancellationToken = default)
    {
        var next = _sessionState.Settings with { NotificationOffsetSeconds = seconds };
        await _settingsStore.SaveSettingsAsync(next, cancellationToken);
        _sessionState.UpdateSettings(next);

        if (next.NotificationsEnabled)
        {
            await _notificationScheduler.RescheduleAsync(_sessionState.ScheduleLessons, next, cancellationToken);
        }
    }
}
