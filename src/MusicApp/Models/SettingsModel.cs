namespace MusicApp.Models;

public class SettingsModel
{
    // Appearance
    public string Theme { get; set; } = "Dark";
    public bool UseTransparency { get; set; } = true;
    public bool ShowAlbumArtInPlayer { get; set; } = true;
    public bool MinimizeToTray { get; set; } = false;

    // Audio
    public double Volume { get; set; } = 0.8;
    public bool IsMuted { get; set; } = false;
    public string? OutputDevice { get; set; }
    public bool EnableGapless { get; set; } = true;
    public bool EnableReplayGain { get; set; } = false;
    public double PreampGain { get; set; } = 0;

    // Cache
    public int CacheSizeLimitMB { get; set; } = 512;
    public string? CacheLocation { get; set; }

    // Downloads
    public string? DownloadLocation { get; set; }
    public bool DownloadHighQuality { get; set; } = true;
    public bool AutoDownloadLiked { get; set; } = false;

    // Library
    public List<string> LibraryFolders { get; set; } = new();
    public bool AutoScanOnStartup { get; set; } = true;
    public bool ShowHiddenFiles { get; set; } = false;

    // Providers
    public bool EnableSoundCloud { get; set; } = true;
    public bool EnableSpotify { get; set; } = false;
    public string? SoundCloudClientId { get; set; }
    public string? SpotifyClientId { get; set; }
    public string? SpotifyClientSecret { get; set; }

    // Session
    public bool ResumeLastSession { get; set; } = true;
    public string? LastPlayedTrackId { get; set; }
    public TimeSpan? LastPlayedPosition { get; set; }
    public int? LastPlayedQueueIndex { get; set; }
}
