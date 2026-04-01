using CommunityToolkit.Mvvm.Input;
using Nocturne.App.Models;
using Nocturne.App.Services;

namespace Nocturne.App.ViewModels;

public sealed class LibraryPageViewModel : PageViewModelBase
{
    private readonly ILibraryService _libraryService;
    private readonly IPlaybackService _playbackService;
    private readonly INavigationService _navigationService;

    public LibraryPageViewModel(
        ILibraryService libraryService,
        IPlaybackService playbackService,
        INavigationService navigationService)
    {
        _libraryService = libraryService;
        _playbackService = playbackService;
        _navigationService = navigationService;

        PlayTrackCommand = new AsyncRelayCommand<Track?>(PlayTrackAsync);
        OpenArtistCommand = new AsyncRelayCommand<Artist?>(OpenArtistAsync);
        OpenAlbumCommand = new AsyncRelayCommand<Album?>(OpenAlbumAsync);
        OpenPlaylistCommand = new AsyncRelayCommand<Playlist?>(OpenPlaylistAsync);

        RefreshCollections();
        _libraryService.LibraryChanged += (_, _) => RefreshCollections();
    }

    public string Title => "Your Library";

    public ObservableCollection<Track> RecentlyAdded { get; } = [];

    public ObservableCollection<Track> FavoriteTracks { get; } = [];

    public ObservableCollection<Track> OfflineTracks { get; } = [];

    public ObservableCollection<Album> Albums { get; } = [];

    public ObservableCollection<Artist> Artists { get; } = [];

    public ObservableCollection<Playlist> Playlists { get; } = [];

    public int FavoriteCount => FavoriteTracks.Count;

    public int OfflineCount => OfflineTracks.Count;

    public int AlbumCount => Albums.Count;

    public int PlaylistCount => Playlists.Count;

    public IAsyncRelayCommand<Track?> PlayTrackCommand { get; }

    public IAsyncRelayCommand<Artist?> OpenArtistCommand { get; }

    public IAsyncRelayCommand<Album?> OpenAlbumCommand { get; }

    public IAsyncRelayCommand<Playlist?> OpenPlaylistCommand { get; }

    public override Task OnNavigatedToAsync(object? parameter)
    {
        RefreshCollections();
        return Task.CompletedTask;
    }

    private async Task PlayTrackAsync(Track? track)
    {
        if (track is null)
        {
            return;
        }

        var queue = RecentlyAdded.Count > 0 ? RecentlyAdded.ToList() : FavoriteTracks.ToList();
        if (queue.Count == 0)
        {
            queue = OfflineTracks.ToList();
        }

        await _playbackService.PlayQueueAsync(queue, track, "library");
    }

    private Task OpenArtistAsync(Artist? artist) =>
        artist is null ? Task.CompletedTask : _navigationService.NavigateAsync<ArtistPageViewModel>(artist);

    private Task OpenAlbumAsync(Album? album) =>
        album is null ? Task.CompletedTask : _navigationService.NavigateAsync<AlbumPageViewModel>(album);

    private Task OpenPlaylistAsync(Playlist? playlist) =>
        playlist is null ? Task.CompletedTask : _navigationService.NavigateAsync<PlaylistPageViewModel>(playlist);

    private void RefreshCollections()
    {
        ReplaceCollection(RecentlyAdded, _libraryService.GetRecentlyAdded(8));
        ReplaceCollection(FavoriteTracks, _libraryService.GetFavorites().Take(6));
        ReplaceCollection(OfflineTracks, _libraryService.GetOfflineTracks().Take(6));
        ReplaceCollection(Albums, _libraryService.GetAlbums().Take(8));
        ReplaceCollection(Artists, _libraryService.GetArtists().Take(8));
        ReplaceCollection(Playlists, _libraryService.GetPlaylists().Take(8));

        OnPropertyChanged(nameof(FavoriteCount));
        OnPropertyChanged(nameof(OfflineCount));
        OnPropertyChanged(nameof(AlbumCount));
        OnPropertyChanged(nameof(PlaylistCount));
    }

    private static void ReplaceCollection<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }
}
