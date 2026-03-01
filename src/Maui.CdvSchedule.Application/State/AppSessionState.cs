using Maui.CdvSchedule.Domain.Models;

namespace Maui.CdvSchedule.Application.State;

public sealed class AppSessionState : INotifyPropertyChanged
{
    private AppSettings _settings = AppSettings.Default;
    private bool _isInitialized;
    private bool _isLoggedIn;
    private string _email = string.Empty;
    private string _tokenEncoded = string.Empty;
    private UserProfile? _user;
    private string? _avatarFilePath;
    private IReadOnlyList<ScheduleLesson> _scheduleLessons = Array.Empty<ScheduleLesson>();

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? Changed;

    public AppSettings Settings => _settings;
    public bool IsInitialized => _isInitialized;
    public bool IsLoggedIn => _isLoggedIn;
    public string Email => _email;
    public string TokenEncoded => _tokenEncoded;
    public UserProfile? User => _user;
    public string? AvatarFilePath => _avatarFilePath;
    public IReadOnlyList<ScheduleLesson> ScheduleLessons => _scheduleLessons;

    public void ApplyPersistedState(PersistedAppState state, string? avatarFilePath)
    {
        _settings = state.Settings;
        _isLoggedIn = state.IsLoggedIn;
        _email = state.Email;
        _tokenEncoded = state.TokenEncoded;
        _user = state.User;
        _avatarFilePath = avatarFilePath;
        _scheduleLessons = Array.Empty<ScheduleLesson>();
        NotifyAll();
    }

    public void SetAuthenticated(
        string email,
        string tokenEncoded,
        UserProfile user,
        string? avatarFilePath)
    {
        _isLoggedIn = true;
        _email = email;
        _tokenEncoded = tokenEncoded;
        _user = user;
        _avatarFilePath = avatarFilePath;
        NotifyAll();
    }

    public void ClearAuthentication()
    {
        _isLoggedIn = false;
        _email = string.Empty;
        _tokenEncoded = string.Empty;
        _user = null;
        _avatarFilePath = null;
        _scheduleLessons = Array.Empty<ScheduleLesson>();
        NotifyAll();
    }

    public void SetSchedule(IReadOnlyList<ScheduleLesson> lessons)
    {
        _scheduleLessons = lessons;
        Notify(nameof(ScheduleLessons));
    }

    public void UpdateSettings(AppSettings settings)
    {
        _settings = settings;
        Notify(nameof(Settings));
    }

    public void MarkInitialized()
    {
        _isInitialized = true;
        Notify(nameof(IsInitialized));
    }

    private void NotifyAll()
    {
        Notify(nameof(Settings));
        Notify(nameof(IsInitialized));
        Notify(nameof(IsLoggedIn));
        Notify(nameof(Email));
        Notify(nameof(TokenEncoded));
        Notify(nameof(User));
        Notify(nameof(AvatarFilePath));
        Notify(nameof(ScheduleLessons));
    }

    private void Notify(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
