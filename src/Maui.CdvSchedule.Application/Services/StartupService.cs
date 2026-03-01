using Maui.CdvSchedule.Application.Abstractions;
using Maui.CdvSchedule.Application.State;
using Maui.CdvSchedule.Domain.Exceptions;

namespace Maui.CdvSchedule.Application.Services;

public sealed class StartupService
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ISettingsStore _settingsStore;
    private readonly ILocalizationService _localizationService;
    private readonly IAppThemeService _themeService;
    private readonly INotificationScheduler _notificationScheduler;
    private readonly IAvatarStore _avatarStore;
    private readonly AppSessionState _sessionState;
    private readonly ScheduleService _scheduleService;
    private readonly AuthService _authService;
    private bool _initialized;

    public StartupService(
        ISettingsStore settingsStore,
        ILocalizationService localizationService,
        IAppThemeService themeService,
        INotificationScheduler notificationScheduler,
        IAvatarStore avatarStore,
        AppSessionState sessionState,
        ScheduleService scheduleService,
        AuthService authService)
    {
        _settingsStore = settingsStore;
        _localizationService = localizationService;
        _themeService = themeService;
        _notificationScheduler = notificationScheduler;
        _avatarStore = avatarStore;
        _sessionState = sessionState;
        _scheduleService = scheduleService;
        _authService = authService;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            return;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_initialized)
            {
                return;
            }

            var persisted = await _settingsStore.LoadAsync(cancellationToken);
            var avatarPath = await _avatarStore.GetAvatarPathAsync(cancellationToken);

            _sessionState.ApplyPersistedState(persisted, avatarPath);
            await _notificationScheduler.InitializeAsync(cancellationToken);
            await _localizationService.InitializeAsync(_sessionState.Settings.Locale, cancellationToken);
            _themeService.SetTheme(_sessionState.Settings.Theme);

            if (_sessionState.IsLoggedIn)
            {
                try
                {
                    await _scheduleService.RefreshAsync(cancellationToken);
                }
                catch (CdvApiException ex) when (ex.IsUnauthorized)
                {
                    await _authService.SignOutAsync(cancellationToken);
                }
                catch
                {
                    // Keep the authenticated session for transient startup/network errors.
                }
            }

            _initialized = true;
            _sessionState.MarkInitialized();
        }
        finally
        {
            _gate.Release();
        }
    }
}
