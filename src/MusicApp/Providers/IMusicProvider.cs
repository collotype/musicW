using MusicApp.Models;

namespace MusicApp.Providers;

public interface IMusicProvider
{
    string ProviderName { get; }
    bool IsAvailable { get; }
    bool SupportsPlayback { get; }
    bool SupportsSearch { get; }

    Task<SearchResults> SearchAsync(string query, CancellationToken cancellationToken = default);
    Task<Artist?> GetArtistAsync(string artistId, CancellationToken cancellationToken = default);
    Task<List<Track>> GetArtistTracksAsync(string artistId, CancellationToken cancellationToken = default);
    Task<List<Album>> GetArtistReleasesAsync(string artistId, CancellationToken cancellationToken = default);
    Task<Album?> GetAlbumAsync(string albumId, CancellationToken cancellationToken = default);
    Task<Playlist?> GetPlaylistAsync(string playlistId, CancellationToken cancellationToken = default);
    Task<string?> ResolvePlaybackUrlAsync(Track track, CancellationToken cancellationToken = default);
    Task<byte[]?> DownloadTrackAsync(Track track, CancellationToken cancellationToken = default);
}
