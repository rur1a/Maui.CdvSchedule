using Maui.CdvSchedule.Application.Services;
using Maui.CdvSchedule.Application.State;
using Maui.CdvSchedule.Domain.Models;
using Maui.CdvSchedule.App.Tests.TestDoubles;

namespace Maui.CdvSchedule.App.Tests.Application.Services;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task SignInAsync_NormalizesEmail_SavesAuthAndUpdatesSession()
    {
        var api = new FakeCdvApiClient
        {
            LoginResponse = new LoginResponseModel(true, "token-123", null, string.Empty)
        };
        var tokenDecoder = new FakeTokenDecoder
        {
            Result = new UserToken(42, "user@example.com", "student", "John Doe", "A-1")
        };
        var avatarStore = new FakeAvatarStore();
        var settingsStore = new FakeSettingsStore();
        var session = new AppSessionState();
        var sut = new AuthService(api, tokenDecoder, avatarStore, settingsStore, session);

        await sut.SignInAsync("  user@example.com  ", "pass");

        Assert.Equal(1, api.LoginCalls);
        Assert.Single(settingsStore.SavedAuth);
        Assert.Equal("user@example.com", settingsStore.SavedAuth[0].Email);
        Assert.Equal("token-123", settingsStore.SavedAuth[0].TokenEncoded);
        Assert.Equal(0, avatarStore.SaveCalls);
        Assert.True(session.IsLoggedIn);
        Assert.Equal("user@example.com", session.Email);
        Assert.Equal("token-123", session.TokenEncoded);
        Assert.NotNull(session.User);
        Assert.Equal("42", session.User!.UserId);
    }

    [Fact]
    public async Task SignInAsync_WhenLoginFails_ThrowsAndDoesNotPersist()
    {
        var api = new FakeCdvApiClient
        {
            LoginResponse = new LoginResponseModel(false, string.Empty, "Bad credentials", string.Empty)
        };
        var sut = new AuthService(api, new FakeTokenDecoder(), new FakeAvatarStore(), new FakeSettingsStore(), new AppSessionState());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.SignInAsync("user@example.com", "bad-pass"));

        Assert.Equal("Bad credentials", ex.Message);
    }

    [Fact]
    public async Task SignInAsync_WithInvalidPhotoBase64_DoesNotFail()
    {
        var api = new FakeCdvApiClient
        {
            LoginResponse = new LoginResponseModel(true, "token-123", null, "not-base64")
        };
        var tokenDecoder = new FakeTokenDecoder();
        var avatarStore = new FakeAvatarStore();
        var settingsStore = new FakeSettingsStore();
        var session = new AppSessionState();
        var sut = new AuthService(api, tokenDecoder, avatarStore, settingsStore, session);

        await sut.SignInAsync("user@example.com", "pass");

        Assert.Equal(0, avatarStore.SaveCalls);
        Assert.Null(session.AvatarFilePath);
        Assert.True(session.IsLoggedIn);
        Assert.Single(settingsStore.SavedAuth);
    }

    [Fact]
    public async Task SignOutAsync_ClearsAuthAndSession()
    {
        var session = new AppSessionState();
        session.SetAuthenticated(
            "user@example.com",
            "token-123",
            new UserProfile("7", "user@example.com", "student", "User", "A-1"),
            "avatar.png");

        var settingsStore = new FakeSettingsStore();
        var avatarStore = new FakeAvatarStore();
        var sut = new AuthService(new FakeCdvApiClient(), new FakeTokenDecoder(), avatarStore, settingsStore, session);

        await sut.SignOutAsync();

        Assert.Equal(1, settingsStore.ClearAuthCalls);
        Assert.Equal(1, avatarStore.DeleteCalls);
        Assert.False(session.IsLoggedIn);
        Assert.Equal(string.Empty, session.TokenEncoded);
        Assert.Null(session.User);
        Assert.Null(session.AvatarFilePath);
    }
}
