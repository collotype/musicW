using Nocturne.App.Models;

namespace Nocturne.App.Services;

public interface ISettingsService
{
    SettingsModel Current { get; }

    Task InitializeAsync();

    Task SaveAsync();

    Task ClearCacheAsync();
}
