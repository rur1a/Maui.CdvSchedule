using Maui.CdvSchedule.Application.Abstractions;
using Maui.CdvSchedule.Application.Services;
using Maui.CdvSchedule.Application.State;
using Maui.CdvSchedule.Domain.Enums;
using Maui.CdvSchedule.App.Presentation.Models;

namespace Maui.CdvSchedule.App.Presentation.ViewModels;

public sealed class SettingsViewModel : BaseViewModel
{
    private readonly AppSessionState _sessionState;
    private readonly AppSettingsService _appSettingsService;
    private readonly ILocalizationService _localizationService;

    private OptionItem<string>? _selectedLocale;
    private OptionItem<AppThemeKind>? _selectedTheme;
    private OptionItem<int>? _selectedNotificationOffset;
    private bool _notificationsEnabled;

    public SettingsViewModel(
        AppSessionState sessionState,
        AppSettingsService appSettingsService,
        ILocalizationService localizationService)
    {
        _sessionState = sessionState;
        _appSettingsService = appSettingsService;
        _localizationService = localizationService;

        _sessionState.Changed += (_, _) => RefreshFromState();
        _localizationService.LocaleChanged += (_, _) =>
        {
            RebuildOptions();
            RefreshLocalizedText();
        };

        RebuildOptions();
        RefreshFromState();
    }

    public ObservableCollection<OptionItem<string>> LocaleOptions { get; } = new();
    public ObservableCollection<OptionItem<AppThemeKind>> ThemeOptions { get; } = new();
    public ObservableCollection<OptionItem<int>> NotificationOffsetOptions { get; } = new();

    public bool IsLoaded { get; private set; }

    public string PageTitle => _localizationService.Translate("Main.Settings");
    public string LocaleLabel => _localizationService.Translate("Settings.c.lang");
    public string ThemeLabel => _localizationService.Translate("Settings.theme");
    public string NotificationsLabel => _localizationService.Translate("Settings.notification");
    public string NotificationTimeLabel => _localizationService.Translate("Settings.notificationTime");
    public string RestartRequiredTitle => _localizationService.Translate("Settings.AlertDialog");
    public string RestartRequiredQuestion => _localizationService.Translate("Settings.AlertDialog.q");
    public string RestartButtonText => _localizationService.Translate("Setting.AlertDialog.Restart");

    public OptionItem<string>? SelectedLocale
    {
        get => _selectedLocale;
        set => SetProperty(ref _selectedLocale, value);
    }

    public OptionItem<AppThemeKind>? SelectedTheme
    {
        get => _selectedTheme;
        set => SetProperty(ref _selectedTheme, value);
    }

    public OptionItem<int>? SelectedNotificationOffset
    {
        get => _selectedNotificationOffset;
        set => SetProperty(ref _selectedNotificationOffset, value);
    }

    public bool NotificationsEnabled
    {
        get => _notificationsEnabled;
        set => SetProperty(ref _notificationsEnabled, value);
    }

    public void MarkLoaded() => IsLoaded = true;

    public void RefreshFromState()
    {
        NotificationsEnabled = _sessionState.Settings.NotificationsEnabled;
        SelectedLocale = LocaleOptions.FirstOrDefault(x => x.Value == _sessionState.Settings.Locale);
        SelectedTheme = ThemeOptions.FirstOrDefault(x => x.Value == _sessionState.Settings.Theme);
        SelectedNotificationOffset = NotificationOffsetOptions.FirstOrDefault(x => x.Value == _sessionState.Settings.NotificationOffsetSeconds);
    }

    public async Task<bool> ApplyLocaleAsync()
    {
        if (SelectedLocale is null || SelectedLocale.Value == _sessionState.Settings.Locale)
        {
            return false;
        }

        await _appSettingsService.UpdateLocaleAsync(SelectedLocale.Value);
        return true;
    }

