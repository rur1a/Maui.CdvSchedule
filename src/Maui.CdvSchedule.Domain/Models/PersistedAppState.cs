namespace Maui.CdvSchedule.Domain.Models;

public sealed record PersistedAppState(
    AppSettings Settings,
    bool IsLoggedIn,
    string Email,
    string TokenEncoded,
    UserProfile? User);
