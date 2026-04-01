namespace Nocturne.App.Models;

public sealed class SettingsModel
{
    public string AccentColorHex { get; set; } = "#B7F9E4";

    public string LanguageCode { get; set; } = "en-US";

    public bool RememberLastSession { get; set; } = true;

    public bool StartMinimized { get; set; }

    public bool SoundCloudEnabled { get; set; } = true;

    public bool NormalizeAudio { get; set; }

    public string CacheFolderPath { get; set; } = Helpers.AppPaths.CacheFolder;

    public ObservableCollection<string> LocalMusicFolders { get; set; } =
    [
        Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)
    ];

    public string SpotifyClientId { get; set; } = string.Empty;

    public string SpotifyClientSecret { get; set; } = string.Empty;

    public string SpotifyMarket { get; set; } = "US";

    public bool DownloadTempStreams { get; set; }

    public string LastVisitedPage { get; set; } = "Library";
}
