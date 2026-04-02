using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicApp.Services;

namespace MusicApp.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IImageCacheService _imageCacheService;
    private readonly IPlaybackService _playbackService;
    private bool _isLoading;

    [ObservableProperty]
    private double _volume;

    [ObservableProperty]
    private bool _autoScanOnStartup;

    [ObservableProperty]
    private bool _enableSoundCloud;

    [ObservableProperty]
    private long _cacheSizeBytes;

    [ObservableProperty]
    private string _cacheSizeFormatted = "0 MB";

    public SettingsViewModel(
        ISettingsService settingsService,
        IImageCacheService imageCacheService,
        IPlaybackService playbackService)
    {
        _settingsService = settingsService;
        _imageCacheService = imageCacheService;
        _playbackService = playbackService;

        LoadSettings();
    }

    private void LoadSettings()
    {
        _isLoading = true;

        var settings = _settingsService.Settings;
        Volume = settings.Volume;
        AutoScanOnStartup = settings.AutoScanOnStartup;
        EnableSoundCloud = settings.EnableSoundCloud;

        _isLoading = false;
        _ = UpdateCacheSizeAsync();
    }

    private async Task UpdateCacheSizeAsync()
    {
        CacheSizeBytes = await _imageCacheService.GetCacheSizeAsync();
        CacheSizeFormatted = $"{CacheSizeBytes / (1024 * 1024)} MB";
    }

    partial void OnVolumeChanged(double value)
    {
        if (_isLoading)
        {
            return;
        }

        SaveSetting(s => s.Volume = value);
        _ = _playbackService.SetVolumeAsync(value);
    }

    partial void OnAutoScanOnStartupChanged(bool value)
    {
        if (_isLoading)
        {
            return;
        }

        SaveSetting(s => s.AutoScanOnStartup = value);
    }

    partial void OnEnableSoundCloudChanged(bool value)
    {
        if (_isLoading)
        {
            return;
        }

        SaveSetting(s => s.EnableSoundCloud = value);
    }

    private void SaveSetting(Action<Models.SettingsModel> update)
    {
        _ = _settingsService.UpdateSettingsAsync(update);
    }

    [RelayCommand]
    private async Task ClearCache()
    {
        await _imageCacheService.ClearCacheAsync();
        await UpdateCacheSizeAsync();
    }

    [RelayCommand]
    private async Task RefreshCacheSize()
    {
        await UpdateCacheSizeAsync();
    }
}
