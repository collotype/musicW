using NAudio.Wave;
using Nocturne.App.Models;
using Nocturne.App.Models.Enums;
using System.Windows;
using System.Windows.Threading;
using PlaybackStateModel = Nocturne.App.Models.PlaybackState;

namespace Nocturne.App.Services;

public sealed class PlaybackService : IPlaybackService, IDisposable
{
    private readonly IQueueService _queueService;
    private readonly IOnlineMusicService _onlineMusicService;
    private readonly INotificationService _notificationService;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly DispatcherTimer _positionTimer;

    private IWavePlayer? _outputDevice;
    private MediaFoundationReader? _reader;
    private bool _ignoreStopEvent;
    private bool _disposed;

    public PlaybackService(
        IQueueService queueService,
        IOnlineMusicService onlineMusicService,
        INotificationService notificationService)
    {
        _queueService = queueService;
        _onlineMusicService = onlineMusicService;
        _notificationService = notificationService;

        _positionTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };
        _positionTimer.Tick += OnPositionTimerTick;
    }

    public PlaybackStateModel State { get; } = new();

    public event EventHandler? StateChanged;

    public async Task PlayQueueAsync(IReadOnlyList<Track> tracks, Track? startTrack = null, string origin = "")
    {
        if (tracks.Count == 0)
        {
            return;
        }

        _queueService.ReplaceQueue(tracks, startTrack ?? tracks.First(), origin);
        if (_queueService.CurrentTrack is not null)
        {
            await StartTrackAsync(_queueService.CurrentTrack);
        }
    }

    public async Task PlayTrackAsync(Track track, IReadOnlyList<Track>? queue = null, string origin = "")
    {
        if (queue is not null && queue.Count > 0)
        {
            _queueService.ReplaceQueue(queue, track, origin);
        }
        else if (!_queueService.TrySetCurrent(track))
        {
            _queueService.ReplaceQueue([track], track, origin);
        }

        await StartTrackAsync(track);
    }

    public async Task TogglePlayPauseAsync()
    {
        if (_outputDevice is null && _queueService.CurrentTrack is not null)
        {
            await StartTrackAsync(_queueService.CurrentTrack);
            return;
        }

        if (_outputDevice is null)
        {
            return;
        }

        switch (_outputDevice.PlaybackState)
        {
            case NAudio.Wave.PlaybackState.Playing:
                _outputDevice.Pause();
                State.IsPlaying = false;
                break;
            case NAudio.Wave.PlaybackState.Paused:
                _outputDevice.Play();
                State.IsPlaying = true;
                break;
        }

        NotifyStateChanged();
    }

    public async Task NextAsync()
    {
        if (State.RepeatMode == RepeatMode.One && State.CurrentTrack is not null)
        {
            await StartTrackAsync(State.CurrentTrack);
            return;
        }

        var nextTrack = _queueService.MoveNext(State.IsShuffleEnabled);
        if (nextTrack is null && State.RepeatMode == RepeatMode.All && _queueService.Items.Count > 0)
        {
            nextTrack = _queueService.Items[0].Track;
            _queueService.TrySetCurrent(nextTrack);
        }

        if (nextTrack is not null)
        {
            await StartTrackAsync(nextTrack);
            return;
        }

        State.IsPlaying = false;
        NotifyStateChanged();
    }

    public async Task PreviousAsync()
    {
        if (_reader?.CurrentTime > TimeSpan.FromSeconds(3))
        {
            _reader.CurrentTime = TimeSpan.Zero;
            State.Position = TimeSpan.Zero;
            NotifyStateChanged();
            return;
        }

        var previousTrack = _queueService.MovePrevious();
        if (previousTrack is not null)
        {
            await StartTrackAsync(previousTrack);
        }
    }

    public Task SeekAsync(double progress)
    {
        if (_reader is null)
        {
            return Task.CompletedTask;
        }

        var clamped = Math.Clamp(progress, 0, 1);
        _reader.CurrentTime = TimeSpan.FromMilliseconds(_reader.TotalTime.TotalMilliseconds * clamped);
        State.Position = _reader.CurrentTime;
        NotifyStateChanged();
        return Task.CompletedTask;
    }

    public Task SetVolumeAsync(double volume)
    {
        var clamped = Math.Clamp(volume, 0, 1);
        State.Volume = clamped;

        if (_outputDevice is WaveOutEvent waveOutEvent)
        {
            waveOutEvent.Volume = (float)clamped;
        }

        NotifyStateChanged();
        return Task.CompletedTask;
    }

    public Task ToggleShuffleAsync()
    {
        State.IsShuffleEnabled = !State.IsShuffleEnabled;
        NotifyStateChanged();
        return Task.CompletedTask;
    }

    public Task CycleRepeatModeAsync()
    {
        State.RepeatMode = State.RepeatMode switch
        {
            RepeatMode.Off => RepeatMode.All,
            RepeatMode.All => RepeatMode.One,
            _ => RepeatMode.Off
        };

        NotifyStateChanged();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _positionTimer.Stop();
        _positionTimer.Tick -= OnPositionTimerTick;
        CleanupAudio();
        _gate.Dispose();
    }

    private async Task StartTrackAsync(Track track)
    {
        await _gate.WaitAsync();
        try
        {
            var playbackLocation = await ResolvePlaybackLocationAsync(track);
            if (string.IsNullOrWhiteSpace(playbackLocation))
            {
                State.IsPlaying = false;
                NotifyStateChanged();
                return;
            }

            CleanupAudio();

            _reader = new MediaFoundationReader(playbackLocation);
            _outputDevice = new WaveOutEvent
            {
                Volume = (float)State.Volume
            };
            _outputDevice.PlaybackStopped += OnPlaybackStopped;
            _outputDevice.Init(_reader);
            _outputDevice.Play();

            State.CurrentTrack = track;
            State.Duration = _reader.TotalTime;
            State.Position = TimeSpan.Zero;
            State.IsPlaying = true;
            State.ErrorMessage = null;

            track.LastPlayedAt = DateTimeOffset.UtcNow;
            track.PlaybackCount++;

            _positionTimer.Start();
            NotifyStateChanged();
        }
        catch
        {
            State.ErrorMessage = "Playback failed for the selected track.";
            State.IsPlaying = false;
            await _notificationService.ShowAsync("Playback unavailable", "This track could not be opened by the audio engine.", NotificationLevel.Warning);
            NotifyStateChanged();
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<string?> ResolvePlaybackLocationAsync(Track track)
    {
        if (track.Source == TrackSource.Spotify)
        {
            await _notificationService.ShowAsync("Spotify is metadata only", "Open the track externally or play a local or SoundCloud result instead.", NotificationLevel.Info);
            return null;
        }

        if (!string.IsNullOrWhiteSpace(track.LocalFilePath))
        {
            if (File.Exists(track.LocalFilePath))
            {
                return track.LocalFilePath;
            }

            await _notificationService.ShowAsync("File missing", "The local file for this track is no longer available.", NotificationLevel.Warning);
            return null;
        }

        if (track.Source == TrackSource.SoundCloud && string.IsNullOrWhiteSpace(track.StreamUrl))
        {
            var resolved = await _onlineMusicService.ResolvePlaybackAsync(track, CancellationToken.None);
            if (resolved is not null)
            {
                track.StreamUrl = resolved.StreamUrl;
            }
        }

        if (string.IsNullOrWhiteSpace(track.StreamUrl))
        {
            await _notificationService.ShowAsync("Stream unavailable", "No playable stream could be resolved for this track.", NotificationLevel.Warning);
            return null;
        }

        return track.StreamUrl;
    }

    private async void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        if (_ignoreStopEvent || _disposed)
        {
            return;
        }

        State.IsPlaying = false;
        NotifyStateChanged();

        if (e.Exception is null && _reader is not null && _reader.CurrentTime >= _reader.TotalTime.Subtract(TimeSpan.FromMilliseconds(400)))
        {
            await NextAsync();
        }
    }

    private void OnPositionTimerTick(object? sender, EventArgs e)
    {
        if (_reader is null)
        {
            return;
        }

        State.Position = _reader.CurrentTime;
        State.Duration = _reader.TotalTime;
        NotifyStateChanged();
    }

    private void CleanupAudio()
    {
        _ignoreStopEvent = true;
        _positionTimer.Stop();

        if (_outputDevice is not null)
        {
            _outputDevice.PlaybackStopped -= OnPlaybackStopped;
            _outputDevice.Stop();
            _outputDevice.Dispose();
            _outputDevice = null;
        }

        _reader?.Dispose();
        _reader = null;
        _ignoreStopEvent = false;
    }

    private void NotifyStateChanged()
    {
        if (Application.Current?.Dispatcher is { } dispatcher && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(() => StateChanged?.Invoke(this, EventArgs.Empty));
            return;
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
