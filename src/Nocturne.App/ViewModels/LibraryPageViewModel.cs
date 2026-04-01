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

    public ObservableCollection<Album> Albums { get; } = [];

    public ObservableCollection<Artist> Artists { get; } = [];

    public ObservableCollection<Playlist> Playlists { get; } = [];

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

        await _playbackService.PlayQueueAsync(RecentlyAdded.ToList(), track, "library");
    }

    private Task OpenArtistAsync(Artist? artist)
    {
        return artist is null
            ? Task.CompletedTask
            : _navigationService.NavigateAsync<ArtistPageViewModel>(artist);
    }

    private Task OpenAlbumAsync(Album? album)
    {
        return album is null
            ? Task.CompletedTask
            : _navigationService.NavigateAsync<AlbumPageViewModel>(album);
    }

    private Task OpenPlaylistAsync(Playlist? playlist)
    {
        return playlist is null
            ? Task.CompletedTask
            : _navigationService.NavigateAsync<PlaylistPageViewModel>(playlist);
    }

    private void RefreshCollections()
    {
        ReplaceCollection(RecentlyAdded, _libraryService.GetRecentlyAdded());
        ReplaceCollection(FavoriteTracks, _libraryService.GetFavorites());
        ReplaceCollection(Albums, _libraryService.GetAlbums());
        ReplaceCollection(Artists, _libraryService.GetArtists());
        ReplaceCollection(Playlists, _libraryService.GetPlaylists());
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
