using MusicApp.Enums;

namespace MusicApp.Models;

public class PlaybackState
{
    public PlaybackStatus Status { get; set; } = PlaybackStatus.Stopped;
    public QueueItem? CurrentTrack { get; set; }
    public TimeSpan CurrentPosition { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public double Volume { get; set; } = 0.8;
    public bool IsMuted { get; set; }
    public RepeatMode RepeatMode { get; set; } = RepeatMode.None;
    public bool IsShuffle { get; set; }
    public bool IsLoading { get; set; }
    public string? ErrorMessage { get; set; }

    public double ProgressPercent => TotalDuration.TotalSeconds > 0
        ? CurrentPosition.TotalSeconds / TotalDuration.TotalSeconds * 100
        : 0;

    public string CurrentPositionFormatted => FormatTime(CurrentPosition);
    public string TotalDurationFormatted => FormatTime(TotalDuration);

    private static string FormatTime(TimeSpan time)
    {
        if (time.TotalHours >= 1)
            return $"{(int)time.TotalHours}:{time.Minutes:D2}:{time.Seconds:D2}";
        return $"{(int)time.TotalMinutes}:{time.Seconds:D2}";
    }
}
