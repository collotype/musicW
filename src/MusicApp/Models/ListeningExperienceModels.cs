using CommunityToolkit.Mvvm.ComponentModel;
using MusicApp.Enums;

namespace MusicApp.Models;

public class LyricsDocument
{
    public bool IsAvailable { get; set; }
    public bool IsTimed { get; set; }
    public string StatusMessage { get; set; } = "Lyrics are unavailable for this track.";
    public string PlainText { get; set; } = string.Empty;
    public List<LyricLine> Lines { get; set; } = new();
}

public partial class LyricLine : ObservableObject
{
    [ObservableProperty]
    private bool _isActive;

    public TimeSpan? Timestamp { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsSeekable => Timestamp.HasValue;
    public string TimeLabel => Timestamp.HasValue ? $"{(int)Timestamp.Value.TotalMinutes}:{Timestamp.Value.Seconds:D2}" : string.Empty;
}

public class TimedComment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TrackId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = "You";
    public string Text { get; set; } = string.Empty;
    public TimeSpan Timestamp { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsFavoriteMoment { get; set; }

    public string TimeLabel => $"{(int)Timestamp.TotalMinutes}:{Timestamp.Seconds:D2}";
    public string CreatedLabel => CreatedAt.ToLocalTime().ToString("g");
}

public class WaveSeed
{
    public WaveSeedType Type { get; set; } = WaveSeedType.Home;
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = "My Wave";
    public string Subtitle { get; set; } = "Personalized from your listening.";

    public static WaveSeed Home() => new()
    {
        Type = WaveSeedType.Home,
        Title = "My Wave",
        Subtitle = "Blended from your library, likes, and recent plays."
    };
}

public class WaveTunerSettings
{
    public string Activity { get; set; } = "Any";
    public string Mood { get; set; } = "Fluid";
    public string Language { get; set; } = "Any";
    public double Familiarity { get; set; } = 0.55;
    public double Popularity { get; set; } = 0.45;
    public double ArtistVariety { get; set; } = 0.65;
    public double Energy { get; set; } = 0.5;
}

public class WaveRecommendation
{
    public Track Track { get; set; } = new();
    public string Reason { get; set; } = string.Empty;
    public double Score { get; set; }
}

public class SnippetMoment
{
    public Track Track { get; set; } = new();
    public TimeSpan StartTime { get; set; }
    public TimeSpan Length { get; set; } = TimeSpan.FromSeconds(30);
    public string Headline { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string StartLabel => $"{(int)StartTime.TotalMinutes}:{StartTime.Seconds:D2}";
}
