namespace Maui.CdvSchedule.Domain.Models;

public sealed record LoginResponseModel(
    bool Result,
    string Token,
    string? Message,
    string PhotoBase64);

