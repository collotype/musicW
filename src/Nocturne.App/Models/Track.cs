using CommunityToolkit.Mvvm.ComponentModel;
using Nocturne.App.Models.Enums;

namespace Nocturne.App.Models;

public sealed partial class Track : ObservableObject
{
    [ObservableProperty]
    private bool isLiked;

    [ObservableProperty]
    private bool isDownloaded;

    [ObservableProperty]
    private string? streamUrl;

    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    public TrackSource Source { get; init; } = TrackSource.Local;

    public StorageLocation StorageLocation { get; set; } = StorageLocation.Library;

    public string Title { get; set; } = string.Empty;

    public string ArtistName { get; set; } = string.Empty;

    public string? AlbumTitle { get; set; }

    public TimeSpan Duration { get; set; }

    public string? CoverArtUrl { get; set; }

    public string? ArtistImageUrl { get; set; }

    public string? LocalFilePath { get; set; }

    public string? RemotePageUrl { get; set; }

    public string? ProviderTrackId { get; set; }

    public string? ProviderArtistId { get; set; }

    public string? ProviderAlbumId { get; set; }

    public string? ProviderPlaylistId { get; set; }

    public string? ProviderTrackUrn { get; set; }

    public string? TrackAuthorization { get; set; }

    public IReadOnlyList<PlaybackCandidate> PlaybackCandidates { get; set; } = Array.Empty<PlaybackCandidate>();

    public List<string> Genres { get; set; } = [];

    public List<string> Tags { get; set; } = [];

    public int PlaybackCount { get; set; }

    public DateTimeOffset? ReleaseDate { get; set; }

    public DateTimeOffset AddedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastPlayedAt { get; set; }

    public string Subtitle =>
        string.IsNullOrWhiteSpace(AlbumTitle) ? ArtistName : string.Concat(ArtistName, " - ", AlbumTitle);

    public string SourceLabel =>
        Source switch
        {
            TrackSource.Local => "Local",
            TrackSource.SoundCloud => "SoundCloud",
            TrackSource.Spotify => "Spotify",
            _ => "Unknown"
        };

    public bool IsMetadataOnly => Source == TrackSource.Spotify;

    public bool SupportsInAppPlayback => !IsMetadataOnly;

    public string AvailabilityLabel =>
        IsMetadataOnly
            ? "Metadata only"
            : IsDownloaded || StorageLocation == StorageLocation.Library
                ? "Saved"
                : Source == TrackSource.SoundCloud
                    ? "Streaming"
                    : "Playable";

    public string DisplayDuration => Duration.TotalHours >= 1
        ? Duration.ToString(@"h\:mm\:ss")
        : Duration.ToString(@"m\:ss");
}

public sealed class PlaybackCandidate
{
    public string Url { get; set; } = string.Empty;

    public string Protocol { get; set; } = string.Empty;

    public string MimeType { get; set; } = string.Empty;

    public bool IsProgressive { get; set; }
}