    public async Task<bool> ApplyThemeAsync()
    {
        if (SelectedTheme is null || SelectedTheme.Value == _sessionState.Settings.Theme)
        {
            return false;
        }

        await _appSettingsService.UpdateThemeAsync(SelectedTheme.Value);
        return true;
    }

    public async Task ApplyNotificationsEnabledAsync(bool enabled)
    {
        if (enabled == _sessionState.Settings.NotificationsEnabled)
        {
            return;
        }

        await _appSettingsService.UpdateNotificationsEnabledAsync(enabled);
    }

    public async Task ApplyNotificationOffsetAsync()
    {
        if (SelectedNotificationOffset is null || SelectedNotificationOffset.Value == _sessionState.Settings.NotificationOffsetSeconds)
        {
            return;
        }

        await _appSettingsService.UpdateNotificationOffsetAsync(SelectedNotificationOffset.Value);
    }

    private void RebuildOptions()
    {
        var selectedLocaleValue = _selectedLocale?.Value ?? _sessionState.Settings.Locale;
        var selectedThemeValue = _selectedTheme?.Value ?? _sessionState.Settings.Theme;
        var selectedOffsetValue = _selectedNotificationOffset?.Value ?? _sessionState.Settings.NotificationOffsetSeconds;

        LocaleOptions.Clear();
        LocaleOptions.Add(new OptionItem<string> { Label = _localizationService.Translate("Settings.Locale.English"), Value = "en" });
        LocaleOptions.Add(new OptionItem<string> { Label = _localizationService.Translate("Settings.Locale.Polish"), Value = "pl" });
        LocaleOptions.Add(new OptionItem<string> { Label = _localizationService.Translate("Settings.Locale.Russian"), Value = "ru" });
        LocaleOptions.Add(new OptionItem<string> { Label = _localizationService.Translate("Settings.Locale.Turkish"), Value = "tr" });

        ThemeOptions.Clear();
        ThemeOptions.Add(new OptionItem<AppThemeKind> { Label = _localizationService.Translate("Settings.Theme.System"), Value = AppThemeKind.System });
        ThemeOptions.Add(new OptionItem<AppThemeKind> { Label = _localizationService.Translate("Settings.Theme.Light"), Value = AppThemeKind.Light });
        ThemeOptions.Add(new OptionItem<AppThemeKind> { Label = _localizationService.Translate("Settings.Theme.Dark"), Value = AppThemeKind.Dark });
        ThemeOptions.Add(new OptionItem<AppThemeKind> { Label = _localizationService.Translate("Settings.Theme.Amoled"), Value = AppThemeKind.Amoled });

        NotificationOffsetOptions.Clear();
        NotificationOffsetOptions.Add(new OptionItem<int> { Label = _localizationService.Translate("Settings.time.15mins"), Value = 900 });
        NotificationOffsetOptions.Add(new OptionItem<int> { Label = _localizationService.Translate("Settings.time.30mins"), Value = 1800 });
        NotificationOffsetOptions.Add(new OptionItem<int> { Label = _localizationService.Translate("Settings.time.1hour"), Value = 3600 });
        NotificationOffsetOptions.Add(new OptionItem<int> { Label = _localizationService.Translate("Settings.time.2hours"), Value = 7200 });

        SelectedLocale = LocaleOptions.FirstOrDefault(x => x.Value == selectedLocaleValue);
        SelectedTheme = ThemeOptions.FirstOrDefault(x => x.Value == selectedThemeValue);
        SelectedNotificationOffset = NotificationOffsetOptions.FirstOrDefault(x => x.Value == selectedOffsetValue);
    }

    private void RefreshLocalizedText()
    {
        OnPropertiesChanged(
            nameof(PageTitle),
            nameof(LocaleLabel),
            nameof(ThemeLabel),
            nameof(NotificationsLabel),
            nameof(NotificationTimeLabel),
            nameof(RestartRequiredTitle),
            nameof(RestartRequiredQuestion),
            nameof(RestartButtonText));
    }
}
