using MusicApp.Enums;
using MusicApp.Models;
using MusicApp.Providers;

namespace MusicApp.Services;

public interface INavigationService
{
    event EventHandler<NavigationEventArgs>? Navigated;
    NavigationRequest CurrentRequest { get; }
    void NavigateToHome();
    void NavigateToMyWave(WaveSeed? seed = null);
    void NavigateToArtist(string artistId, string providerName = "Local");
    void NavigateToAlbum(string albumId, string providerName = "Local");
    void NavigateToPlaylist(string playlistId, string providerName = "Local");
    void NavigateToLibrary(LibrarySection section = LibrarySection.Overview);
    void NavigateToSearch(string? query = null);
    void NavigateToQueue();
    void NavigateToSettings();
    void GoBack();
}

public class NavigationRequest
{
    public NavigationPage Page { get; set; } = NavigationPage.Home;
    public string? ItemId { get; set; }
    public string ProviderName { get; set; } = "Local";
    public LibrarySection LibrarySection { get; set; } = LibrarySection.Overview;
    public string? Query { get; set; }
    public WaveSeed? WaveSeed { get; set; }
}

public class NavigationEventArgs : EventArgs
{
    public required NavigationRequest Request { get; init; }
}

public interface IPlaybackService
{
    event EventHandler<PlaybackState>? StateChanged;
    PlaybackState CurrentState { get; }
    Task PlayAsync(Track track, List<Track>? queue = null);
    Task PlayQueueAsync(List<QueueItem> queue, int startIndex = 0);
    Task PauseAsync();
    Task ResumeAsync();
    Task TogglePlayPauseAsync();
    Task StopAsync();
    Task NextAsync();
    Task PreviousAsync();
    Task SeekAsync(TimeSpan position);
    Task SetVolumeAsync(double volume);
    Task ToggleMuteAsync();
    Task SetRepeatModeAsync(RepeatMode mode);
    Task ToggleShuffleAsync();
    Task LikeCurrentTrackAsync();
    void Dispose();
}

public interface IQueueService
{
    event EventHandler? QueueChanged;
    List<QueueItem> Queue { get; }
    int CurrentIndex { get; }
    QueueItem? CurrentItem { get; }
    bool SmartQueueEnabled { get; set; }
    void SetQueue(List<QueueItem> queue, int startIndex = 0);
    void AddToQueue(Track track);
    void AddToQueueNext(Track track);
    void RemoveFromQueue(int index);
    void ClearQueue();
    void MoveToNext();
    void MoveToPrevious();
    void MoveItem(int oldIndex, int newIndex);
    void SetCurrentIndex(int index);
    void Shuffle();
    void AppendRecommendations(IEnumerable<QueueItem> recommendations);
    QueueItem? GetNextTrack();
    QueueItem? GetPreviousTrack();
}

public interface ILibraryService
{
    event EventHandler? LibraryChanged;
    List<Track> AllTracks { get; }
    List<Artist> AllArtists { get; }
    List<Album> AllAlbums { get; }
    List<Playlist> Playlists { get; }
    List<Artist> FavoriteArtists { get; }
    List<Album> SavedAlbums { get; }
    List<Playlist> PinnedPlaylists { get; }
    List<Track> LikedTracks { get; }
    List<Track> OfflineTracks { get; }
    Task InitializeAsync();
    Task AddTrackAsync(Track track);
    Task RemoveTrackAsync(string trackId);
    Task ToggleLikeAsync(string trackId);
    Task<bool> IsLikedAsync(string trackId);
    Task CreatePlaylistAsync(string title, string? description = null);
    Task AddToPlaylistAsync(string playlistId, Track track);
    Task RemoveFromPlaylistAsync(string playlistId, string trackId);
    Task ReorderPlaylistTrackAsync(string playlistId, int fromIndex, int toIndex);
    Task TogglePlaylistPinAsync(string playlistId);
    Task DeletePlaylistAsync(string playlistId);
    Task<Playlist?> GetPlaylistAsync(string playlistId);
    Task<Artist?> GetArtistAsync(string artistId);
    Task<Album?> GetAlbumAsync(string albumId);
    Task ToggleFavoriteArtistAsync(string artistId);
    Task ToggleSaveAlbumAsync(string albumId);
}

