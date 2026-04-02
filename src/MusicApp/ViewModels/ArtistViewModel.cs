using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicApp.Enums;
using MusicApp.Models;
using MusicApp.Services;

namespace MusicApp.ViewModels;

public partial class ArtistViewModel : ObservableObject
{
    private readonly IMusicProviderService _providerService;
    private readonly IPlaybackService _playbackService;
    private readonly INavigationService _navigationService;
    private readonly ILibraryService _libraryService;

    private string _providerName = "Local";

    [ObservableProperty]
    private Artist? _artist;

    [ObservableProperty]
    private List<Track> _topTracks = new();

    [ObservableProperty]
    private List<Album> _albums = new();

    [ObservableProperty]
    private List<Artist> _relatedArtists = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _selectedPlaylistId = string.Empty;

    public bool HasTopTracks => TopTracks.Count > 0;
    public bool HasAlbums => Albums.Count > 0;
    public bool HasGenres => Artist?.Genres.Count > 0;
    public bool HasRelatedArtists => RelatedArtists.Count > 0;
    public bool HasBiography => !string.IsNullOrWhiteSpace(Artist?.Biography);
    public bool IsFavoriteArtist => Artist?.IsFollowed == true;
    public List<Playlist> AvailablePlaylists => _libraryService.Playlists;
    public string SourceLabel => _providerName;
    public string HeaderSummary => Artist == null
        ? string.Empty
        : $"{Artist.MonthlyListenersFormatted} monthly listeners • {Artist.TrackCount} tracks • {Artist.AlbumCount} releases";
    public string TrackSummary => HasTopTracks ? $"{TopTracks.Count} tracks ready for playback." : "No artist tracks available.";
    public string ReleaseSummary => HasAlbums ? $"{Albums.Count} releases available in-app." : "No releases surfaced for this artist yet.";

    public ArtistViewModel(
        IMusicProviderService providerService,
        IPlaybackService playbackService,
        INavigationService navigationService,
        ILibraryService libraryService)
    {
        _providerService = providerService;
        _playbackService = playbackService;
        _navigationService = navigationService;
        _libraryService = libraryService;

        EnsureSelectedPlaylist();
    }

    public async Task LoadArtistAsync(string artistId, string providerName = "Local")
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        _providerName = providerName;

        try
        {
            Artist = await _providerService.GetArtistAsync(artistId, providerName);
            if (Artist == null)
            {
                ErrorMessage = "Artist not found.";
                return;
            }

            TopTracks = await _providerService.GetArtistTracksAsync(artistId, providerName);
            Albums = await _providerService.GetArtistReleasesAsync(artistId, providerName);
            RelatedArtists = _libraryService.AllArtists
                .Where(item => item.Id != Artist.Id && item.Genres.Intersect(Artist.Genres, StringComparer.OrdinalIgnoreCase).Any())
                .Take(8)
                .ToList();

            var localArtist = await _libraryService.GetArtistAsync(Artist.Id);
            if (localArtist != null)
            {
                Artist.IsFollowed = localArtist.IsFollowed;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            EnsureSelectedPlaylist();
            IsLoading = false;
            NotifySectionStateChanged();
        }
    }

    [RelayCommand]
    private Task PlayAll()
    {
        return TopTracks.Count == 0 ? Task.CompletedTask : _playbackService.PlayAsync(TopTracks[0], TopTracks);
    }

    [RelayCommand]
    private Task Shuffle()
    {
        if (TopTracks.Count == 0)
        {
            return Task.CompletedTask;
        }

        var shuffledTracks = TopTracks.OrderBy(_ => Guid.NewGuid()).ToList();
        return _playbackService.PlayAsync(shuffledTracks[0], shuffledTracks);
    }

    [RelayCommand]
    private Task PlayTrack(Track? track)
    {
        return track == null ? Task.CompletedTask : _playbackService.PlayAsync(track, TopTracks);
    }

    [RelayCommand]
    private void NavigateToAlbum(string? albumId)
    {
        if (!string.IsNullOrWhiteSpace(albumId))
        {
            _navigationService.NavigateToAlbum(albumId, _providerName);
        }
    }

    [RelayCommand]
    private void NavigateToArtist(string? artistId)
    {
        if (!string.IsNullOrWhiteSpace(artistId))
        {
            _navigationService.NavigateToArtist(artistId, _providerName);
        }
    }

    [RelayCommand]
    private void StartWave()
    {
        if (Artist == null)
        {
            return;
        }

        _navigationService.NavigateToMyWave(new WaveSeed
        {
            Type = WaveSeedType.Artist,
            Id = Artist.Id,
            Title = Artist.Name,
            Subtitle = "Wave tuned from this artist."
        });
    }

    [RelayCommand]
    private async Task ToggleFavoriteArtist()
    {
        if (Artist == null)
        {
            return;
        }

        await _libraryService.ToggleFavoriteArtistAsync(Artist.Id);
        Artist.IsFollowed = !Artist.IsFollowed;
        OnPropertyChanged(nameof(IsFavoriteArtist));
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

    private void NotifySectionStateChanged()
    {
        OnPropertyChanged(nameof(HasTopTracks));
        OnPropertyChanged(nameof(HasAlbums));
        OnPropertyChanged(nameof(HasGenres));
        OnPropertyChanged(nameof(HasRelatedArtists));
        OnPropertyChanged(nameof(HasBiography));
        OnPropertyChanged(nameof(IsFavoriteArtist));
        OnPropertyChanged(nameof(AvailablePlaylists));
        OnPropertyChanged(nameof(SourceLabel));
        OnPropertyChanged(nameof(HeaderSummary));
        OnPropertyChanged(nameof(TrackSummary));
        OnPropertyChanged(nameof(ReleaseSummary));
    }

    private void EnsureSelectedPlaylist()
    {
        if (!string.IsNullOrWhiteSpace(SelectedPlaylistId))
        {
            return;
        }

        SelectedPlaylistId = AvailablePlaylists.FirstOrDefault()?.Id ?? string.Empty;
    }
}
