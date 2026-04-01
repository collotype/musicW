using Nocturne.App.Helpers;
using System.Security.Cryptography;

namespace Nocturne.App.Services;

public sealed class ImageCacheService(IHttpClientFactory httpClientFactory) : IImageCacheService
{
    public async Task<string?> CacheImageAsync(string? url, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        if (File.Exists(url))
        {
            return url;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return url;
        }

        var hash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(url))).ToLowerInvariant();
        var extension = Path.GetExtension(uri.AbsolutePath);
        extension = string.IsNullOrWhiteSpace(extension) ? ".img" : extension;
        var cachedPath = Path.Combine(AppPaths.ImageCacheFolder, $"{hash}{extension}");

        if (File.Exists(cachedPath))
        {
            return cachedPath;
        }

        try
        {
            var client = httpClientFactory.CreateClient("images");
            var data = await client.GetByteArrayAsync(uri, cancellationToken);
            await File.WriteAllBytesAsync(cachedPath, data, cancellationToken);
            return cachedPath;
        }
        catch
        {
            return url;
        }
    }
}
