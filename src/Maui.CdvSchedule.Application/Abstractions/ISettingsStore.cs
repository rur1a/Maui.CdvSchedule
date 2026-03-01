using Maui.CdvSchedule.Domain.Models;

namespace Maui.CdvSchedule.Application.Abstractions;

public interface ISettingsStore
{
    Task<PersistedAppState> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default);

    Task SaveAuthAsync(
        string email,
        string tokenEncoded,
        UserProfile user,
        CancellationToken cancellationToken = default);

    Task ClearAuthAsync(CancellationToken cancellationToken = default);
}
