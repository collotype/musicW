using Nocturne.App.Models;
using Nocturne.App.Models.Enums;
using Nocturne.App.Services;

namespace Nocturne.App.Providers;

public sealed class LocalLibraryProvider(ILibraryService libraryService) : IMusicProvider
{
    public TrackSource Source => TrackSource.Local;

    public bool SupportsPlayback => true;

    public Task<SearchResults> SearchAsync(string query, CancellationToken cancellationToken)
    {
        var comparison = StringComparison.OrdinalIgnoreCase;

        var results = new SearchResults
        {
            Query = query,
            SourceFilter = SearchSourceFilter.Local,
            Tracks = libraryService.Library.Tracks
                .Where(track =>
                    track.Title.Contains(query, comparison) ||
                    track.ArtistName.Contains(query, comparison) ||
                    (track.AlbumTitle?.Contains(query, comparison) ?? false))
                .ToList(),
            Artists = libraryService.Library.Artists
                .Where(artist => artist.Name.Contains(query, comparison))
                .ToList(),
            Albums = libraryService.Library.Albums
                .Where(album => album.Title.Contains(query, comparison) || album.ArtistName.Contains(query, comparison))
                .ToList(),
            Playlists = libraryService.Library.Playlists
                .Where(playlist => playlist.Title.Contains(query, comparison))
                .ToList()
        };

        return Task.FromResult(results);
    }

    public Task<Artist?> GetArtistAsync(string providerArtistId, CancellationToken cancellationToken)
    {
        Artist? artist = libraryService.Library.Artists.FirstOrDefault(candidate =>
            candidate.Id == providerArtistId ||
            candidate.ProviderArtistId == providerArtistId);

        return Task.FromResult(artist);
    }

    public Task<IReadOnlyList<Track>> GetArtistTracksAsync(string providerArtistId, CancellationToken cancellationToken)
    {
        var artist = libraryService.Library.Artists.FirstOrDefault(candidate =>
            candidate.Id == providerArtistId || candidate.ProviderArtistId == providerArtistId);

        IReadOnlyList<Track> tracks = artist is null
            ? []
            : libraryService.Library.Tracks.Where(track => track.ArtistName == artist.Name).ToList();

        return Task.FromResult(tracks);
    }

    public Task<IReadOnlyList<Album>> GetArtistReleasesAsync(string providerArtistId, CancellationToken cancellationToken)
    {
        var artist = libraryService.Library.Artists.FirstOrDefault(candidate =>
            candidate.Id == providerArtistId || candidate.ProviderArtistId == providerArtistId);

        IReadOnlyList<Album> albums = artist is null
            ? []
            : libraryService.Library.Albums.Where(album => album.ArtistName == artist.Name).ToList();

        return Task.FromResult(albums);
    }

    public Task<Album?> GetAlbumAsync(string providerAlbumId, CancellationToken cancellationToken)
    {
        Album? album = libraryService.Library.Albums.FirstOrDefault(candidate =>
            candidate.Id == providerAlbumId || candidate.ProviderAlbumId == providerAlbumId);

        return Task.FromResult(album);
    }

    public Task<Playlist?> GetPlaylistAsync(string providerPlaylistId, CancellationToken cancellationToken)
    {
        Playlist? playlist = libraryService.Library.Playlists.FirstOrDefault(candidate =>
            candidate.Id == providerPlaylistId || candidate.ProviderPlaylistId == providerPlaylistId);

        return Task.FromResult(playlist);
    }

    public Task<ResolvedPlaybackStream?> ResolvePlaybackAsync(Track track, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(track.LocalFilePath) || !File.Exists(track.LocalFilePath))
        {
            return Task.FromResult<ResolvedPlaybackStream?>(null);
        }

        return Task.FromResult<ResolvedPlaybackStream?>(new ResolvedPlaybackStream
        {
            StreamUrl = track.LocalFilePath,
            ProviderName = "Local Library",
            StreamType = "file"
        });
    }

    public Task<string?> DownloadTrackAsync(Track track, string destinationFolder, CancellationToken cancellationToken)
    {
        return Task.FromResult(track.LocalFilePath);
    }
}
