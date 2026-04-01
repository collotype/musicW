using Nocturne.App.Models.Enums;

namespace Nocturne.App.Models;

public sealed class PlaybackState
{
    public Track? CurrentTrack { get; set; }

    public bool IsPlaying { get; set; }

    public TimeSpan Position { get; set; }

    public TimeSpan Duration { get; set; }

    public double Volume { get; set; } = 0.72;

    public bool IsShuffleEnabled { get; set; }

    public RepeatMode RepeatMode { get; set; } = RepeatMode.Off;

    public string? ErrorMessage { get; set; }

    public string PositionLabel => Position.TotalHours >= 1
        ? Position.ToString(@"h\:mm\:ss")
        : Position.ToString(@"m\:ss");

    public string DurationLabel => Duration.TotalHours >= 1
        ? Duration.ToString(@"h\:mm\:ss")
        : Duration.ToString(@"m\:ss");
}
