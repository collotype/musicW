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

    [ObservableProperty] private double _volume;
    [ObservableProperty] private bool _autoScanOnStartup;
    [ObservableProperty] private bool _enableSoundCloud;
    [ObservableProperty] private bool _preferSyncedLyrics;
    [ObservableProperty] private bool _autoScrollLyrics;
    [ObservableProperty] private bool _smartQueueEnabled;
    [ObservableProperty] private bool _showContextPanelByDefault;
    [ObservableProperty] private bool _compactSidebar;
    [ObservableProperty] private bool _enableSmartDownloads;
    [ObservableProperty] private double _discoveryBalance;
    [ObservableProperty] private double _popularityBalance;
    [ObservableProperty] private double _artistVariety;
    [ObservableProperty] private double _energyBalance;
    [ObservableProperty] private long _cacheSizeBytes;
    [ObservableProperty] private string _cacheSizeFormatted = "0 MB";

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
        PreferSyncedLyrics = settings.PreferSyncedLyrics;
        AutoScrollLyrics = settings.AutoScrollLyrics;
        SmartQueueEnabled = settings.SmartQueueEnabled;
        ShowContextPanelByDefault = settings.ShowContextPanelByDefault;
        CompactSidebar = settings.CompactSidebar;
        EnableSmartDownloads = settings.EnableSmartDownloads;
        DiscoveryBalance = settings.DiscoveryBalance;
        PopularityBalance = settings.PopularityBalance;
        ArtistVariety = settings.ArtistVariety;
        EnergyBalance = settings.EnergyBalance;

        _isLoading = false;
        _ = UpdateCacheSizeAsync();
    }

    private async Task UpdateCacheSizeAsync()
    {
        CacheSizeBytes = await _imageCacheService.GetCacheSizeAsync();
        CacheSizeFormatted = $"{CacheSizeBytes / (1024 * 1024.0):F1} MB";
    }

    partial void OnVolumeChanged(double value)
    {
        if (_isLoading) return;
        SaveSetting(settings => settings.Volume = value);
        _ = _playbackService.SetVolumeAsync(value);
    }

    partial void OnAutoScanOnStartupChanged(bool value)
    {
        if (_isLoading) return;
        SaveSetting(settings => settings.AutoScanOnStartup = value);
    }

    partial void OnEnableSoundCloudChanged(bool value)
    {
        if (_isLoading) return;
        SaveSetting(settings => settings.EnableSoundCloud = value);
    }

    partial void OnPreferSyncedLyricsChanged(bool value)
    {
        if (_isLoading) return;
        SaveSetting(settings => settings.PreferSyncedLyrics = value);
    }

    partial void OnAutoScrollLyricsChanged(bool value)
    {
        if (_isLoading) return;
        SaveSetting(settings => settings.AutoScrollLyrics = value);
    }

    partial void OnSmartQueueEnabledChanged(bool value)
    {
        if (_isLoading) return;
        SaveSetting(settings => settings.SmartQueueEnabled = value);
    }

    partial void OnShowContextPanelByDefaultChanged(bool value)
    {
        if (_isLoading) return;
        SaveSetting(settings => settings.ShowContextPanelByDefault = value);
    }

    partial void OnCompactSidebarChanged(bool value)
    {
        if (_isLoading) return;
        SaveSetting(settings => settings.CompactSidebar = value);
    }

    partial void OnEnableSmartDownloadsChanged(bool value)
    {
        if (_isLoading) return;
        SaveSetting(settings => settings.EnableSmartDownloads = value);
    }

    partial void OnDiscoveryBalanceChanged(double value)
    {
        if (_isLoading) return;
        SaveSetting(settings => settings.DiscoveryBalance = value);
    }

    partial void OnPopularityBalanceChanged(double value)
    {
        if (_isLoading) return;
        SaveSetting(settings => settings.PopularityBalance = value);
    }

    partial void OnArtistVarietyChanged(double value)
    {
        if (_isLoading) return;
        SaveSetting(settings => settings.ArtistVariety = value);
    }

    partial void OnEnergyBalanceChanged(double value)
    {
        if (_isLoading) return;
        SaveSetting(settings => settings.EnergyBalance = value);
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
