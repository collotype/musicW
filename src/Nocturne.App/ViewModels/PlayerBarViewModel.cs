using CommunityToolkit.Mvvm.Input;
using Nocturne.App.Models;
using Nocturne.App.Models.Enums;
using Nocturne.App.Services;

namespace Nocturne.App.ViewModels;

public sealed class PlayerBarViewModel : ViewModelBase
{
    private readonly IPlaybackService _playbackService;
    private readonly ILibraryService _libraryService;
    private readonly INavigationService _navigationService;
    private readonly IDownloadService _downloadService;

    public PlayerBarViewModel(
        IPlaybackService playbackService,
        ILibraryService libraryService,
        INavigationService navigationService,
        IDownloadService downloadService)
    {
        _playbackService = playbackService;
        _libraryService = libraryService;
        _navigationService = navigationService;
        _downloadService = downloadService;

        PlayPauseCommand = new AsyncRelayCommand(_playbackService.TogglePlayPauseAsync);
        NextCommand = new AsyncRelayCommand(_playbackService.NextAsync);
        PreviousCommand = new AsyncRelayCommand(_playbackService.PreviousAsync);
        ToggleShuffleCommand = new AsyncRelayCommand(_playbackService.ToggleShuffleAsync);
        CycleRepeatCommand = new AsyncRelayCommand(_playbackService.CycleRepeatModeAsync);
        ToggleLikeCommand = new AsyncRelayCommand(ToggleLikeAsync, () => CurrentTrack is not null);
        DownloadCurrentTrackCommand = new AsyncRelayCommand(DownloadCurrentTrackAsync, () => CurrentTrack is not null && CurrentTrack.Source == TrackSource.SoundCloud);
        OpenArtistCommand = new AsyncRelayCommand(OpenArtistAsync, () => CurrentTrack is not null);
        OpenAlbumCommand = new AsyncRelayCommand(OpenAlbumAsync, () => CurrentTrack is not null);

        _playbackService.StateChanged += (_, _) => RefreshState();
    }

    public Track? CurrentTrack => _playbackService.State.CurrentTrack;

    public bool IsPlaying => _playbackService.State.IsPlaying;

    public TimeSpan Position => _playbackService.State.Position;

    public TimeSpan Duration => _playbackService.State.Duration;

    public double Progress
    {
        get => Duration.TotalMilliseconds <= 0 ? 0 : Position.TotalMilliseconds / Duration.TotalMilliseconds;
        set => _ = SeekAsync(value);
    }

    public double Volume
    {
        get => _playbackService.State.Volume;
        set => _ = _playbackService.SetVolumeAsync(value);
    }

    public bool IsShuffleEnabled => _playbackService.State.IsShuffleEnabled;

    public RepeatMode RepeatMode => _playbackService.State.RepeatMode;

    public IAsyncRelayCommand PlayPauseCommand { get; }

    public IAsyncRelayCommand NextCommand { get; }

    public IAsyncRelayCommand PreviousCommand { get; }

    public IAsyncRelayCommand ToggleShuffleCommand { get; }

    public IAsyncRelayCommand CycleRepeatCommand { get; }

    public IAsyncRelayCommand ToggleLikeCommand { get; }

    public IAsyncRelayCommand DownloadCurrentTrackCommand { get; }

    public IAsyncRelayCommand OpenArtistCommand { get; }

    public IAsyncRelayCommand OpenAlbumCommand { get; }

    public Task SeekAsync(double progress)
    {
        return _playbackService.SeekAsync(progress);
    }

    private async Task ToggleLikeAsync()
    {
        if (CurrentTrack is null)
        {
            return;
        }

        await _libraryService.ToggleLikeAsync(CurrentTrack);
        RefreshState();
    }

    private async Task DownloadCurrentTrackAsync()
    {
        if (CurrentTrack is null)
        {
            return;
        }

        await _downloadService.DownloadTrackAsync(CurrentTrack, CancellationToken.None);
        RefreshState();
    }

    private async Task OpenArtistAsync()
    {
        if (CurrentTrack is null)
        {
            return;
        }

        await _navigationService.NavigateAsync<ArtistPageViewModel>(new Artist
        {
            Id = CurrentTrack.ProviderArtistId ?? $"artist-{CurrentTrack.ArtistName}",
            Name = CurrentTrack.ArtistName,
            AvatarUrl = CurrentTrack.ArtistImageUrl,
            Source = CurrentTrack.Source,
            ProviderArtistId = CurrentTrack.ProviderArtistId,
            Genres = CurrentTrack.Genres.ToList()
        });
    }

    private async Task OpenAlbumAsync()
    {
        if (CurrentTrack is null)
        {
            return;
        }

        await _navigationService.NavigateAsync<AlbumPageViewModel>(new Album
        {
            Id = CurrentTrack.ProviderAlbumId ?? $"album-{CurrentTrack.AlbumTitle}",
            Title = CurrentTrack.AlbumTitle ?? "Current Release",
            ArtistName = CurrentTrack.ArtistName,
            CoverArtUrl = CurrentTrack.CoverArtUrl,
            Source = CurrentTrack.Source,
            ProviderAlbumId = CurrentTrack.ProviderAlbumId,
            Tracks = CurrentTrack.AlbumTitle is null
                ? [CurrentTrack]
                : _libraryService.Library.Tracks.Where(track => track.AlbumTitle == CurrentTrack.AlbumTitle).ToList()
        });
    }

    private void RefreshState()
    {
        RaiseAll(nameof(CurrentTrack), nameof(IsPlaying), nameof(Position), nameof(Duration), nameof(Progress), nameof(Volume), nameof(IsShuffleEnabled), nameof(RepeatMode));
        ToggleLikeCommand.NotifyCanExecuteChanged();
        DownloadCurrentTrackCommand.NotifyCanExecuteChanged();
        OpenArtistCommand.NotifyCanExecuteChanged();
        OpenAlbumCommand.NotifyCanExecuteChanged();
    }
}
