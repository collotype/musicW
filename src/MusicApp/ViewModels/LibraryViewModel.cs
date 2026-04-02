using Microsoft.Win32;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicApp.Models;
using MusicApp.Services;

namespace MusicApp.ViewModels;

public partial class LibraryViewModel : ObservableObject
{
    private readonly ILibraryService _libraryService;
    private readonly IPlaybackService _playbackService;
    private readonly INavigationService _navigationService;
    private readonly ILocalMusicScannerService _localMusicScannerService;

    [ObservableProperty]
    private List<Track> _recentTracks = new();

    [ObservableProperty]
    private List<Album> _recentAlbums = new();

    [ObservableProperty]
    private List<Artist> _recentArtists = new();

    [ObservableProperty]
    private List<Playlist> _playlists = new();

    [ObservableProperty]
    private int _totalTracks;

    [ObservableProperty]
    private int _totalAlbums;

    [ObservableProperty]
    private int _totalArtists;

    [ObservableProperty]
    private int _likedCount;

    [ObservableProperty]
    private int _offlineCount;

    [ObservableProperty]
    private string _selectedPlaylistId = string.Empty;

    public bool HasLibrarySummary => TotalTracks > 0 || TotalAlbums > 0 || TotalArtists > 0 || LikedCount > 0 || OfflineCount > 0;
    public bool HasRecentTracks => RecentTracks.Count > 0;
    public bool HasRecentAlbums => RecentAlbums.Count > 0;
    public bool HasRecentArtists => RecentArtists.Count > 0;
    public bool HasPlaylists => Playlists.Count > 0;
    public bool ShowEmptyState => !HasLibrarySummary;
    public string EmptyStateTitle => HasPlaylists ? "No tracks in your library yet" : "Your library is empty";
    public string EmptyStateMessage => HasPlaylists
        ? "You have playlists ready, but no real tracks have been imported yet."
        : "Import some audio files to start building your local library.";

    public LibraryViewModel(
        ILibraryService libraryService,
        IPlaybackService playbackService,
        INavigationService navigationService,
        ILocalMusicScannerService localMusicScannerService)
    {
        _libraryService = libraryService;
        _playbackService = playbackService;
        _navigationService = navigationService;
        _localMusicScannerService = localMusicScannerService;

        _libraryService.LibraryChanged += OnLibraryChanged;
    }

    private void OnLibraryChanged(object? sender, EventArgs e)
    {
        LoadLibrary();
    }

    public void LoadLibrary()
    {
        TotalTracks = _libraryService.AllTracks.Count;
        TotalAlbums = _libraryService.AllAlbums.Count;
        TotalArtists = _libraryService.AllArtists.Count;
        LikedCount = _libraryService.LikedTracks.Count;
        OfflineCount = _libraryService.OfflineTracks.Count;

        Playlists = _libraryService.Playlists.ToList();

        RecentTracks = _libraryService.AllTracks
            .OrderByDescending(t => t.PlayCount ?? 0)
            .ThenBy(t => t.Title)
            .Take(10)
            .ToList();

        RecentAlbums = _libraryService.AllAlbums
            .OrderByDescending(a => a.ReleaseDate ?? DateTime.MinValue)
            .ThenBy(a => a.Title)
            .Take(10)
            .ToList();

        RecentArtists = _libraryService.AllArtists
            .OrderByDescending(a => a.TrackCount)
            .ThenBy(a => a.Name)
            .Take(10)
            .ToList();

        NotifyLibraryStateChanged();
    }

    [RelayCommand]
    private async Task ImportTracks()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Import Tracks",
            Multiselect = true,
            Filter = "Audio Files|*.mp3;*.wav;*.flac;*.m4a;*.wma;*.ogg;*.aac"
        };

        if (dialog.ShowDialog() != true || dialog.FileNames.Length == 0)
        {
            return;
        }

        await _localMusicScannerService.ImportFilesAsync(dialog.FileNames);
        LoadLibrary();
    }

    [RelayCommand]
    private async Task PlayTrack(Track track)
    {
        await _playbackService.PlayAsync(track, RecentTracks);
    }

    [RelayCommand]
    private void NavigateToAlbum(string albumId)
    {
        _navigationService.NavigateToAlbum(albumId);
    }

    [RelayCommand]
    private void NavigateToArtist(string artistId)
    {
        _navigationService.NavigateToArtist(artistId);
    }

    [RelayCommand]
    private void NavigateToPlaylist(string playlistId)
    {
        _navigationService.NavigateToPlaylist(playlistId);
        SelectedPlaylistId = playlistId;
    }

    [RelayCommand]
    private async Task CreatePlaylist()
    {
        await _libraryService.CreatePlaylistAsync($"New Playlist {DateTime.Now:MMddyy}");
    }

    [RelayCommand]
    private void NavigateToFavorites()
    {
        _navigationService.NavigateToPlaylist("favorites");
        SelectedPlaylistId = "favorites";
    }

    [RelayCommand]
    private void NavigateToOffline()
    {
        _navigationService.NavigateToPlaylist("offline");
        SelectedPlaylistId = "offline";
    }

    private void NotifyLibraryStateChanged()
    {
        OnPropertyChanged(nameof(HasLibrarySummary));
        OnPropertyChanged(nameof(HasRecentTracks));
        OnPropertyChanged(nameof(HasRecentAlbums));
        OnPropertyChanged(nameof(HasRecentArtists));
        OnPropertyChanged(nameof(HasPlaylists));
        OnPropertyChanged(nameof(ShowEmptyState));
        OnPropertyChanged(nameof(EmptyStateTitle));
        OnPropertyChanged(nameof(EmptyStateMessage));
    }
}
