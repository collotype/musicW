using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicApp.Enums;
using MusicApp.Models;
using MusicApp.Services;

namespace MusicApp.ViewModels;

public partial class NowPlayingViewModel : ObservableObject
{
    private readonly IPlaybackService _playbackService;
    private readonly ILibraryService _libraryService;

    [ObservableProperty]
    private string _title = "Not Playing";

    [ObservableProperty]
    private string _artist = "—";

    [ObservableProperty]
    private string _coverArtUrl = string.Empty;

    [ObservableProperty]
    private TimeSpan _currentPosition;

    [ObservableProperty]
    private TimeSpan _totalDuration;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private bool _isLiked;

    [ObservableProperty]
    private double _volume = 0.8;

    [ObservableProperty]
    private bool _isMuted;

    [ObservableProperty]
    private RepeatMode _repeatMode;

    [ObservableProperty]
    private bool _isShuffle;

    [ObservableProperty]
    private string _currentPositionFormatted = "0:00";

    [ObservableProperty]
    private string _totalDurationFormatted = "0:00";

    public NowPlayingViewModel(IPlaybackService playbackService, ILibraryService libraryService)
    {
        _playbackService = playbackService;
        _libraryService = libraryService;

        _playbackService.StateChanged += OnPlaybackStateChanged;
    }

    private void OnPlaybackStateChanged(object? sender, PlaybackState state)
    {
        CurrentPosition = state.CurrentPosition;
        TotalDuration = state.TotalDuration;
        Progress = state.ProgressPercent;
        IsPlaying = state.Status == PlaybackStatus.Playing;
        Volume = state.Volume;
        IsMuted = state.IsMuted;
        RepeatMode = state.RepeatMode;
        IsShuffle = state.IsShuffle;
        CurrentPositionFormatted = state.CurrentPositionFormatted;
        TotalDurationFormatted = state.TotalDurationFormatted;

        if (state.CurrentTrack?.Track != null)
        {
            var track = state.CurrentTrack.Track;
            Title = track.Title;
            Artist = track.ArtistName;
            CoverArtUrl = track.CoverArtUrl ?? string.Empty;
            IsLiked = track.IsLiked;
        }
        else
        {
            Title = "Not Playing";
            Artist = "—";
            CoverArtUrl = string.Empty;
            IsLiked = false;
        }
    }

    [RelayCommand]
    private async Task TogglePlayPause()
    {
        await _playbackService.TogglePlayPauseAsync();
    }

    [RelayCommand]
    private async Task Next()
    {
        await _playbackService.NextAsync();
    }

    [RelayCommand]
    private async Task Previous()
    {
        await _playbackService.PreviousAsync();
    }

    [RelayCommand]
    private async Task ToggleLike()
    {
        await _playbackService.LikeCurrentTrackAsync();
    }

    [RelayCommand]
    private async Task SetVolume(double volume)
    {
        await _playbackService.SetVolumeAsync(volume);
    }

    [RelayCommand]
    private async Task ToggleMute()
    {
        await _playbackService.ToggleMuteAsync();
    }

    [RelayCommand]
    private async Task SetRepeatMode(int mode)
    {
        await _playbackService.SetRepeatModeAsync((RepeatMode)mode);
    }

    [RelayCommand]
    private async Task ToggleShuffle()
    {
        await _playbackService.ToggleShuffleAsync();
    }

    [RelayCommand]
    private async Task Seek(double position)
    {
        if (TotalDuration.TotalSeconds > 0)
        {
            var timePosition = TimeSpan.FromSeconds(position / 100 * TotalDuration.TotalSeconds);
            await _playbackService.SeekAsync(timePosition);
        }
    }
}
