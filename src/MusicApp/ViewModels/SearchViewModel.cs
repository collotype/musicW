using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicApp.Enums;
using MusicApp.Models;
using MusicApp.Services;

namespace MusicApp.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private CancellationTokenSource? _debounceCts;
    private CancellationTokenSource? _searchCts;

    private readonly ISearchService _searchService;
    private readonly IPlaybackService _playbackService;
    private readonly INavigationService _navigationService;
    private readonly IQueueService _queueService;
    private readonly ILibraryService _libraryService;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private SearchTab _selectedTab = SearchTab.Tracks;

    [ObservableProperty]
    private SearchResults _localResults = new();

    [ObservableProperty]
    private SearchResults _onlineResults = new();

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private bool _hasSearched;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _selectedPlaylistId = string.Empty;

    public bool HasTrackResults => LocalResults.Tracks.Count > 0 || OnlineResults.Tracks.Count > 0;
    public bool HasArtistResults => LocalResults.Artists.Count > 0 || OnlineResults.Artists.Count > 0;
    public bool HasAlbumResults => LocalResults.Albums.Count > 0 || OnlineResults.Albums.Count > 0;
    public bool HasPlaylistResults => LocalResults.Playlists.Count > 0 || OnlineResults.Playlists.Count > 0;
    public bool HasNoResults => HasSearched && !IsSearching && string.IsNullOrWhiteSpace(ErrorMessage) && !HasTrackResults && !HasArtistResults && !HasAlbumResults && !HasPlaylistResults;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool CanClearSearch => !string.IsNullOrWhiteSpace(SearchQuery);
    public List<Playlist> AvailablePlaylists => _libraryService.Playlists.OrderByDescending(playlist => playlist.IsPinned).ThenBy(playlist => playlist.Title).ToList();
    public string CurrentTabTitle => SelectedTab.ToString();

    public SearchViewModel(
        ISearchService searchService,
        IPlaybackService playbackService,
        INavigationService navigationService,
        IQueueService queueService,
        ILibraryService libraryService)
    {
        _searchService = searchService;
        _playbackService = playbackService;
        _navigationService = navigationService;
        _queueService = queueService;
        _libraryService = libraryService;

        _libraryService.LibraryChanged += (_, _) => OnPropertyChanged(nameof(AvailablePlaylists));
    }

    partial void OnSearchQueryChanged(string value)
    {
        OnPropertyChanged(nameof(CanClearSearch));

        if (string.IsNullOrWhiteSpace(value))
        {
            ResetSearch();
            return;
        }

        if (value.Trim().Length < 2)
        {
            CancelPendingSearch();
            LocalResults = new SearchResults { Query = value.Trim() };
            OnlineResults = new SearchResults { Query = value.Trim() };
            ErrorMessage = string.Empty;
            HasSearched = false;
            IsSearching = false;
            NotifyResultStateChanged();
            return;
        }

        _ = QueueSearchAsync();
    }

    partial void OnSelectedTabChanged(SearchTab value)
    {
        OnPropertyChanged(nameof(CurrentTabTitle));
    }

    [RelayCommand]
    private async Task Search()
    {
        var query = SearchQuery.Trim();
        if (query.Length < 2)
        {
            ResetSearch();
            return;
        }

        CancelActiveSearch();

        var searchCts = new CancellationTokenSource();
        _searchCts = searchCts;
        IsSearching = true;
        ErrorMessage = string.Empty;
        HasSearched = true;

        try
        {
            var localTask = _searchService.SearchLocalAsync(query);
            var onlineTask = _searchService.SearchOnlineAsync(query, searchCts.Token);

            await Task.WhenAll(localTask, onlineTask);

            if (_searchCts != searchCts || searchCts.IsCancellationRequested)
            {
                return;
            }

            LocalResults = await localTask;
            OnlineResults = await onlineTask;
            ErrorMessage = OnlineResults.ErrorMessage ?? string.Empty;
        }
        catch (OperationCanceledException) when (searchCts.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            if (_searchCts != searchCts)
            {
                return;
            }

            LocalResults = new SearchResults { Query = query };
            OnlineResults = new SearchResults { Query = query };
            ErrorMessage = ex.Message;
        }
        finally
        {
            if (_searchCts == searchCts)
            {
                IsSearching = false;
                _searchCts.Dispose();
                _searchCts = null;
                NotifyResultStateChanged();
            }
        }
    }

    [RelayCommand]
    private Task PlayTrack(Track? track)
    {
        if (track == null)
        {
            return Task.CompletedTask;
        }

        var queue = LocalResults.Tracks.Concat(OnlineResults.Tracks).DistinctBy(item => item.Id).ToList();
        return _playbackService.PlayAsync(track, queue);
    }

    [RelayCommand]
    private void QueueTrack(Track? track)
    {
        if (track != null)
        {
            _queueService.AddToQueue(track);
        }
    }

    [RelayCommand]
    private async Task ToggleLike(Track? track)
    {
        if (track == null)
        {
            return;
        }

        if (_libraryService.AllTracks.All(item => item.Id != track.Id))
        {
            await _libraryService.AddTrackAsync(track);
        }

        await _libraryService.ToggleLikeAsync(track.Id);
    }

    [RelayCommand]
    private async Task AddTrackToSelectedPlaylist(Track? track)
    {
        if (track == null || string.IsNullOrWhiteSpace(SelectedPlaylistId))
        {
            return;
        }

        await _libraryService.AddToPlaylistAsync(SelectedPlaylistId, track);
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
    private void ClearSearch()
    {
        SearchQuery = string.Empty;
    }

    private async Task QueueSearchAsync()
    {
        CancelDebounceOnly();

        var debounceCts = new CancellationTokenSource();
        _debounceCts = debounceCts;

        try
        {
            await Task.Delay(250, debounceCts.Token);
            await Search();
        }
        catch (OperationCanceledException) when (debounceCts.IsCancellationRequested)
        {
        }
        finally
        {
            if (_debounceCts == debounceCts)
            {
                _debounceCts.Dispose();
                _debounceCts = null;
            }
        }
    }

    private void ResetSearch()
    {
        CancelPendingSearch();
        LocalResults = new SearchResults();
        OnlineResults = new SearchResults();
        ErrorMessage = string.Empty;
        HasSearched = false;
        IsSearching = false;
        NotifyResultStateChanged();
    }

    private void CancelPendingSearch()
    {
        CancelDebounceOnly();
        CancelActiveSearch();
    }

    private void CancelDebounceOnly()
    {
        _debounceCts?.Cancel();
    }

    private void CancelActiveSearch()
    {
        _searchCts?.Cancel();
    }

    private void NotifyResultStateChanged()
    {
        OnPropertyChanged(nameof(HasTrackResults));
        OnPropertyChanged(nameof(HasArtistResults));
        OnPropertyChanged(nameof(HasAlbumResults));
        OnPropertyChanged(nameof(HasPlaylistResults));
        OnPropertyChanged(nameof(HasNoResults));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(CurrentTabTitle));
        OnPropertyChanged(nameof(AvailablePlaylists));
    }
}
