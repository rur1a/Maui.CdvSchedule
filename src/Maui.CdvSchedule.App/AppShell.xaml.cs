using Microsoft.Maui.ApplicationModel;
using Maui.CdvSchedule.Application.Abstractions;
using Maui.CdvSchedule.App.Presentation.Pages;
using Maui.CdvSchedule.App.Presentation.ViewModels;

namespace Maui.CdvSchedule.App;

public partial class AppShell : Shell
{
    private readonly ILocalizationService _localizationService;
    private readonly ShellContent _profileTab;
    private readonly ShellContent _scheduleTab;
    private readonly ShellContent _settingsTab;
    private bool _initialized;
    private AppShellViewModel ViewModel => (AppShellViewModel)BindingContext;

    public AppShell(
        AppShellViewModel viewModel,
        ILocalizationService localizationService,
        ProfilePage profilePage,
        SchedulePage schedulePage,
        SettingsPage settingsPage)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _localizationService = localizationService;
        _localizationService.LocaleChanged += OnLocaleChanged;

        _profileTab = new ShellContent
        {
            Icon = "profile_tab.svg",
            Route = "profile",
            Content = profilePage,
        };
        _scheduleTab = new ShellContent
        {
            Icon = "schedule_tab.svg",
            Route = "schedule",
            Content = schedulePage,
        };
        _settingsTab = new ShellContent
        {
            Icon = "settings_tab.svg",
            Route = "settings",
            Content = settingsPage,
        };

        var tabBar = new TabBar();
        tabBar.Items.Add(_profileTab);
        tabBar.Items.Add(_scheduleTab);
        tabBar.Items.Add(_settingsTab);
        Items.Add(tabBar);

        ApplyTabLocalization();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_initialized)
        {
            return;
        }

        _initialized = true;
        await ViewModel.InitializeAsync();
        ApplyTabLocalization();
    }

    private void OnLocaleChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(ApplyTabLocalization);
    }

    private void ApplyTabLocalization()
    {
        _profileTab.Title = ViewModel.ProfileTitle;
        _scheduleTab.Title = ViewModel.ScheduleTitle;
        _settingsTab.Title = ViewModel.SettingsTitle;
    }
}
