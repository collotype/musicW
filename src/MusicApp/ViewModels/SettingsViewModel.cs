using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicApp.Services;

namespace MusicApp.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IImageCacheService _imageCacheService;

    [ObservableProperty]
    private bool _useTransparency;

    [ObservableProperty]
    private bool _showAlbumArtInPlayer;

    [ObservableProperty]
    private bool _minimizeToTray;

    [ObservableProperty]
    private double _volume;

    [ObservableProperty]
    private bool _enableGapless;

    [ObservableProperty]
    private int _cacheSizeLimitMB;

    [ObservableProperty]
    private string? _downloadLocation;

    [ObservableProperty]
    private bool _autoScanOnStartup;

    [ObservableProperty]
    private bool _enableSoundCloud;

    [ObservableProperty]
    private bool _enableSpotify;

    [ObservableProperty]
    private long _cacheSizeBytes;

    [ObservableProperty]
    private string _cacheSizeFormatted = "0 MB";

    public SettingsViewModel(
        ISettingsService settingsService,
        IImageCacheService imageCacheService)
    {
        _settingsService = settingsService;
        _imageCacheService = imageCacheService;

        LoadSettings();
    }

    private void LoadSettings()
    {
        var settings = _settingsService.Settings;

        UseTransparency = settings.UseTransparency;
        ShowAlbumArtInPlayer = settings.ShowAlbumArtInPlayer;
        MinimizeToTray = settings.MinimizeToTray;
        Volume = settings.Volume;
        EnableGapless = settings.EnableGapless;
        CacheSizeLimitMB = settings.CacheSizeLimitMB;
        DownloadLocation = settings.DownloadLocation;
        AutoScanOnStartup = settings.AutoScanOnStartup;
        EnableSoundCloud = settings.EnableSoundCloud;
        EnableSpotify = settings.EnableSpotify;

        _ = UpdateCacheSizeAsync();
    }

    private async Task UpdateCacheSizeAsync()
    {
        CacheSizeBytes = await _imageCacheService.GetCacheSizeAsync();
        CacheSizeFormatted = $"{CacheSizeBytes / (1024 * 1024)} MB";
    }

    partial void OnUseTransparencyChanged(bool value) => SaveSetting(s => s.UseTransparency = value);
    partial void OnShowAlbumArtInPlayerChanged(bool value) => SaveSetting(s => s.ShowAlbumArtInPlayer = value);
    partial void OnMinimizeToTrayChanged(bool value) => SaveSetting(s => s.MinimizeToTray = value);
    partial void OnVolumeChanged(double value) => SaveSetting(s => s.Volume = value);
    partial void OnEnableGaplessChanged(bool value) => SaveSetting(s => s.EnableGapless = value);
    partial void OnCacheSizeLimitMBChanged(int value) => SaveSetting(s => s.CacheSizeLimitMB = value);
    partial void OnDownloadLocationChanged(string? value) => SaveSetting(s => s.DownloadLocation = value);
    partial void OnAutoScanOnStartupChanged(bool value) => SaveSetting(s => s.AutoScanOnStartup = value);
    partial void OnEnableSoundCloudChanged(bool value) => SaveSetting(s => s.EnableSoundCloud = value);
    partial void OnEnableSpotifyChanged(bool value) => SaveSetting(s => s.EnableSpotify = value);

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
    private async Task BrowseDownloadLocation()
    {
        // Would use folder picker dialog in real implementation
        DownloadLocation = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
    }

    [RelayCommand]
    private async Task RefreshCacheSize()
    {
        await UpdateCacheSizeAsync();
    }
}