public interface ISearchService
{
    Task<SearchResults> SearchAsync(string query, SearchResultType filter = SearchResultType.All, CancellationToken cancellationToken = default);
    Task<SearchResults> SearchLocalAsync(string query);
    Task<SearchResults> SearchOnlineAsync(string query, CancellationToken cancellationToken = default);
}

public interface IImageCacheService
{
    Task<string?> GetCachedImagePathAsync(string url);
    Task CacheImageAsync(string url);
    Task ClearCacheAsync();
    Task<long> GetCacheSizeAsync();
}

public interface ISettingsService
{
    SettingsModel Settings { get; }
    event EventHandler<SettingsModel>? SettingsChanged;
    Task LoadSettingsAsync();
    Task SaveSettingsAsync();
    Task UpdateSettingsAsync(Action<SettingsModel> update);
}

public interface IDownloadService
{
    event EventHandler<DownloadProgressEventArgs>? ProgressChanged;
    Task<bool> DownloadTrackAsync(Track track, string? destinationPath = null);
    Task CancelDownloadAsync(string trackId);
    Task<List<Track>> GetDownloadedTracksAsync();
    Task<bool> IsDownloadedAsync(string trackId);
    Task DeleteDownloadAsync(string trackId);
}

public class DownloadProgressEventArgs : EventArgs
{
    public string TrackId { get; set; } = string.Empty;
    public double Progress { get; set; }
    public bool IsComplete { get; set; }
    public string? ErrorMessage { get; set; }
}

public interface ILocalMusicScannerService
{
    event EventHandler? ScanCompleted;
    Task InitializeAsync();
    Task ScanLibraryAsync();
    Task ScanFolderAsync(string path);
    Task ImportFilesAsync(IEnumerable<string> filePaths);
    Task RefreshMetadataAsync(string trackId);
}

public interface ILyricsService
{
    Task<LyricsDocument> GetLyricsAsync(Track? track);
}

public interface ITimedCommentService
{
    event EventHandler<string>? CommentsChanged;
    Task<List<TimedComment>> GetCommentsAsync(string trackId);
    Task AddCommentAsync(string trackId, TimeSpan timestamp, string text, string authorName = "You");
    Task DeleteCommentAsync(string commentId);
    Task ToggleFavoriteMomentAsync(string commentId);
}

public interface IRecommendationService
{
    WaveTunerSettings CreateTunerFromSettings();
    Task SaveTunerAsync(WaveTunerSettings tuner);
    List<Track> GetContinueListening(int count = 8);
    List<Album> GetSuggestedAlbums(int count = 8);
    List<Artist> GetFavoriteArtistSuggestions(int count = 8);
    List<Playlist> GetHighlightedPlaylists(int count = 6);
    List<Track> GetDiscoveryTracks(int count = 12);
    List<WaveRecommendation> CreateWave(WaveSeed seed, WaveTunerSettings tuner, int count = 24);
    List<SnippetMoment> CreateSnippets(WaveSeed seed, WaveTunerSettings tuner, int count = 8);
    List<QueueItem> GetSmartQueueTracks(Track? currentTrack, int count = 6, IEnumerable<string>? excludeTrackIds = null);
}

public interface IMusicProviderService
{
    List<IMusicProvider> Providers { get; }
    Task<SearchResults> SearchAsync(string query, CancellationToken cancellationToken = default);
    Task<Artist?> GetArtistAsync(string artistId, string providerName);
    Task<List<Track>> GetArtistTracksAsync(string artistId, string providerName);
    Task<List<Album>> GetArtistReleasesAsync(string artistId, string providerName);
    Task<Album?> GetAlbumAsync(string albumId, string providerName);
    Task<Playlist?> GetPlaylistAsync(string playlistId, string providerName);
    Task<string?> ResolvePlaybackUrlAsync(Track track, CancellationToken cancellationToken = default);
}
