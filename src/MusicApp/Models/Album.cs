namespace MusicApp.Models;

public class Album
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string ArtistName { get; set; } = string.Empty;
    public string ArtistId { get; set; } = string.Empty;
    public string? CoverArtUrl { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public string? Label { get; set; }
    public string AlbumType { get; set; } = "Album"; // Album, EP, Single
    public List<Track> Tracks { get; set; } = new();
    public List<string> Genres { get; set; } = new();
    public bool IsLiked { get; set; }
    public int TotalTracks => Tracks.Count;
    public TimeSpan TotalDuration => Tracks.Aggregate(TimeSpan.Zero, (acc, t) => acc + t.Duration);
    public string? ProviderAlbumId { get; set; }

    public string ReleaseYear => ReleaseDate?.Year.ToString() ?? "—";
    public string TotalDurationFormatted => FormatDuration(TotalDuration);

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
            return $"{(int)duration.TotalHours}:{duration.Minutes:D2}:{duration.Seconds:D2}";
        return $"{(int)duration.TotalMinutes}:{duration.Seconds:D2}";
    }
}
