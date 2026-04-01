using CommunityToolkit.Mvvm.ComponentModel;
using Nocturne.App.Models.Enums;

namespace Nocturne.App.Models;

public sealed partial class Artist : ObservableObject
{
    [ObservableProperty]
    private bool isFollowed;

    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    public TrackSource Source { get; set; } = TrackSource.Unknown;

    public string Name { get; set; } = string.Empty;

    public string? Subtitle { get; set; }

    public string? AvatarUrl { get; set; }

    public string? HeaderImageUrl { get; set; }

    public string? Country { get; set; }

    public string? ProviderArtistId { get; set; }

    public string? RemotePageUrl { get; set; }

    public long Followers { get; set; }

    public long MonthlyListeners { get; set; }

    public int TrackCount { get; set; }

    public string About { get; set; } = string.Empty;

    public List<string> Genres { get; set; } = [];

    public List<Track> TopTracks { get; set; } = [];

    public List<Album> Albums { get; set; } = [];

    public List<Artist> RelatedArtists { get; set; } = [];
}
