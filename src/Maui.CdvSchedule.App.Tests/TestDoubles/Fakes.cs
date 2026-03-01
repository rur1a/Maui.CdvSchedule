using Maui.CdvSchedule.Application.Abstractions;
using Maui.CdvSchedule.Domain.Enums;
using Maui.CdvSchedule.Domain.Models;
using System.Globalization;
using AppSettings = Maui.CdvSchedule.Domain.Models.AppSettings;

namespace Maui.CdvSchedule.App.Tests.TestDoubles;

internal sealed class FakeCdvApiClient : ICdvApiClient
{
    public int LoginCalls { get; private set; }
    public int ScheduleCalls { get; private set; }
    public LoginResponseModel LoginResponse { get; set; } = new(true, "token", null, string.Empty);
    public IReadOnlyList<ScheduleLesson> ScheduleResponse { get; set; } = Array.Empty<ScheduleLesson>();
    public List<(string UserType, string UserId, string TokenEncoded, DateTime From, DateTime To)> GetScheduleArgs { get; } = new();

    public Task<LoginResponseModel> LoginAsync(string login, string password, CancellationToken cancellationToken = default)
    {
        LoginCalls++;
        return Task.FromResult(LoginResponse);
    }

    public Task<IReadOnlyList<ScheduleLesson>> GetScheduleAsync(
        string userType,
        string userId,
        string tokenEncoded,
        DateTime fromInclusive,
        DateTime toInclusive,
        CancellationToken cancellationToken = default)
    {
        ScheduleCalls++;
        GetScheduleArgs.Add((userType, userId, tokenEncoded, fromInclusive, toInclusive));
        return Task.FromResult(ScheduleResponse);
    }
}

internal sealed class FakeTokenDecoder : ITokenDecoder
{
    public UserToken Result { get; set; } = new(1, "user@example.com", "student", "Demo User", "12345");

    public UserToken Decode(string tokenEncoded) => Result;
}

internal sealed class FakeAvatarStore : IAvatarStore
{
    public int SaveCalls { get; private set; }
    public int GetCalls { get; private set; }
    public int DeleteCalls { get; private set; }
    public string? SaveResult { get; set; } = "avatar.png";
    public string? GetAvatarPathResult { get; set; }

    public Task<string?> SaveAvatarAsync(byte[] bytes, CancellationToken cancellationToken = default)
    {
        SaveCalls++;
        return Task.FromResult(SaveResult);
    }

    public Task<string?> GetAvatarPathAsync(CancellationToken cancellationToken = default)
    {
        GetCalls++;
        return Task.FromResult(GetAvatarPathResult);
    }

    public Task DeleteAvatarAsync(CancellationToken cancellationToken = default)
    {
        DeleteCalls++;
        return Task.CompletedTask;
    }
}

internal sealed class FakeSettingsStore : ISettingsStore
{
    public PersistedAppState PersistedState { get; set; } =
        new(
            AppSettings.Default,
            false,
            string.Empty,
            string.Empty,
            null);

    public int LoadCalls { get; private set; }
    public int ClearAuthCalls { get; private set; }
    public List<AppSettings> SavedSettings { get; } = new();
    public List<(string Email, string TokenEncoded, UserProfile User)> SavedAuth { get; } = new();

    public Task<PersistedAppState> LoadAsync(CancellationToken cancellationToken = default)
    {
        LoadCalls++;
        return Task.FromResult(PersistedState);
    }

    public Task SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        SavedSettings.Add(settings);
        return Task.CompletedTask;
    }

    public Task SaveAuthAsync(
        string email,
        string tokenEncoded,
        UserProfile user,
        CancellationToken cancellationToken = default)
    {
        SavedAuth.Add((email, tokenEncoded, user));
        return Task.CompletedTask;
    }

    public Task ClearAuthAsync(CancellationToken cancellationToken = default)
    {
        ClearAuthCalls++;
        return Task.CompletedTask;
    }
}

internal sealed class FakeLocalizationService : ILocalizationService
{
    public event EventHandler? LocaleChanged;

    public string CurrentLocale { get; private set; } = "pl";
    public CultureInfo CurrentCulture { get; private set; } = CultureInfo.GetCultureInfo("pl-PL");
    public int InitializeCalls { get; private set; }

    public Task InitializeAsync(string locale, CancellationToken cancellationToken = default)
    {
        InitializeCalls++;
        CurrentLocale = locale;
        CurrentCulture = CultureInfo.InvariantCulture;
        LocaleChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public string Translate(string key) => $"tr:{key}";
}

internal sealed class FakeNotificationScheduler : INotificationScheduler
{
    public int InitializeCalls { get; private set; }
    public int CancelAllCalls { get; private set; }
    public int RescheduleCalls { get; private set; }
    public IReadOnlyList<ScheduleLesson>? LastLessons { get; private set; }
    public AppSettings? LastSettings { get; private set; }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        InitializeCalls++;
        return Task.CompletedTask;
    }

    public Task CancelAllAsync(CancellationToken cancellationToken = default)
    {
        CancelAllCalls++;
        return Task.CompletedTask;
    }

    public Task RescheduleAsync(
        IReadOnlyList<ScheduleLesson> lessons,
        AppSettings settings,
        CancellationToken cancellationToken = default)
    {
        RescheduleCalls++;
        LastLessons = lessons;
        LastSettings = settings;
        return Task.CompletedTask;
    }
}

internal sealed class FakeThemeService : IAppThemeService
{
    public event EventHandler? ThemeChanged;

    public AppThemeKind CurrentTheme { get; private set; } = AppThemeKind.System;
    public int InitializeCalls { get; private set; }
    public int SetThemeCalls { get; private set; }

    public void Initialize()
    {
        InitializeCalls++;
    }

    public void SetTheme(AppThemeKind theme)
    {
        SetThemeCalls++;
        CurrentTheme = theme;
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }
}
