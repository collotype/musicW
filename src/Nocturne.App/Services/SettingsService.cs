using Nocturne.App.Helpers;
using Nocturne.App.Models;
using Nocturne.App.Persistence;

namespace Nocturne.App.Services;

public sealed class SettingsService : ISettingsService
{
    public SettingsModel Current { get; private set; } = new();

    public async Task InitializeAsync()
    {
        Current = await JsonFileStore.ReadAsync<SettingsModel>(AppPaths.SettingsFile) ?? new SettingsModel();
        await SaveAsync();
    }

    public Task SaveAsync()
    {
        return JsonFileStore.WriteAsync(AppPaths.SettingsFile, Current);
    }

    public Task ClearCacheAsync()
    {
        if (Directory.Exists(AppPaths.CacheFolder))
        {
            Directory.Delete(AppPaths.CacheFolder, true);
        }

        AppPaths.EnsureCreated();
        return Task.CompletedTask;
    }
}
