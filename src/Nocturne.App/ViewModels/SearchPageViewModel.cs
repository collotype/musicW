using CommunityToolkit.Mvvm.Input;
using Nocturne.App.Models;
using Nocturne.App.Models.Enums;
using Nocturne.App.Services;

namespace Nocturne.App.ViewModels;

public sealed class SearchPageViewModel : PageViewModelBase
{
    private readonly ISearchService _searchService;
    private readonly IPlaybackService _playbackService;
    private readonly INavigationService _navigationService;

    private string _query = string.Empty;
    private SearchSourceFilter _selectedSourceFilter = SearchSourceFilter.All;
    private bool _isSearching;
    private string _statusMessage = "Search across local files, SoundCloud, and Spotify metadata.";
    private CancellationTokenSource? _searchCancellationTokenSource;

    public SearchPageViewModel(
        ISearchService searchService,
        IPlaybackService playbackService,
        INavigationService navigationService)
    {
        _searchService = searchService;
        _playbackService = playbackService;
        _navigationService = navigationService;

        SearchCommand = new AsyncRelayCommand(ExecuteSearchAsync);
        PlayTrackCommand = new AsyncRelayCommand<Track?>(PlayTrackAsync);
        OpenArtistCommand = new AsyncRelayCommand<Artist?>(OpenArtistAsync);
        OpenAlbumCommand = new AsyncRelayCommand<Album?>(OpenAlbumAsync);
        OpenPlaylistCommand = new AsyncRelayCommand<Playlist?>(OpenPlaylistAsync);
    }

    public IReadOnlyList<SearchSourceFilter> SourceOptions { get; } =
    [
        SearchSourceFilter.All,
        SearchSourceFilter.Local,
        SearchSourceFilter.SoundCloud,
        SearchSourceFilter.Spotify
    ];

    public string Query
    {
        get => _query;
        set => SetProperty(ref _query, value);
    }

    public SearchSourceFilter SelectedSourceFilter
    {
        get => _selectedSourceFilter;
        set => SetProperty(ref _selectedSourceFilter, value);
    }

    public SearchResults Results { get; private set; } = new();

    public bool IsSearching
    {
        get => _isSearching;
        private set => SetProperty(ref _isSearching, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public IAsyncRelayCommand SearchCommand { get; }

    public IAsyncRelayCommand<Track?> PlayTrackCommand { get; }

    public IAsyncRelayCommand<Artist?> OpenArtistCommand { get; }

    public IAsyncRelayCommand<Album?> OpenAlbumCommand { get; }

    public IAsyncRelayCommand<Playlist?> OpenPlaylistCommand { get; }

    public override async Task OnNavigatedToAsync(object? parameter)
    {
        if (Results.IsEmpty)
        {
            await ExecuteSearchAsync();
        }
    }

    private async Task ExecuteSearchAsync()
    {
        _searchCancellationTokenSource?.Cancel();
        _searchCancellationTokenSource = new CancellationTokenSource();

        try
        {
            IsSearching = true;
            StatusMessage = "Searching...";

            Results = await _searchService.SearchAsync(Query, SelectedSourceFilter, _searchCancellationTokenSource.Token);
            OnPropertyChanged(nameof(Results));
            StatusMessage = Results.IsEmpty
                ? "Nothing matched that query."
                : $"{Results.Tracks.Count + Results.Artists.Count + Results.Albums.Count + Results.Playlists.Count} results.";
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            IsSearching = false;
        }
    }

    private async Task PlayTrackAsync(Track? track)
    {
        if (track is null)
        {
            return;
        }

        await _playbackService.PlayQueueAsync(Results.Tracks, track, "search");
    }

    private Task OpenArtistAsync(Artist? artist) =>
        artist is null ? Task.CompletedTask : _navigationService.NavigateAsync<ArtistPageViewModel>(artist);

    private Task OpenAlbumAsync(Album? album) =>
        album is null ? Task.CompletedTask : _navigationService.NavigateAsync<AlbumPageViewModel>(album);

    private Task OpenPlaylistAsync(Playlist? playlist) =>
        playlist is null ? Task.CompletedTask : _navigationService.NavigateAsync<PlaylistPageViewModel>(playlist);
}
