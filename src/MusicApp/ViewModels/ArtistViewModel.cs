using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicApp.Models;
using MusicApp.Providers;
using MusicApp.Services;

namespace MusicApp.ViewModels;

public partial class ArtistViewModel : ObservableObject
{
    private readonly IMusicProviderService _providerService;
    private readonly IPlaybackService _playbackService;
    private readonly INavigationService _navigationService;

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

    public ArtistViewModel(
        IMusicProviderService providerService,
        IPlaybackService playbackService,
        INavigationService navigationService)
    {
        _providerService = providerService;
        _playbackService = playbackService;
        _navigationService = navigationService;
    }

    public async Task LoadArtistAsync(string artistId, string providerName = "Local")
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            // Get artist details
            Artist = await _providerService.GetArtistAsync(artistId, providerName);

            if (Artist != null)
            {
                // Get artist tracks
                TopTracks = await _providerService.GetArtistTracksAsync(artistId, providerName);

                // Get artist albums
                Albums = await _providerService.GetArtistReleasesAsync(artistId, providerName);

                // Generate related artists (mock for now)
                RelatedArtists = new List<Artist>();
            }
            else
            {
                ErrorMessage = "Artist not found";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task PlayAll()
    {
        if (TopTracks.Count > 0)
        {
            await _playbackService.PlayAsync(TopTracks[0], TopTracks);
        }
    }

    [RelayCommand]
    private async Task PlayTrack(Track track)
    {
        await _playbackService.PlayAsync(track, TopTracks);
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
    private async Task ToggleFollow()
    {
        if (Artist != null)
        {
            Artist.IsFollowed = !Artist.IsFollowed;
        }
    }
}
