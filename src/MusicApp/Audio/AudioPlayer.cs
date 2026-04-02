using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using MusicApp.Models;
using System.IO;

namespace MusicApp.Audio;

public class AudioPlayer : IDisposable
{
    private IWavePlayer? _wavePlayer;
    private AudioFileReader? _audioFileReader;
    private MediaFoundationReader? _mediaReader;
    private VolumeSampleProvider? _volumeSampleProvider;
    private float _volume = 0.8f;
    private bool _disposed;

    public event EventHandler<PlaybackPositionEventArgs>? PositionChanged;
    public event EventHandler? PlaybackEnded;
    public event EventHandler<PlaybackErrorEventArgs>? ErrorOccurred;

    public bool IsPlaying => _wavePlayer?.PlaybackState == NAudio.Wave.PlaybackState.Playing;
    public TimeSpan CurrentPosition => _audioFileReader?.CurrentTime ?? _mediaReader?.CurrentTime ?? TimeSpan.Zero;
    public TimeSpan TotalDuration => _audioFileReader?.TotalTime ?? _mediaReader?.TotalTime ?? TimeSpan.Zero;
    public float Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0f, 1f);
            if (_volumeSampleProvider != null)
            {
                _volumeSampleProvider.Volume = _volume;
            }
        }
    }

    public void Play(string filePath)
    {
        Stop();

        try
        {
            var isRemoteSource = Uri.TryCreate(filePath, UriKind.Absolute, out var sourceUri) &&
                                 (sourceUri.Scheme == Uri.UriSchemeHttp || sourceUri.Scheme == Uri.UriSchemeHttps);

            if (!isRemoteSource && !File.Exists(filePath))
            {
                ErrorOccurred?.Invoke(this, new PlaybackErrorEventArgs { Message = "File not found" });
                return;
            }

            var extension = isRemoteSource
                ? string.Empty
                : Path.GetExtension(filePath).ToLowerInvariant();

            if (!isRemoteSource && extension is ".wav" or ".aiff")
            {
                _audioFileReader = new AudioFileReader(filePath);
                _wavePlayer = new WaveOutEvent();
                _volumeSampleProvider = new VolumeSampleProvider(_audioFileReader) { Volume = _volume };
                _wavePlayer.Init(_volumeSampleProvider);
            }
            else
            {
                _mediaReader = new MediaFoundationReader(filePath);
                _wavePlayer = new WaveOutEvent();
                _volumeSampleProvider = new VolumeSampleProvider(_mediaReader.ToSampleProvider()) { Volume = _volume };
                _wavePlayer.Init(_volumeSampleProvider);
            }

            _wavePlayer.PlaybackStopped += OnPlaybackStopped;
            _wavePlayer.Play();

            // Start position polling
            Task.Run(async () => await PollPositionAsync());
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, new PlaybackErrorEventArgs { Message = ex.Message });
        }
    }

    public void Pause()
    {
        _wavePlayer?.Pause();
    }

    public void Resume()
    {
        _wavePlayer?.Play();
    }

    public void Stop()
    {
        if (_wavePlayer != null)
        {
            _wavePlayer.PlaybackStopped -= OnPlaybackStopped;
            _wavePlayer.Stop();
            _wavePlayer.Dispose();
            _wavePlayer = null;
        }

        _audioFileReader?.Dispose();
        _audioFileReader = null;

        _mediaReader?.Dispose();
        _mediaReader = null;

        _volumeSampleProvider = null;
    }

    public void Seek(TimeSpan position)
    {
        if (_audioFileReader != null)
        {
            _audioFileReader.CurrentTime = position;
        }
        else if (_mediaReader != null)
        {
            _mediaReader.CurrentTime = position;
        }
    }

    public void SetVolume(float volume)
    {
        Volume = Math.Clamp(volume, 0f, 1f);
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception != null)
        {
            ErrorOccurred?.Invoke(this, new PlaybackErrorEventArgs { Message = e.Exception.Message });
        }
        else if (CurrentPosition >= TotalDuration - TimeSpan.FromMilliseconds(100))
        {
            PlaybackEnded?.Invoke(this, EventArgs.Empty);
        }
    }

    private async Task PollPositionAsync()
    {
        while (_wavePlayer != null && _wavePlayer.PlaybackState == NAudio.Wave.PlaybackState.Playing)
        {
            PositionChanged?.Invoke(this, new PlaybackPositionEventArgs
            {
                CurrentPosition = CurrentPosition,
                TotalDuration = TotalDuration
            });
            await Task.Delay(250);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        Stop();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

public class PlaybackPositionEventArgs : EventArgs
{
    public TimeSpan CurrentPosition { get; set; }
    public TimeSpan TotalDuration { get; set; }
}

public class PlaybackErrorEventArgs : EventArgs
{
    public string Message { get; set; } = string.Empty;
}
