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

    public LibraryViewModel(
        ILibraryService libraryService,
        IPlaybackService playbackService,
        INavigationService navigationService)
    {
        _libraryService = libraryService;
        _playbackService = playbackService;
        _navigationService = navigationService;

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

        // Get recent items (last added/played)
        RecentTracks = _libraryService.AllTracks.OrderByDescending(t => t.PlayCount ?? 0).Take(10).ToList();
        RecentAlbums = _libraryService.AllAlbums.OrderByDescending(a => a.ReleaseDate).Take(10).ToList();
        RecentArtists = _libraryService.AllArtists.Take(10).ToList();
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
}
