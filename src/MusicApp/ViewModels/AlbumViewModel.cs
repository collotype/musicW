using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicApp.Models;
using MusicApp.Providers;
using MusicApp.Services;

namespace MusicApp.ViewModels;

public partial class AlbumViewModel : ObservableObject
{
    private readonly IMusicProviderService _providerService;
    private readonly IPlaybackService _playbackService;
    private readonly INavigationService _navigationService;
    private readonly ILibraryService _libraryService;

    [ObservableProperty]
    private Album? _album;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private Track? _playingTrack;

    public bool HasTracks => Album?.Tracks.Count > 0;

    public AlbumViewModel(
        IMusicProviderService providerService,
        IPlaybackService playbackService,
        INavigationService navigationService,
        ILibraryService libraryService)
    {
        _providerService = providerService;
        _playbackService = playbackService;
        _navigationService = navigationService;
        _libraryService = libraryService;
    }

    public async Task LoadAlbumAsync(string albumId, string providerName = "Local")
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            Album = await _providerService.GetAlbumAsync(albumId, providerName);

            if (Album == null)
            {
                ErrorMessage = "Album not found";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasTracks));
        }
    }

    [RelayCommand]
    private async Task PlayAll()
    {
        if (Album?.Tracks.Count > 0)
        {
            await _playbackService.PlayAsync(Album.Tracks[0], Album.Tracks);
        }
    }

    [RelayCommand]
    private async Task Shuffle()
    {
        if (Album?.Tracks.Count > 0)
        {
            var shuffledTracks = Album.Tracks.OrderBy(_ => Guid.NewGuid()).ToList();
            await _playbackService.PlayAsync(shuffledTracks[0], shuffledTracks);
        }
    }

    [RelayCommand]
    private async Task PlayTrack(Track track)
    {
        await _playbackService.PlayAsync(track, Album?.Tracks);
        PlayingTrack = track;
    }

    [RelayCommand]
    private async Task AddToLibrary()
    {
        if (Album?.Tracks != null)
        {
            foreach (var track in Album.Tracks)
            {
                await _libraryService.AddTrackAsync(track);
            }
        }
    }

    [RelayCommand]
    private void NavigateToArtist(string artistId)
    {
        _navigationService.NavigateToArtist(artistId);
    }
}
