using System.Globalization;
using Maui.CdvSchedule.Application.Abstractions;
using Maui.CdvSchedule.Application.Services;
using Maui.CdvSchedule.Application.State;

namespace Maui.CdvSchedule.App.Presentation.ViewModels;

public sealed class ProfileViewModel : BaseViewModel
{
    private readonly AppSessionState _sessionState;
    private readonly AuthService _authService;
    private readonly ScheduleService _scheduleService;
    private readonly ILocalizationService _localizationService;

    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isBusy;
    private ImageSource? _avatarSource = ImageSource.FromFile("logo_text.png");

    public ProfileViewModel(
        AppSessionState sessionState,
        AuthService authService,
        ScheduleService scheduleService,
        ILocalizationService localizationService)
    {
        _sessionState = sessionState;
        _authService = authService;
        _scheduleService = scheduleService;
        _localizationService = localizationService;

        _sessionState.Changed += (_, _) => RefreshFromState();
        _localizationService.LocaleChanged += (_, _) => RefreshLocalizedText();

        RefreshFromState();
    }

    public string PageTitle => _localizationService.Translate("Main.Account");
    public string EmailLabel => _localizationService.Translate("Login.Page.Email");
    public string PasswordLabel => _localizationService.Translate("Login.Page.Password");
    public string EmailPlaceholder => _localizationService.Translate("Login.Page.Hint.login");
    public string PasswordPlaceholder => _localizationService.Translate("Login.Page.Hint.password");
    public string SignInButtonText => _localizationService.Translate("Login.Page.btn");
    public string SignOutButtonText => _localizationService.Translate("Profile.signOut");
    public string LoadingText => _localizationService.Translate("Profile.Loading");
    public string SignOutConfirmTitle => _localizationService.Translate("Profile.Confirmation");
    public string SignOutConfirmQuestion => _localizationService.Translate("Login.ask");
    public string CancelText => _localizationService.Translate("Profile.Cancel");
    public string AlbumNumberLabelPrefix => _localizationService.Translate("Profile.AlbumNumberLabel");

    public bool IsLoggedIn => _sessionState.IsLoggedIn;
    public string DisplayName => _sessionState.User?.DisplayName ?? string.Empty;
    public string UserTypeDisplay =>
        string.IsNullOrWhiteSpace(_sessionState.User?.UserType)
            ? string.Empty
            : CultureInfo.CurrentCulture.TextInfo.ToTitleCase((_sessionState.User?.UserType ?? string.Empty).ToLowerInvariant());
    public string AlbumNumber => _sessionState.User?.AlbumNumber ?? string.Empty;

    public ImageSource? AvatarSource
    {
        get => _avatarSource;
        private set => SetProperty(ref _avatarSource, value);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public void RefreshFromState()
    {
        Email = _sessionState.Email;
        if (!_sessionState.IsLoggedIn)
        {
            Password = string.Empty;
        }

        AvatarSource = !string.IsNullOrWhiteSpace(_sessionState.AvatarFilePath)
            ? ImageSource.FromFile(_sessionState.AvatarFilePath)
            : ImageSource.FromFile("logo_text.png");

        OnPropertiesChanged(
            nameof(IsLoggedIn),
            nameof(DisplayName),
            nameof(UserTypeDisplay),
            nameof(AlbumNumber));
    }

    public async Task<bool> SignInAsync()
    {
        if (IsBusy)
        {
            return false;
        }

        ErrorMessage = string.Empty;
        IsBusy = true;
        try
        {
            await _authService.SignInAsync(Email, Password);
            try
            {
                await _scheduleService.RefreshAsync();
            }
            catch
            {
                // Keep authenticated session when schedule refresh fails transiently.
            }

            Password = string.Empty;
            return true;
        }
        catch
        {
            ErrorMessage = _localizationService.Translate("Login.Page.error");
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task SignOutAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await _authService.SignOutAsync();
            Password = string.Empty;
            ErrorMessage = string.Empty;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RefreshLocalizedText()
    {
        if (!string.IsNullOrWhiteSpace(ErrorMessage))
        {
            ErrorMessage = _localizationService.Translate("Login.Page.error");
        }

        OnPropertiesChanged(
            nameof(PageTitle),
            nameof(EmailLabel),
            nameof(PasswordLabel),
            nameof(EmailPlaceholder),
            nameof(PasswordPlaceholder),
            nameof(SignInButtonText),
            nameof(SignOutButtonText),
            nameof(LoadingText),
            nameof(SignOutConfirmTitle),
            nameof(SignOutConfirmQuestion),
            nameof(CancelText),
            nameof(AlbumNumberLabelPrefix),
            nameof(UserTypeDisplay));
    }
}
