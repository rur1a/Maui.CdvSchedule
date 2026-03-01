using Maui.CdvSchedule.Application.Abstractions;
using Maui.CdvSchedule.Application.State;
using Maui.CdvSchedule.Domain.Models;

namespace Maui.CdvSchedule.Application.Services;

public sealed class AuthService
{
    private readonly ICdvApiClient _apiClient;
    private readonly ITokenDecoder _tokenDecoder;
    private readonly IAvatarStore _avatarStore;
    private readonly ISettingsStore _settingsStore;
    private readonly AppSessionState _sessionState;

    public AuthService(
        ICdvApiClient apiClient,
        ITokenDecoder tokenDecoder,
        IAvatarStore avatarStore,
        ISettingsStore settingsStore,
        AppSessionState sessionState)
    {
        _apiClient = apiClient;
        _tokenDecoder = tokenDecoder;
        _avatarStore = avatarStore;
        _settingsStore = settingsStore;
        _sessionState = sessionState;
    }

    public async Task SignInAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = (email ?? string.Empty).Trim();
        var response = await _apiClient.LoginAsync(normalizedEmail, password, cancellationToken);

        if (!response.Result || string.IsNullOrWhiteSpace(response.Token))
        {
            throw new InvalidOperationException(response.Message ?? "Login failed.");
        }

        var token = _tokenDecoder.Decode(response.Token);
        var user = new UserProfile(
            token.UserId.ToString(),
            token.UserEmail,
            token.UserType,
            token.UserName,
            token.UserAlbumNumber);

        string? avatarFilePath = null;
        if (!string.IsNullOrWhiteSpace(response.PhotoBase64))
        {
            try
            {
                var bytes = Convert.FromBase64String(response.PhotoBase64);
                avatarFilePath = await _avatarStore.SaveAvatarAsync(bytes, cancellationToken);
            }
            catch
            {
                avatarFilePath = null;
            }
        }

        await _settingsStore.SaveAuthAsync(normalizedEmail, response.Token, user, cancellationToken);
        _sessionState.SetAuthenticated(normalizedEmail, response.Token, user, avatarFilePath);
    }

    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        await _settingsStore.ClearAuthAsync(cancellationToken);
        await _avatarStore.DeleteAvatarAsync(cancellationToken);
        _sessionState.ClearAuthentication();
    }
}
