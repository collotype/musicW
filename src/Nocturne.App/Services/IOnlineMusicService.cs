using Nocturne.App.Models;
using Nocturne.App.Models.Enums;

namespace Nocturne.App.Services;

public interface IOnlineMusicService
{
    Task<SearchResults> SearchAsync(string query, SearchSourceFilter sourceFilter, CancellationToken cancellationToken);

    Task<Artist?> GetArtistAsync(TrackSource source, string providerArtistId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Track>> GetArtistTracksAsync(TrackSource source, string providerArtistId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Album>> GetArtistReleasesAsync(TrackSource source, string providerArtistId, CancellationToken cancellationToken);

    Task<Album?> GetAlbumAsync(TrackSource source, string providerAlbumId, CancellationToken cancellationToken);

    Task<Playlist?> GetPlaylistAsync(TrackSource source, string providerPlaylistId, CancellationToken cancellationToken);

    Task<ResolvedPlaybackStream?> ResolvePlaybackAsync(Track track, CancellationToken cancellationToken);

    Task<string?> DownloadTrackAsync(Track track, CancellationToken cancellationToken);
}
