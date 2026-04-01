namespace Nocturne.App.Services;

public interface IImageCacheService
{
    Task<string?> CacheImageAsync(string? url, CancellationToken cancellationToken);
}
