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
    public string VolumePercent => $"{Volume:P0}";
    public string PlaybackSummary => $"{VolumePercent} default volume • {(AutoScanOnStartup ? "auto-scan on" : "auto-scan off")} • {(EnableSmartDownloads ? "smart downloads on" : "smart downloads off")}";
    public string DiscoverySummary => $"Familiarity {DiscoveryBalance:P0}, popularity {PopularityBalance:P0}, variety {ArtistVariety:P0}, energy {EnergyBalance:P0}";
    public string LyricsSummary => PreferSyncedLyrics
        ? (AutoScrollLyrics ? "Prefer synced lyrics with auto-scroll." : "Prefer synced lyrics without auto-scroll.")
        : "Plain lyrics stay the default unless timestamps are stronger.";
    public string InterfaceSummary => $"{(ShowContextPanelByDefault ? "Context panel on" : "Context panel off")} • {(CompactSidebar ? "compact sidebar" : "full sidebar")}";
    public string ServicesSummary => EnableSoundCloud
        ? "SoundCloud is enabled as an external catalog and metadata source."
        : "Only local library data is active right now.";
    public string OfflineSummary => EnableSmartDownloads
        ? "Downloads are allowed to evolve with the library."
        : "Offline content changes only through direct user actions.";
    public string QualitySummary => "Playback currently uses the Windows default output path; deeper device routing is reserved as a clear extension point.";
    public string PrivacySummary => "Timed comments and favorites stay local in this build. Cloud sync and social publishing are intentionally absent.";

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
        NotifyDerivedStateChanged();
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
        NotifyDerivedStateChanged();
    }

    partial void OnAutoScanOnStartupChanged(bool value)
    {
        if (_isLoading) return;
        SaveSetting(settings => settings.AutoScanOnStartup = value);
        NotifyDerivedStateChanged();
    }

    partial void OnEnableSoundCloudChanged(bool value)
    {
        if (_isLoading) return;
        SaveSetting(settings => settings.EnableSoundCloud = value);
        NotifyDerivedStateChanged();
    }

    partial void OnPreferSyncedLyricsChanged(bool value)
    {
        if (_isLoading) return;
        SaveSetting(settings => settings.PreferSyncedLyrics = value);
        NotifyDerivedStateChanged();
    }

    partial void OnAutoScrollLyricsChanged(bool value)
    {
        if (_isLoading) return;
        SaveSetting(settings => settings.AutoScrollLyrics = value);
        NotifyDerivedStateChanged();
    }

    partial void OnSmartQueueEnabledChanged(bool value)
    {
        if (_isLoading) return;
        SaveSetting(settings => settings.SmartQueueEnabled = value);
        NotifyDerivedStateChanged();
    }

    partial void OnShowContextPanelByDefaultChanged(bool value)
    {
        if (_isLoading) return;
        SaveSetting(settings => settings.ShowContextPanelByDefault = value);
        NotifyDerivedStateChanged();
    }

    partial void OnCompactSidebarChanged(bool value)
    {
        if (_isLoading) return;
        SaveSetting(settings => settings.CompactSidebar = value);
        NotifyDerivedStateChanged();
    }

    partial void OnEnableSmartDownloadsChanged(bool value)
    {
        if (_isLoading) return;
        SaveSetting(settings => settings.EnableSmartDownloads = value);
        NotifyDerivedStateChanged();
    }

    partial void OnDiscoveryBalanceChanged(double value)
    {
        if (_isLoading) return;
        SaveSetting(settings => settings.DiscoveryBalance = value);
        NotifyDerivedStateChanged();
    }

    partial void OnPopularityBalanceChanged(double value)
    {
        if (_isLoading) return;
        SaveSetting(settings => settings.PopularityBalance = value);
        NotifyDerivedStateChanged();
    }

    partial void OnArtistVarietyChanged(double value)
    {
        if (_isLoading) return;
        SaveSetting(settings => settings.ArtistVariety = value);
        NotifyDerivedStateChanged();
    }

    partial void OnEnergyBalanceChanged(double value)
    {
        if (_isLoading) return;
        SaveSetting(settings => settings.EnergyBalance = value);
        NotifyDerivedStateChanged();
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

    private void NotifyDerivedStateChanged()
    {
        OnPropertyChanged(nameof(VolumePercent));
        OnPropertyChanged(nameof(PlaybackSummary));
        OnPropertyChanged(nameof(DiscoverySummary));
        OnPropertyChanged(nameof(LyricsSummary));
        OnPropertyChanged(nameof(InterfaceSummary));
        OnPropertyChanged(nameof(ServicesSummary));
        OnPropertyChanged(nameof(OfflineSummary));
        OnPropertyChanged(nameof(QualitySummary));
        OnPropertyChanged(nameof(PrivacySummary));
    }
}
