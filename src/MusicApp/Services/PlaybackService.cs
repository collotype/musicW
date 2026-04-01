using MusicApp.Audio;
using MusicApp.Enums;
using MusicApp.Models;
using MusicApp.Providers;
using System.Net.Http;
using System.IO;

namespace MusicApp.Services;

public class PlaybackService : IPlaybackService
{
    private readonly AudioPlayer _audioPlayer;
    private readonly IQueueService _queueService;
    private readonly IMusicProviderService _providerService;
    private readonly ILibraryService _libraryService;
    private PlaybackState _currentState = new();
    private RepeatMode _repeatMode = RepeatMode.None;
    private bool _isShuffle;
    private bool _disposed;

    public event EventHandler<PlaybackState>? StateChanged;

    public PlaybackState CurrentState => _currentState;

    public PlaybackService(
        AudioPlayer audioPlayer,
        IQueueService queueService,
        IMusicProviderService providerService,
        ILibraryService libraryService)
    {
        _audioPlayer = audioPlayer;
        _queueService = queueService;
        _providerService = providerService;
        _libraryService = libraryService;

        _audioPlayer.PositionChanged += OnPositionChanged;
        _audioPlayer.PlaybackEnded += OnPlaybackEnded;
        _audioPlayer.ErrorOccurred += OnErrorOccurred;
    }

    public async Task PlayAsync(Track track, List<Track>? queue = null)
    {
        try
        {
            _currentState.IsLoading = true;
            _currentState.ErrorMessage = null;
            OnStateChanged();

            var queueItems = queue?.Select(t => QueueItem.FromTrack(t)).ToList()
                ?? new List<QueueItem> { QueueItem.FromTrack(track) };

            _queueService.SetQueue(queueItems, 0);

            await PlayCurrentAsync();
        }
        catch (Exception ex)
        {
            _currentState.IsLoading = false;
            _currentState.ErrorMessage = ex.Message;
            OnStateChanged();
        }
    }

    public async Task PlayQueueAsync(List<QueueItem> queue, int startIndex = 0)
    {
        try
        {
            _currentState.IsLoading = true;
            _currentState.ErrorMessage = null;
            OnStateChanged();

            _queueService.SetQueue(queue, startIndex);
            await PlayCurrentAsync();
        }
        catch (Exception ex)
        {
            _currentState.IsLoading = false;
            _currentState.ErrorMessage = ex.Message;
            OnStateChanged();
        }
    }

    public Task PauseAsync()
    {
        _audioPlayer.Pause();
        _currentState.Status = PlaybackStatus.Paused;
        OnStateChanged();
        return Task.CompletedTask;
    }

    public Task ResumeAsync()
    {
        _audioPlayer.Resume();
        _currentState.Status = PlaybackStatus.Playing;
        OnStateChanged();
        return Task.CompletedTask;
    }

    public async Task TogglePlayPauseAsync()
    {
        if (_currentState.Status == PlaybackStatus.Playing)
        {
            await PauseAsync();
        }
        else if (_currentState.Status == PlaybackStatus.Paused)
        {
            await ResumeAsync();
        }
        else if (_queueService.CurrentItem != null)
        {
            await ResumeAsync();
        }
    }

    public Task StopAsync()
    {
        _audioPlayer.Stop();
        _currentState.Status = PlaybackStatus.Stopped;
        _currentState.CurrentPosition = TimeSpan.Zero;
        _currentState.TotalDuration = TimeSpan.Zero;
        OnStateChanged();
        return Task.CompletedTask;
    }

    public async Task NextAsync()
    {
        if (_repeatMode == RepeatMode.One)
        {
            await PlayCurrentAsync();
            return;
        }

        _queueService.MoveToNext();

        if (_queueService.CurrentItem != null)
        {
            await PlayCurrentAsync();
        }
        else if (_repeatMode == RepeatMode.All && _queueService.Queue.Count > 0)
        {
            _queueService.SetQueue(_queueService.Queue, 0);
            await PlayCurrentAsync();
        }
        else
        {
            await StopAsync();
        }
    }

    public async Task PreviousAsync()
    {
        // If more than 3 seconds in, restart current track
        if (_currentState.CurrentPosition.TotalSeconds > 3)
        {
            await SeekAsync(TimeSpan.Zero);
            return;
        }

        _queueService.MoveToPrevious();

        if (_queueService.CurrentItem != null)
        {
            await PlayCurrentAsync();
        }
    }

