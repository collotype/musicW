namespace MusicApp.Models;

public class Playlist
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverArtUrl { get; set; }
    public string OwnerName { get; set; } = "You";
    public string? OwnerId { get; set; }
    public List<Track> Tracks { get; set; } = new();
    public bool IsPublic { get; set; } = true;
    public bool IsSystemPlaylist { get; set; } = false;
    public bool IsPinned { get; set; }
    public bool IsDownloaded { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime? LastModifiedDate { get; set; }
    public int TotalTracks => Tracks.Count;
    public TimeSpan TotalDuration => Tracks.Aggregate(TimeSpan.Zero, (acc, t) => acc + t.Duration);
    public string? ProviderPlaylistId { get; set; }

    public string TotalDurationFormatted => FormatDuration(TotalDuration);

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
            return $"{(int)duration.TotalHours} hr {(int)duration.Minutes} min";
        return $"{(int)duration.TotalMinutes} min";
    }
}
