using Microsoft.Win32;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicApp.Enums;
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
    private LibrarySection _selectedSection = LibrarySection.Overview;

    [ObservableProperty]
    private List<Track> _allTracks = new();

    [ObservableProperty]
    private List<Track> _likedTracks = new();

    [ObservableProperty]
    private List<Track> _offlineTracks = new();

    [ObservableProperty]
    private List<Track> _recentlyPlayedTracks = new();

    [ObservableProperty]
    private List<Artist> _favoriteArtists = new();

    [ObservableProperty]
    private List<Album> _savedAlbums = new();

    [ObservableProperty]
    private List<Playlist> _playlists = new();

    [ObservableProperty]
    private List<Playlist> _pinnedPlaylists = new();

    [ObservableProperty]
    private List<Playlist> _userPlaylists = new();

    [ObservableProperty]
    private List<Album> _downloadedAlbums = new();

    [ObservableProperty]
    private List<Playlist> _downloadedPlaylists = new();

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
    private int _totalPlaylists;

    [ObservableProperty]
    private int _pinnedCount;

    public bool IsOverview => SelectedSection == LibrarySection.Overview;
    public bool IsAllTracks => SelectedSection == LibrarySection.AllTracks;
    public bool IsLikedTracks => SelectedSection == LibrarySection.LikedTracks;
    public bool IsFavoriteArtists => SelectedSection == LibrarySection.FavoriteArtists;
    public bool IsAlbums => SelectedSection == LibrarySection.Albums;
    public bool IsPlaylists => SelectedSection == LibrarySection.Playlists;
    public bool IsOffline => SelectedSection == LibrarySection.Offline;
    public bool IsRecentlyPlayed => SelectedSection == LibrarySection.RecentlyPlayed;
    public bool IsPinned => SelectedSection == LibrarySection.Pinned;
    public bool HasAnyLibraryContent => TotalTracks > 0 || Playlists.Count > 0;
    public bool HasFavoriteArtists => FavoriteArtists.Count > 0;
    public bool HasSavedAlbums => SavedAlbums.Count > 0;
    public bool HasPinnedPlaylists => PinnedPlaylists.Count > 0;
    public bool HasUserPlaylists => UserPlaylists.Count > 0;
    public bool HasDownloadedAlbums => DownloadedAlbums.Count > 0;
    public bool HasDownloadedPlaylists => DownloadedPlaylists.Count > 0;
    public bool HasOfflineCollections => HasDownloadedAlbums || HasDownloadedPlaylists;
    public string OverviewSummary => $"{TotalTracks} tracks, {TotalAlbums} albums, {TotalArtists} artists, {TotalPlaylists} playlists";
    public string OfflineCollectionsSummary => HasOfflineCollections
        ? $"{DownloadedAlbums.Count} albums and {DownloadedPlaylists.Count} playlists are ready offline."
        : "Only downloaded tracks are available offline right now.";
    public string SectionTitle => SelectedSection switch
    {
        LibrarySection.AllTracks => "All Tracks",
        LibrarySection.LikedTracks => "Liked Tracks",
        LibrarySection.FavoriteArtists => "Favorite Artists",
        LibrarySection.Albums => "Saved Albums",
        LibrarySection.Playlists => "Playlists",
        LibrarySection.Offline => "Downloads & Offline",
        LibrarySection.RecentlyPlayed => "Recently Played",
        LibrarySection.Pinned => "Pinned",
        _ => "Library"
    };

    public string SectionSubtitle => SelectedSection switch
    {
        LibrarySection.AllTracks => "Everything currently indexed in your desktop library.",
        LibrarySection.LikedTracks => "Tracks you explicitly kept close.",
        LibrarySection.FavoriteArtists => "Artists you chose to follow across the workspace.",
        LibrarySection.Albums => "Albums saved into your listening shelf.",
        LibrarySection.Playlists => "User collections, system collections, and active mixes.",
        LibrarySection.Offline => "Downloaded tracks, albums, and playlists ready for offline use.",
        LibrarySection.RecentlyPlayed => "Fast return path into recent listening sessions.",
        LibrarySection.Pinned => "Collections you pinned for constant access.",
        _ => "A desktop-first view over your music workspace."
    };

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

    partial void OnSelectedSectionChanged(LibrarySection value)
    {
        NotifySectionStateChanged();
    }

    private void OnLibraryChanged(object? sender, EventArgs e)
    {
        LoadLibrary();
    }

    public void SetSection(LibrarySection section)
    {
        SelectedSection = section;
        NotifySectionStateChanged();
    }

    public void LoadLibrary()
    {
        AllTracks = _libraryService.AllTracks
            .OrderByDescending(track => track.DateAdded)
            .ToList();

        LikedTracks = _libraryService.LikedTracks;
        OfflineTracks = _libraryService.OfflineTracks;
        FavoriteArtists = _libraryService.FavoriteArtists
            .OrderByDescending(artist => artist.TrackCount)
            .ToList();

        SavedAlbums = _libraryService.SavedAlbums
            .OrderByDescending(album => album.ReleaseDate ?? DateTime.MinValue)
            .ThenBy(album => album.Title)
            .ToList();

        Playlists = _libraryService.Playlists
            .OrderByDescending(playlist => playlist.IsPinned)
            .ThenByDescending(playlist => playlist.LastModifiedDate ?? playlist.CreatedDate)
            .ToList();

        PinnedPlaylists = _libraryService.PinnedPlaylists;
        UserPlaylists = _libraryService.Playlists
            .Where(playlist => !playlist.IsSystemPlaylist)
            .OrderByDescending(playlist => playlist.LastModifiedDate ?? playlist.CreatedDate)
            .ToList();
        DownloadedAlbums = _libraryService.AllAlbums
            .Where(album => album.IsDownloaded)
            .OrderByDescending(album => album.ReleaseDate ?? DateTime.MinValue)
            .ToList();
        DownloadedPlaylists = _libraryService.Playlists
            .Where(playlist => playlist.IsDownloaded || (playlist.Tracks.Count > 0 && playlist.Tracks.All(track => track.IsDownloaded)))
            .OrderByDescending(playlist => playlist.LastModifiedDate ?? playlist.CreatedDate)
            .ToList();
        RecentlyPlayedTracks = _libraryService.AllTracks
            .Where(track => track.LastPlayedAt.HasValue)
            .OrderByDescending(track => track.LastPlayedAt)
            .Take(24)
            .ToList();

        TotalTracks = _libraryService.AllTracks.Count;
        TotalAlbums = _libraryService.AllAlbums.Count;
        TotalArtists = _libraryService.AllArtists.Count;
        LikedCount = _libraryService.LikedTracks.Count;
        OfflineCount = _libraryService.OfflineTracks.Count;
        TotalPlaylists = _libraryService.Playlists.Count;
        PinnedCount = _libraryService.PinnedPlaylists.Count;

        NotifySectionStateChanged();
        OnPropertyChanged(nameof(HasAnyLibraryContent));
        OnPropertyChanged(nameof(HasFavoriteArtists));
        OnPropertyChanged(nameof(HasSavedAlbums));
        OnPropertyChanged(nameof(HasPinnedPlaylists));
        OnPropertyChanged(nameof(HasUserPlaylists));
        OnPropertyChanged(nameof(HasDownloadedAlbums));
        OnPropertyChanged(nameof(HasDownloadedPlaylists));
        OnPropertyChanged(nameof(HasOfflineCollections));
        OnPropertyChanged(nameof(OverviewSummary));
        OnPropertyChanged(nameof(OfflineCollectionsSummary));
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
    private Task PlayTrack(Track? track)
    {
        if (track == null)
        {
            return Task.CompletedTask;
        }

        var queue = SelectedSection switch
        {
            LibrarySection.LikedTracks => LikedTracks,
            LibrarySection.Offline => OfflineTracks,
            LibrarySection.RecentlyPlayed => RecentlyPlayedTracks,
            _ => AllTracks
        };

        return _playbackService.PlayAsync(track, queue);
    }

    [RelayCommand]
    private Task PlayPlaylist(Playlist? playlist)
    {
        if (playlist == null || playlist.Tracks.Count == 0)
        {
            return Task.CompletedTask;
        }

        return _playbackService.PlayAsync(playlist.Tracks[0], playlist.Tracks);
    }

    [RelayCommand]
    private Task PlayAlbum(Album? album)
    {
        if (album == null || album.Tracks.Count == 0)
        {
            return Task.CompletedTask;
        }

        return _playbackService.PlayAsync(album.Tracks[0], album.Tracks);
    }

    [RelayCommand]
    private void NavigateToAlbum(string? albumId)
    {
        if (!string.IsNullOrWhiteSpace(albumId))
        {
            _navigationService.NavigateToAlbum(albumId);
        }
    }

    [RelayCommand]
    private void NavigateToArtist(string? artistId)
    {
        if (!string.IsNullOrWhiteSpace(artistId))
        {
            _navigationService.NavigateToArtist(artistId);
        }
    }

    [RelayCommand]
    private void NavigateToPlaylist(string? playlistId)
    {
        if (!string.IsNullOrWhiteSpace(playlistId))
        {
            _navigationService.NavigateToPlaylist(playlistId);
        }
    }

    [RelayCommand]
    private async Task CreatePlaylist()
    {
        await _libraryService.CreatePlaylistAsync($"New Playlist {DateTime.Now:HHmm}");
    }

    [RelayCommand]
    private async Task ToggleFavoriteArtist(Artist? artist)
    {
        if (artist == null)
        {
            return;
        }

        await _libraryService.ToggleFavoriteArtistAsync(artist.Id);
    }

    [RelayCommand]
    private async Task ToggleSaveAlbum(Album? album)
    {
        if (album == null)
        {
            return;
        }

        await _libraryService.ToggleSaveAlbumAsync(album.Id);
    }

    [RelayCommand]
    private async Task TogglePlaylistPin(Playlist? playlist)
    {
        if (playlist == null)
        {
            return;
        }

        await _libraryService.TogglePlaylistPinAsync(playlist.Id);
    }

    [RelayCommand]
    private void OpenPlaylist(string? playlistId)
    {
        if (!string.IsNullOrWhiteSpace(playlistId))
        {
            _navigationService.NavigateToPlaylist(playlistId);
        }
    }

    private void NotifySectionStateChanged()
    {
        OnPropertyChanged(nameof(IsOverview));
        OnPropertyChanged(nameof(IsAllTracks));
        OnPropertyChanged(nameof(IsLikedTracks));
        OnPropertyChanged(nameof(IsFavoriteArtists));
        OnPropertyChanged(nameof(IsAlbums));
        OnPropertyChanged(nameof(IsPlaylists));
        OnPropertyChanged(nameof(IsOffline));
        OnPropertyChanged(nameof(IsRecentlyPlayed));
        OnPropertyChanged(nameof(IsPinned));
        OnPropertyChanged(nameof(SectionTitle));
        OnPropertyChanged(nameof(SectionSubtitle));
        OnPropertyChanged(nameof(HasFavoriteArtists));
        OnPropertyChanged(nameof(HasSavedAlbums));
        OnPropertyChanged(nameof(HasPinnedPlaylists));
        OnPropertyChanged(nameof(HasUserPlaylists));
        OnPropertyChanged(nameof(HasDownloadedAlbums));
        OnPropertyChanged(nameof(HasDownloadedPlaylists));
        OnPropertyChanged(nameof(HasOfflineCollections));
        OnPropertyChanged(nameof(OverviewSummary));
        OnPropertyChanged(nameof(OfflineCollectionsSummary));
    }
}