    public Task SeekAsync(TimeSpan position)
    {
        _audioPlayer.Seek(position);
        _currentState.CurrentPosition = position;
        OnStateChanged();
        return Task.CompletedTask;
    }

    public Task SetVolumeAsync(double volume)
    {
        _audioPlayer.SetVolume((float)volume);
        _currentState.Volume = volume;
        OnStateChanged();
        return Task.CompletedTask;
    }

    public Task ToggleMuteAsync()
    {
        _currentState.IsMuted = !_currentState.IsMuted;
        _audioPlayer.SetVolume(_currentState.IsMuted ? 0f : (float)_currentState.Volume);
        OnStateChanged();
        return Task.CompletedTask;
    }

    public Task SetRepeatModeAsync(RepeatMode mode)
    {
        _repeatMode = mode;
        _currentState.RepeatMode = mode;
        OnStateChanged();
        return Task.CompletedTask;
    }

    public Task ToggleShuffleAsync()
    {
        _isShuffle = !_isShuffle;
        _currentState.IsShuffle = _isShuffle;

        if (_isShuffle)
        {
            _queueService.Shuffle();
        }

        OnStateChanged();
        return Task.CompletedTask;
    }

    public async Task LikeCurrentTrackAsync()
    {
        var currentTrack = _queueService.CurrentItem?.Track;
        if (currentTrack != null)
        {
            currentTrack.IsLiked = !currentTrack.IsLiked;
            await _libraryService.ToggleLikeAsync(currentTrack.Id);
            OnStateChanged();
        }
    }

    private async Task PlayCurrentAsync()
    {
        var currentItem = _queueService.CurrentItem;
        if (currentItem == null) return;

        var track = currentItem.Track;
        _currentState.CurrentTrack = currentItem;
        _currentState.Status = PlaybackStatus.Playing;
        _currentState.IsLoading = true;
        OnStateChanged();

        try
        {
            string? playbackUrl = null;

            if (track.Source == TrackSource.Local && !string.IsNullOrEmpty(track.LocalFilePath))
            {
                playbackUrl = track.LocalFilePath;
            }
            else
            {
                // Try to resolve stream URL from provider
                playbackUrl = await _providerService.ResolvePlaybackUrlAsync(track, CancellationToken.None);
            }

            if (string.IsNullOrEmpty(playbackUrl))
            {
                throw new Exception("Could not resolve playback URL");
            }

            if (track.Source == TrackSource.Local)
            {
                _audioPlayer.Play(playbackUrl);
            }
            else
            {
                // For remote streams, download to temp and play
                var tempPath = Path.Combine(Path.GetTempPath(), $"music_{track.Id}.tmp");
                using var client = new HttpClient();
                var data = await client.GetByteArrayAsync(playbackUrl);
                await File.WriteAllBytesAsync(tempPath, data);
                _audioPlayer.Play(tempPath);
            }

            _currentState.IsLoading = false;
            _currentState.TotalDuration = _audioPlayer.TotalDuration;
            _currentState.CurrentPosition = TimeSpan.Zero;
            OnStateChanged();
        }
        catch (Exception ex)
        {
            _currentState.IsLoading = false;
            _currentState.ErrorMessage = ex.Message;
            OnStateChanged();

            // Auto-skip to next track on error
            await NextAsync();
        }
    }

    private void OnPositionChanged(object? sender, PlaybackPositionEventArgs e)
    {
        _currentState.CurrentPosition = e.CurrentPosition;
        _currentState.TotalDuration = e.TotalDuration;
        OnStateChanged();
    }

    private void OnPlaybackEnded(object? sender, EventArgs e)
    {
        Task.Run(async () => await NextAsync());
    }

    private void OnErrorOccurred(object? sender, PlaybackErrorEventArgs e)
    {
        _currentState.ErrorMessage = e.Message;
        _currentState.IsLoading = false;
        OnStateChanged();
    }

    private void OnStateChanged()
    {
        StateChanged?.Invoke(this, _currentState);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _audioPlayer.PositionChanged -= OnPositionChanged;
        _audioPlayer.PlaybackEnded -= OnPlaybackEnded;
        _audioPlayer.ErrorOccurred -= OnErrorOccurred;
        _audioPlayer.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
