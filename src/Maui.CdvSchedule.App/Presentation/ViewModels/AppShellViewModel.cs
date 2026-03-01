using Maui.CdvSchedule.Application.Abstractions;
using Maui.CdvSchedule.Application.Services;

namespace Maui.CdvSchedule.App.Presentation.ViewModels;

public sealed class AppShellViewModel : BaseViewModel
{
    private readonly StartupService _startupService;
    private readonly ILocalizationService _localizationService;
    private bool _isInitializing;
    private bool _initialized;

    public AppShellViewModel(StartupService startupService, ILocalizationService localizationService)
    {
        _startupService = startupService;
        _localizationService = localizationService;
        _localizationService.LocaleChanged += (_, _) => RefreshTexts();
        RefreshTexts();
    }

    public string ProfileTitle => _localizationService.Translate("Main.Account");
    public string ScheduleTitle => _localizationService.Translate("Main.Schedule");
    public string SettingsTitle => _localizationService.Translate("Main.Settings");

    public bool IsInitializing
    {
        get => _isInitializing;
        private set => SetProperty(ref _isInitializing, value);
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        IsInitializing = true;
        try
        {
            await _startupService.InitializeAsync();
            _initialized = true;
            RefreshTexts();
        }
        finally
        {
            IsInitializing = false;
        }
    }

    private void RefreshTexts()
    {
        OnPropertiesChanged(nameof(ProfileTitle), nameof(ScheduleTitle), nameof(SettingsTitle));
    }
}
