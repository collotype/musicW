namespace MusicApp.Models;

public class QueueItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public Track Track { get; set; } = null!;
    public string? SourceContext { get; set; } // e.g., "Album: XYZ", "Playlist: ABC"
    public string? SourceId { get; set; }
    public DateTime QueuedAt { get; set; } = DateTime.Now;
    public bool IsPlaying { get; set; }
    public bool HasBeenPlayed { get; set; }

    public static QueueItem FromTrack(Track track, string? sourceContext = null, string? sourceId = null)
    {
        return new QueueItem
        {
            Track = track,
            SourceContext = sourceContext,
            SourceId = sourceId
        };
    }
}
