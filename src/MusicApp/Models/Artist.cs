namespace MusicApp.Models;

public class Artist
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string? Biography { get; set; }
    public string? ImageUrl { get; set; }
    public string? BannerUrl { get; set; }
    public string? Country { get; set; }
    public long? Followers { get; set; }
    public long? MonthlyListeners { get; set; }
    public int TrackCount { get; set; }
    public int AlbumCount { get; set; }
    public List<string> Genres { get; set; } = new();
    public List<string> SocialLinks { get; set; } = new();
    public bool IsFollowed { get; set; }
    public DateTime? FormedYear { get; set; }
    public List<Track> TopTracks { get; set; } = new();
    public List<Album> Albums { get; set; } = new();
    public List<Artist> RelatedArtists { get; set; } = new();

    public string FollowersFormatted => Followers.HasValue ? FormatNumber(Followers.Value) : "—";
    public string MonthlyListenersFormatted => MonthlyListeners.HasValue ? FormatNumber(MonthlyListeners.Value) : "—";

    private static string FormatNumber(long num)
    {
        if (num >= 1_000_000)
            return $"{num / 1_000_000.0:F1}M";
        if (num >= 1_000)
            return $"{num / 1_000.0:F1}K";
        return num.ToString();
    }
}
