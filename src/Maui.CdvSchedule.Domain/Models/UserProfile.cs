namespace Maui.CdvSchedule.Domain.Models;

public sealed record UserProfile(
    string UserId,
    string Email,
    string UserType,
    string DisplayName,
    string AlbumNumber);

