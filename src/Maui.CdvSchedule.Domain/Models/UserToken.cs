namespace Maui.CdvSchedule.Domain.Models;

public sealed record UserToken(
    int UserId,
    string UserEmail,
    string UserType,
    string UserName,
    string UserAlbumNumber);

