using CommunityToolkit.Mvvm.ComponentModel;
using Nocturne.App.Models.Enums;

namespace Nocturne.App.Models;

public sealed partial class Album : ObservableObject
{
    [ObservableProperty]
    private bool isSaved;

    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    public TrackSource Source { get; set; } = TrackSource.Unknown;

    public StorageLocation StorageLocation { get; set; } = StorageLocation.Remote;

    public string Title { get; set; } = string.Empty;

    public string ArtistName { get; set; } = string.Empty;

    public string? CoverArtUrl { get; set; }

    public string? HeaderImageUrl { get; set; }

    public string? RemotePageUrl { get; set; }

    public string? ProviderAlbumId { get; set; }

    public DateTimeOffset? ReleaseDate { get; set; }

    public string Description { get; set; } = string.Empty;

    public string Label { get; set; } = "Album";

    public List<string> Genres { get; set; } = [];

    public List<Track> Tracks { get; set; } = [];

    public TimeSpan Duration => TimeSpan.FromSeconds(Tracks.Sum(track => track.Duration.TotalSeconds));

    public int TrackCount => Tracks.Count;

    public string DurationLabel => Duration.TotalHours >= 1
        ? Duration.ToString(@"h\:mm\:ss")
        : Duration.ToString(@"m\:ss");
}
