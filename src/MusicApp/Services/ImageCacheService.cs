using System.IO;
using System.Security.Cryptography;
using System.Net.Http;

namespace MusicApp.Services;

public class ImageCacheService : IImageCacheService
{
    private readonly string _cacheDirectory;
    private readonly HttpClient _httpClient = new();
    private const int CacheDurationHours = 24;

    public ImageCacheService()
    {
        _cacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MusicApp", "ImageCache");
        Directory.CreateDirectory(_cacheDirectory);
    }

    public async Task<string?> GetCachedImagePathAsync(string url)
    {
        if (string.IsNullOrEmpty(url)) return null;

        var cacheKey = GetCacheKey(url);
        var cachePath = Path.Combine(_cacheDirectory, cacheKey);

        if (File.Exists(cachePath))
        {
            var fileInfo = new FileInfo(cachePath);
            if (fileInfo.LastWriteTimeUtc > DateTime.UtcNow - TimeSpan.FromHours(CacheDurationHours))
            {
                return cachePath;
            }
        }

        try
        {
            var data = await _httpClient.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(cachePath, data);
            return cachePath;
        }
        catch
        {
            return File.Exists(cachePath) ? cachePath : null;
        }
    }

    public async Task CacheImageAsync(string url)
    {
        if (string.IsNullOrEmpty(url)) return;
        await GetCachedImagePathAsync(url);
    }

    public Task ClearCacheAsync()
    {
        try
        {
            if (Directory.Exists(_cacheDirectory))
            {
                foreach (var file in Directory.GetFiles(_cacheDirectory))
                {
                    File.Delete(file);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to clear cache: {ex.Message}");
        }
        return Task.CompletedTask;
    }

    public async Task<long> GetCacheSizeAsync()
    {
        try
        {
            if (!Directory.Exists(_cacheDirectory)) return 0;

            long totalSize = 0;
            foreach (var file in Directory.GetFiles(_cacheDirectory))
            {
                var fileInfo = new FileInfo(file);
                totalSize += fileInfo.Length;
            }
            return totalSize;
        }
        catch
        {
            return 0;
        }
    }

    private static string GetCacheKey(string url)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(url));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant() + ".jpg";
    }
}
