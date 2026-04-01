using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicApp.Enums;
using MusicApp.Models;
using MusicApp.Services;

namespace MusicApp.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly ISearchService _searchService;
    private readonly IPlaybackService _playbackService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private SearchResultType _selectedFilter = SearchResultType.All;

    [ObservableProperty]
    private SearchResults? _results;

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private bool _hasSearched;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

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
        if (!string.IsNullOrEmpty(value) && value.Length >= 2)
        {
            SearchCommand.Execute(null);
        }
    }

    partial void OnSelectedFilterChanged(SearchResultType value)
    {
        if (HasSearched)
        {
            SearchCommand.Execute(null);
        }
    }

    [RelayCommand]
    private async Task Search()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            return;
        }

        IsSearching = true;
        ErrorMessage = string.Empty;
        HasSearched = true;

        try
        {
            Results = await _searchService.SearchAsync(SearchQuery);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    private async Task PlayTrack(Track track)
    {
        await _playbackService.PlayAsync(track, Results?.Tracks);
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
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchQuery = string.Empty;
        Results = null;
        HasSearched = false;
    }
}
