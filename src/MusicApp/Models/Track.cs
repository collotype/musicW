using MusicApp.Enums;

namespace MusicApp.Models;

public class Track
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public TrackSource Source { get; set; } = TrackSource.Unknown;
    public StorageLocation StorageLocation { get; set; } = StorageLocation.Remote;
    public string Title { get; set; } = string.Empty;
    public string ArtistName { get; set; } = string.Empty;
    public string ArtistId { get; set; } = string.Empty;
    public string AlbumTitle { get; set; } = string.Empty;
    public string AlbumId { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public string? CoverArtUrl { get; set; }
    public string? LocalFilePath { get; set; }
    public string? RemotePageUrl { get; set; }
    public string? StreamUrl { get; set; }
    public string? ProviderTrackId { get; set; }
    public bool IsLiked { get; set; }
    public bool IsDownloaded { get; set; }
    public string? ArtistImageUrl { get; set; }
    public List<string> Genres { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public DateTime? ReleaseDate { get; set; }
    public int TrackNumber { get; set; }
    public int DiscNumber { get; set; } = 1;
    public long? PlayCount { get; set; }
    public string? Lyrics { get; set; }

    public string DurationFormatted => Duration.ToString(@"m\:ss");

    public Track Clone()
    {
        return new Track
        {
            Id = Id,
            Source = Source,
            StorageLocation = StorageLocation,
            Title = Title,
            ArtistName = ArtistName,
            ArtistId = ArtistId,
            AlbumTitle = AlbumTitle,
            AlbumId = AlbumId,
            Duration = Duration,
            CoverArtUrl = CoverArtUrl,
            LocalFilePath = LocalFilePath,
            RemotePageUrl = RemotePageUrl,
            StreamUrl = StreamUrl,
            ProviderTrackId = ProviderTrackId,
            IsLiked = IsLiked,
            IsDownloaded = IsDownloaded,
            ArtistImageUrl = ArtistImageUrl,
            Genres = new List<string>(Genres),
            Tags = new List<string>(Tags),
            ReleaseDate = ReleaseDate,
            TrackNumber = TrackNumber,
            DiscNumber = DiscNumber,
            PlayCount = PlayCount,
            Lyrics = Lyrics
        };
    }
}
