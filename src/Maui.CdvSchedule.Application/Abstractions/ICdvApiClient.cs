using Maui.CdvSchedule.Domain.Models;

namespace Maui.CdvSchedule.Application.Abstractions;

public interface ICdvApiClient
{
    Task<LoginResponseModel> LoginAsync(string login, string password, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScheduleLesson>> GetScheduleAsync(
        string userType,
        string userId,
        string tokenEncoded,
        DateTime fromInclusive,
        DateTime toInclusive,
        CancellationToken cancellationToken = default);
}

