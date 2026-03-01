using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Maui.CdvSchedule.Application.Abstractions;
using Maui.CdvSchedule.Application.Services;
using Maui.CdvSchedule.Application.State;
using Maui.CdvSchedule.App.Infrastructure.Api;
using Maui.CdvSchedule.App.Infrastructure.Localization;
using Maui.CdvSchedule.App.Infrastructure.Notifications;
using Maui.CdvSchedule.App.Infrastructure.Security;
using Maui.CdvSchedule.App.Infrastructure.Storage;
using Maui.CdvSchedule.App.Infrastructure.Theming;
using Maui.CdvSchedule.App.Presentation.Pages;
using Maui.CdvSchedule.App.Presentation.Services;
using Maui.CdvSchedule.App.Presentation.ViewModels;
using Shared.Theming.Maui;

namespace Maui.CdvSchedule.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        ConfigureAppSettings(builder.Configuration);

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.Configure<CdvApiOptions>(builder.Configuration.GetSection(CdvApiOptions.SectionName));

        builder.Services.AddHttpClient<ICdvApiClient, CdvApiClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<CdvApiOptions>>().Value;
            var baseUrl = options.BaseUrl;

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new InvalidOperationException($"{CdvApiOptions.SectionName}:BaseUrl must be configured.");
            }

            if (!baseUrl.EndsWith('/'))
            {
                baseUrl += "/";
            }

            client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
            var timeoutSeconds = options.TimeoutSeconds > 0 ? options.TimeoutSeconds : 30;
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        });

        builder.Services.AddSharedTheming();
        builder.Services.AddSingleton<IAppThemeService, SharedThemeServiceAdapter>();

        builder.Services.AddSingleton<AppSessionState>();

        builder.Services.AddSingleton<ISettingsStore, PreferencesSettingsStore>();
        builder.Services.AddSingleton<ILocalizationService, CsvLocalizationService>();
        builder.Services.AddSingleton<IAvatarStore, AvatarFileStore>();
        builder.Services.AddSingleton<ITokenDecoder, JwtTokenDecoder>();
        builder.Services.AddSingleton<INotificationScheduler, AndroidNotificationScheduler>();

        builder.Services.AddSingleton<LessonMetadataService>();
        builder.Services.AddSingleton<ScheduleService>();
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<AppSettingsService>();
        builder.Services.AddSingleton<StartupService>();

        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddSingleton<AppShellViewModel>();
        builder.Services.AddSingleton<ProfileViewModel>();
        builder.Services.AddSingleton<ScheduleViewModel>();
        builder.Services.AddSingleton<SettingsViewModel>();

        builder.Services.AddSingleton<ProfilePage>();
        builder.Services.AddSingleton<SchedulePage>();
        builder.Services.AddSingleton<SettingsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private static void ConfigureAppSettings(IConfigurationBuilder configuration)
    {
        AddConfigurationAsset(configuration, "appsettings.json");

        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
#if DEBUG
        environment ??= "Development";
#endif

        if (!string.IsNullOrWhiteSpace(environment))
        {
            AddConfigurationAsset(configuration, $"appsettings.{environment}.json");
        }
    }

    private static void AddConfigurationAsset(IConfigurationBuilder configuration, string assetName)
    {
        try
        {
            using var stream = FileSystem.Current.OpenAppPackageFileAsync(assetName).GetAwaiter().GetResult();
            configuration.AddJsonStream(stream);
        }
        catch (FileNotFoundException)
        {
            // Optional appsettings file.
        }
    }
}
