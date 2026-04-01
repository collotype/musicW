using Nocturne.App.Models;
using Nocturne.App.Models.Enums;

namespace Nocturne.App.Providers;

public interface IMusicProvider
{
    TrackSource Source { get; }

    bool SupportsPlayback { get; }

    Task<SearchResults> SearchAsync(string query, CancellationToken cancellationToken);

    Task<Artist?> GetArtistAsync(string providerArtistId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Track>> GetArtistTracksAsync(string providerArtistId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Album>> GetArtistReleasesAsync(string providerArtistId, CancellationToken cancellationToken);

    Task<Album?> GetAlbumAsync(string providerAlbumId, CancellationToken cancellationToken);

    Task<Playlist?> GetPlaylistAsync(string providerPlaylistId, CancellationToken cancellationToken);

    Task<ResolvedPlaybackStream?> ResolvePlaybackAsync(Track track, CancellationToken cancellationToken);

    Task<string?> DownloadTrackAsync(Track track, string destinationFolder, CancellationToken cancellationToken);
}
