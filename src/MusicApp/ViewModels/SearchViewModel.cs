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

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private SearchResultType _selectedFilter = SearchResultType.All;

    [ObservableProperty]
    private SearchResults _results = new();

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private bool _hasSearched;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public bool HasTrackResults => Results.Tracks.Count > 0;
    public bool HasArtistResults => Results.Artists.Count > 0;
    public bool HasAlbumResults => Results.Albums.Count > 0;
    public bool HasPlaylistResults => Results.Playlists.Count > 0;
    public bool HasNoResults => HasSearched && !IsSearching && string.IsNullOrWhiteSpace(ErrorMessage) && !Results.HasAnyResults;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool CanClearSearch => !string.IsNullOrWhiteSpace(SearchQuery);

    public SearchViewModel(
        ISearchService searchService,
        IPlaybackService playbackService,
        INavigationService navigationService)
    {
        _searchService = searchService;
        _playbackService = playbackService;
        _navigationService = navigationService;
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
            Results = new SearchResults { Query = value.Trim() };
            ErrorMessage = string.Empty;
            HasSearched = false;
            IsSearching = false;
            return;
        }

        _ = QueueSearchAsync();
    }

    partial void OnSelectedFilterChanged(SearchResultType value)
    {
        if (SearchQuery.Trim().Length >= 2)
        {
            _ = QueueSearchAsync();
        }
    }

    partial void OnResultsChanged(SearchResults value) => NotifyResultStateChanged();
    partial void OnIsSearchingChanged(bool value) => NotifyResultStateChanged();
    partial void OnHasSearchedChanged(bool value) => NotifyResultStateChanged();
    partial void OnErrorMessageChanged(string value) => NotifyResultStateChanged();

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
            var results = await _searchService.SearchAsync(query, SelectedFilter, searchCts.Token);
            if (_searchCts != searchCts || searchCts.IsCancellationRequested)
            {
                return;
            }

            Results = results;
            ErrorMessage = results.ErrorMessage ?? string.Empty;
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

            Results = new SearchResults { Query = query };
            ErrorMessage = ex.Message;
        }
        finally
        {
            if (_searchCts == searchCts)
            {
                IsSearching = false;
                _searchCts.Dispose();
                _searchCts = null;
            }
        }
    }

    [RelayCommand]
    private async Task PlayTrack(Track track)
    {
        await _playbackService.PlayAsync(track, Results.Tracks);
    }

    [RelayCommand]
    private void NavigateToAlbum(string albumId)
    {
        if (!string.IsNullOrWhiteSpace(albumId))
        {
            _navigationService.NavigateToAlbum(albumId);
        }
    }

    [RelayCommand]
    private void NavigateToArtist(string artistId)
    {
        if (!string.IsNullOrWhiteSpace(artistId))
        {
            _navigationService.NavigateToArtist(artistId);
        }
    }

    [RelayCommand]
    private void NavigateToPlaylist(string playlistId)
    {
        if (!string.IsNullOrWhiteSpace(playlistId))
        {
            _navigationService.NavigateToPlaylist(playlistId);
        }
    }

    [RelayCommand]
    private void ClearSearch()
    {
        if (string.IsNullOrEmpty(SearchQuery))
        {
            ResetSearch();
            return;
        }

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
        Results = new SearchResults();
        ErrorMessage = string.Empty;
        HasSearched = false;
        IsSearching = false;
    }

    private void CancelPendingSearch()
    {
        CancelDebounceOnly();
        CancelActiveSearch();
    }

    private void CancelDebounceOnly()
    {
        if (_debounceCts == null)
        {
            return;
        }

        _debounceCts.Cancel();
    }

    private void CancelActiveSearch()
    {
        if (_searchCts == null)
        {
            return;
        }

        _searchCts.Cancel();
    }

    private void NotifyResultStateChanged()
    {
        OnPropertyChanged(nameof(HasTrackResults));
        OnPropertyChanged(nameof(HasArtistResults));
        OnPropertyChanged(nameof(HasAlbumResults));
        OnPropertyChanged(nameof(HasPlaylistResults));
        OnPropertyChanged(nameof(HasNoResults));
        OnPropertyChanged(nameof(HasError));
    }
}
