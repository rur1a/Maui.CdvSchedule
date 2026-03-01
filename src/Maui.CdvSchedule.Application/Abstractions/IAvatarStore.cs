namespace Maui.CdvSchedule.Application.Abstractions;

public interface IAvatarStore
{
    Task<string?> SaveAvatarAsync(byte[] bytes, CancellationToken cancellationToken = default);

    Task<string?> GetAvatarPathAsync(CancellationToken cancellationToken = default);

    Task DeleteAvatarAsync(CancellationToken cancellationToken = default);
}

