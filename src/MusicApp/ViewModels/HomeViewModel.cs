using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicApp.Enums;
using MusicApp.Models;
using MusicApp.Services;

namespace MusicApp.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly IRecommendationService _recommendationService;
    private readonly IPlaybackService _playbackService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private List<Track> _continueListening = new();

    [ObservableProperty]
    private List<Artist> _favoriteArtists = new();

    [ObservableProperty]
    private List<Album> _suggestedAlbums = new();

    [ObservableProperty]
    private List<Playlist> _highlightedPlaylists = new();

    [ObservableProperty]
    private List<Track> _discoveryTracks = new();

    [ObservableProperty]
    private List<SnippetMoment> _snippetMoments = new();

    public bool HasLibraryDrivenHome => ContinueListening.Count > 0 || FavoriteArtists.Count > 0 || SuggestedAlbums.Count > 0 || HighlightedPlaylists.Count > 0 || DiscoveryTracks.Count > 0;
    public bool HasFavoriteArtists => FavoriteArtists.Count > 0;
    public bool HasSuggestedAlbums => SuggestedAlbums.Count > 0;
    public bool HasHighlightedPlaylists => HighlightedPlaylists.Count > 0;
    public bool HasSnippets => SnippetMoments.Count > 0;

    public HomeViewModel(
        IRecommendationService recommendationService,
        IPlaybackService playbackService,
        INavigationService navigationService)
    {
        _recommendationService = recommendationService;
        _playbackService = playbackService;
        _navigationService = navigationService;
    }

    public void Refresh()
    {
        ContinueListening = _recommendationService.GetContinueListening(8);
        FavoriteArtists = _recommendationService.GetFavoriteArtistSuggestions(8);
        SuggestedAlbums = _recommendationService.GetSuggestedAlbums(8);
        HighlightedPlaylists = _recommendationService.GetHighlightedPlaylists(6);
        DiscoveryTracks = _recommendationService.GetDiscoveryTracks(12);
        SnippetMoments = _recommendationService.CreateSnippets(WaveSeed.Home(), _recommendationService.CreateTunerFromSettings(), 6);

        OnPropertyChanged(nameof(HasLibraryDrivenHome));
        OnPropertyChanged(nameof(HasFavoriteArtists));
        OnPropertyChanged(nameof(HasSuggestedAlbums));
        OnPropertyChanged(nameof(HasHighlightedPlaylists));
        OnPropertyChanged(nameof(HasSnippets));
    }

    [RelayCommand]
    private Task PlayTrack(Track? track)
    {
        if (track == null)
        {
            return Task.CompletedTask;
        }

        var queue = DiscoveryTracks.Count > 0 ? DiscoveryTracks : ContinueListening;
        return _playbackService.PlayAsync(track, queue);
    }

    [RelayCommand]
    private async Task PlaySnippet(SnippetMoment? snippet)
    {
        if (snippet == null)
        {
            return;
        }

        await _playbackService.PlayAsync(snippet.Track, DiscoveryTracks);
        await _playbackService.SeekAsync(snippet.StartTime);
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
    private void NavigateToAlbum(string? albumId)
    {
        if (!string.IsNullOrWhiteSpace(albumId))
        {
            _navigationService.NavigateToAlbum(albumId);
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
    private void OpenMyWave()
    {
        _navigationService.NavigateToMyWave(WaveSeed.Home());
    }

    [RelayCommand]
    private void OpenSearch()
    {
        _navigationService.NavigateToSearch();
    }

    [RelayCommand]
    private void OpenLikedTracks()
    {
        _navigationService.NavigateToLibrary(LibrarySection.LikedTracks);
    }

    [RelayCommand]
    private void OpenLibrary()
    {
        _navigationService.NavigateToLibrary();
    }
}
