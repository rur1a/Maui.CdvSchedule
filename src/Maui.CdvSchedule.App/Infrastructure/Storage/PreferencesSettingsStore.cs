using Maui.CdvSchedule.Application.Abstractions;
using Maui.CdvSchedule.Domain.Enums;
using Maui.CdvSchedule.Domain.Models;

namespace Maui.CdvSchedule.App.Infrastructure.Storage;

public sealed class PreferencesSettingsStore : ISettingsStore
{
    private const string KeyThemeId = "themeId";
    private const string KeyLocalization = "localization";
    private const string KeyNotificationsToggle = "notificationsToggle";
    private const string KeyNotificationsTime = "notificationsTime";
    private const string KeyIsUserLoggedIn = "isUserLoggedIn";
    private const string KeySavedEmail = "savedEmail";
    private const string LegacyKeySavedPassword = "savedPassword";
    private const string KeySavedTokenEncoded = "savedTokenEncoded";
    private const string KeySavedUserName = "savedUserName";
    private const string KeySavedUserType = "savedUserType";
    private const string KeySavedUserAlbumNumber = "savedUserAlbumNumber";
    private const string KeySavedUserId = "savedUserId";

    public async Task<PersistedAppState> LoadAsync(CancellationToken cancellationToken = default)
    {
        var settings = new AppSettings(
            Locale: Preferences.Default.Get(KeyLocalization, AppSettings.Default.Locale),
            Theme: AppThemeKindExtensions.FromId(Preferences.Default.Get(KeyThemeId, (int)AppSettings.Default.Theme)),
            NotificationsEnabled: Preferences.Default.Get(KeyNotificationsToggle, AppSettings.Default.NotificationsEnabled),
            NotificationOffsetSeconds: Preferences.Default.Get(KeyNotificationsTime, AppSettings.Default.NotificationOffsetSeconds));

        var isMarkedLoggedIn = Preferences.Default.Get(KeyIsUserLoggedIn, false);
        var tokenEncoded = await GetTokenEncodedAsync(cancellationToken);
        Preferences.Default.Remove(LegacyKeySavedPassword);

        UserProfile? user = null;
        if (isMarkedLoggedIn)
        {
            var userId = Preferences.Default.Get(KeySavedUserId, string.Empty);
            var email = Preferences.Default.Get(KeySavedEmail, string.Empty);
            var userType = Preferences.Default.Get(KeySavedUserType, string.Empty);
            var name = Preferences.Default.Get(KeySavedUserName, string.Empty);
            var album = Preferences.Default.Get(KeySavedUserAlbumNumber, string.Empty);

            if (!string.IsNullOrWhiteSpace(userId))
            {
                user = new UserProfile(userId, email, userType, name, album);
            }
        }

        return new PersistedAppState(
            Settings: settings,
            IsLoggedIn: isMarkedLoggedIn && user is not null && !string.IsNullOrWhiteSpace(tokenEncoded),
            Email: Preferences.Default.Get(KeySavedEmail, string.Empty),
            TokenEncoded: tokenEncoded,
            User: user);
    }

    public Task SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        Preferences.Default.Set(KeyThemeId, (int)settings.Theme);
        Preferences.Default.Set(KeyLocalization, settings.Locale);
        Preferences.Default.Set(KeyNotificationsToggle, settings.NotificationsEnabled);
        Preferences.Default.Set(KeyNotificationsTime, settings.NotificationOffsetSeconds);
        return Task.CompletedTask;
    }

    public async Task SaveAuthAsync(
        string email,
        string tokenEncoded,
        UserProfile user,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Preferences.Default.Set(KeyIsUserLoggedIn, true);
        Preferences.Default.Set(KeySavedEmail, email);
        Preferences.Default.Remove(LegacyKeySavedPassword);
        Preferences.Default.Set(KeySavedUserName, user.DisplayName);
        Preferences.Default.Set(KeySavedUserType, user.UserType);
        Preferences.Default.Set(KeySavedUserAlbumNumber, user.AlbumNumber);
        Preferences.Default.Set(KeySavedUserId, user.UserId);

        await SecureStorage.Default.SetAsync(KeySavedTokenEncoded, tokenEncoded);
    }

    public Task ClearAuthAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Preferences.Default.Set(KeyIsUserLoggedIn, false);

        Preferences.Default.Remove(KeySavedEmail);
        Preferences.Default.Remove(LegacyKeySavedPassword);
        Preferences.Default.Remove(KeySavedUserName);
        Preferences.Default.Remove(KeySavedUserType);
        Preferences.Default.Remove(KeySavedUserAlbumNumber);
        Preferences.Default.Remove(KeySavedUserId);

        try
        {
            SecureStorage.Default.Remove(KeySavedTokenEncoded);
        }
        catch
        {
            // Secure storage can fail on compromised/unsupported device state.
        }

        return Task.CompletedTask;
    }

    private static async Task<string> GetTokenEncodedAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            return await SecureStorage.Default.GetAsync(KeySavedTokenEncoded) ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
