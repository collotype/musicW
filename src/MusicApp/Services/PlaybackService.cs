using MusicApp.Audio;
using MusicApp.Enums;
using MusicApp.Models;

namespace MusicApp.Services;

public class PlaybackService : IPlaybackService
{
    private readonly AudioPlayer _audioPlayer;
    private readonly IQueueService _queueService;
    private readonly IMusicProviderService _providerService;
    private readonly ILibraryService _libraryService;
    private readonly ISettingsService _settingsService;
    private readonly IRecommendationService _recommendationService;
    private readonly PlaybackState _currentState = new();

    private RepeatMode _repeatMode = RepeatMode.None;
    private bool _isShuffle;
    private bool _disposed;

    public event EventHandler<PlaybackState>? StateChanged;

    public PlaybackState CurrentState => _currentState;

    public PlaybackService(
        AudioPlayer audioPlayer,
        IQueueService queueService,
        IMusicProviderService providerService,
        ILibraryService libraryService,
        ISettingsService settingsService,
        IRecommendationService recommendationService)
    {
        _audioPlayer = audioPlayer;
        _queueService = queueService;
        _providerService = providerService;
        _libraryService = libraryService;
        _settingsService = settingsService;
        _recommendationService = recommendationService;

        _currentState.Volume = _settingsService.Settings.Volume;
        _currentState.IsMuted = _settingsService.Settings.IsMuted;
        _audioPlayer.SetVolume((float)_currentState.Volume);
        _queueService.SmartQueueEnabled = _settingsService.Settings.SmartQueueEnabled;

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

            var queueItems = queue?.Select(item => QueueItem.FromTrack(item)).ToList()
                ?? new List<QueueItem> { QueueItem.FromTrack(track) };
            var startIndex = queueItems.FindIndex(item => IsSameTrack(item.Track, track));
            if (startIndex < 0)
            {
                startIndex = 0;
            }

            _queueService.SetQueue(queueItems, startIndex);
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
        _ = UpdateTrackProgressAsync(_currentState.CurrentPosition);
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
            await PlayCurrentAsync();
        }
    }

    public async Task StopAsync()
    {
        _audioPlayer.Stop();
        await UpdateTrackProgressAsync(_currentState.CurrentPosition);
        _currentState.Status = PlaybackStatus.Stopped;
        _currentState.CurrentPosition = TimeSpan.Zero;
        _currentState.TotalDuration = TimeSpan.Zero;
        OnStateChanged();
    }

    public async Task NextAsync()
    {
        if (_repeatMode == RepeatMode.One)
        {
            await PlayCurrentAsync();
            return;
        }

        if (_queueService.CurrentIndex < _queueService.Queue.Count - 1)
        {
            _queueService.MoveToNext();
            await PlayCurrentAsync();
            return;
        }

        if (_repeatMode == RepeatMode.All && _queueService.Queue.Count > 0)
        {
            _queueService.SetCurrentIndex(0);
            await PlayCurrentAsync();
            return;
        }

        if (_queueService.SmartQueueEnabled)
        {
            var recommendations = _recommendationService.GetSmartQueueTracks(
                _queueService.CurrentItem?.Track,
                excludeTrackIds: _queueService.Queue.Select(item => item.Track.Id));

            if (recommendations.Count > 0)
            {
                _queueService.AppendRecommendations(recommendations);
                _queueService.MoveToNext();
                await PlayCurrentAsync();
                return;
            }
        }

        await StopAsync();
    }

    public async Task PreviousAsync()
    {
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

    public async Task SeekAsync(TimeSpan position)
    {
        _audioPlayer.Seek(position);
        _currentState.CurrentPosition = position;
        await UpdateTrackProgressAsync(position);
        OnStateChanged();
    }

    public Task SetVolumeAsync(double volume)
    {
        var normalizedVolume = Math.Clamp(volume, 0d, 1d);
        _audioPlayer.SetVolume((float)normalizedVolume);
        _currentState.Volume = normalizedVolume;
        _currentState.IsMuted = normalizedVolume <= 0;
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
        if (currentTrack == null)
        {
            return;
        }

        if (_libraryService.AllTracks.All(existing => !IsSameTrack(existing, currentTrack)))
        {
            await _libraryService.AddTrackAsync(currentTrack);
        }

        await _libraryService.ToggleLikeAsync(currentTrack.Id);
        currentTrack.IsLiked = await _libraryService.IsLikedAsync(currentTrack.Id);
        OnStateChanged();
    }

    private async Task PlayCurrentAsync()
    {
        var currentItem = _queueService.CurrentItem;
        if (currentItem == null)
        {
            return;
        }

        var track = currentItem.Track;
        _currentState.CurrentTrack = currentItem;
        _currentState.Status = PlaybackStatus.Playing;
        _currentState.IsLoading = true;
        _currentState.ErrorMessage = null;
        OnStateChanged();

        try
        {
            var playbackSource = track.Source == TrackSource.Local && !string.IsNullOrEmpty(track.LocalFilePath)
                ? track.LocalFilePath
                : await _providerService.ResolvePlaybackUrlAsync(track, CancellationToken.None);

            if (string.IsNullOrEmpty(playbackSource))
            {
                throw new Exception("Playback is not available for this track yet.");
            }

            _audioPlayer.Play(playbackSource);
            _audioPlayer.SetVolume(_currentState.IsMuted ? 0f : (float)_currentState.Volume);

            _currentState.IsLoading = false;
            _currentState.TotalDuration = _audioPlayer.TotalDuration;
            _currentState.CurrentPosition = TimeSpan.Zero;
            await UpdateTrackPlayStateAsync(track, TimeSpan.Zero);
            OnStateChanged();
        }
        catch (Exception ex)
        {
            _currentState.IsLoading = false;
            _currentState.ErrorMessage = ex.Message;
            OnStateChanged();
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

    private async Task UpdateTrackPlayStateAsync(Track track, TimeSpan position)
    {
        var libraryTrack = _libraryService.AllTracks.FirstOrDefault(existing => IsSameTrack(existing, track));
        if (libraryTrack == null)
        {
            track.LastPlayedAt = DateTime.UtcNow;
            track.LastPlaybackPosition = position;
            return;
        }

        libraryTrack.PlayCount = (libraryTrack.PlayCount ?? 0) + 1;
        libraryTrack.LastPlayedAt = DateTime.UtcNow;
        libraryTrack.LastPlaybackPosition = position;

        track.PlayCount = libraryTrack.PlayCount;
        track.LastPlayedAt = libraryTrack.LastPlayedAt;
        track.LastPlaybackPosition = libraryTrack.LastPlaybackPosition;

        await _libraryService.AddTrackAsync(libraryTrack);
    }

    private async Task UpdateTrackProgressAsync(TimeSpan position)
    {
        var currentTrack = _queueService.CurrentItem?.Track;
        if (currentTrack == null)
        {
            return;
        }

        var libraryTrack = _libraryService.AllTracks.FirstOrDefault(existing => IsSameTrack(existing, currentTrack));
        if (libraryTrack == null)
        {
            currentTrack.LastPlaybackPosition = position;
            return;
        }

        libraryTrack.LastPlaybackPosition = position;
        currentTrack.LastPlaybackPosition = position;
        await _libraryService.AddTrackAsync(libraryTrack);
    }

    private void OnStateChanged()
    {
        StateChanged?.Invoke(this, _currentState);
    }

    private static bool IsSameTrack(Track left, Track right)
    {
        return left.Id == right.Id ||
               (!string.IsNullOrWhiteSpace(left.LocalFilePath) &&
                !string.IsNullOrWhiteSpace(right.LocalFilePath) &&
                string.Equals(left.LocalFilePath, right.LocalFilePath, StringComparison.OrdinalIgnoreCase)) ||
               (!string.IsNullOrWhiteSpace(left.ProviderTrackId) &&
                !string.IsNullOrWhiteSpace(right.ProviderTrackId) &&
                left.Source == right.Source &&
                string.Equals(left.ProviderTrackId, right.ProviderTrackId, StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _audioPlayer.PositionChanged -= OnPositionChanged;
        _audioPlayer.PlaybackEnded -= OnPlaybackEnded;
        _audioPlayer.ErrorOccurred -= OnErrorOccurred;
        _audioPlayer.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
