using Maui.CdvSchedule.Application.Abstractions;

namespace Maui.CdvSchedule.App.Infrastructure.Storage;

public sealed class AvatarFileStore : IAvatarStore
{
    private const string AvatarFileName = "avatar.png";

    public async Task<string?> SaveAvatarAsync(byte[] bytes, CancellationToken cancellationToken = default)
    {
        if (bytes.Length == 0)
        {
            return null;
        }

        var path = GetPath();
        await File.WriteAllBytesAsync(path, bytes, cancellationToken);
        return path;
    }

    public Task<string?> GetAvatarPathAsync(CancellationToken cancellationToken = default)
    {
        var path = GetPath();
        return Task.FromResult<string?>(File.Exists(path) ? path : null);
    }

    public Task DeleteAvatarAsync(CancellationToken cancellationToken = default)
    {
        var path = GetPath();
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private static string GetPath()
    {
        return Path.Combine(FileSystem.Current.AppDataDirectory, AvatarFileName);
    }
}

