using MusicApp.Enums;
using MusicApp.Models;
using MusicApp.Services;

namespace MusicApp.Providers;

public class LocalLibraryProvider : IMusicProvider
{
    private readonly ILibraryService _libraryService;

    public string ProviderName => "Local";
    public bool IsAvailable => true;
    public bool SupportsPlayback => true;
    public bool SupportsSearch => true;

    public LocalLibraryProvider(ILibraryService libraryService)
    {
        _libraryService = libraryService;
    }

    public Task<SearchResults> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var queryLower = query.ToLowerInvariant();

        var tracks = _libraryService.AllTracks
            .Where(t => t.Title.ToLowerInvariant().Contains(queryLower) ||
                       t.ArtistName.ToLowerInvariant().Contains(queryLower) ||
                       t.AlbumTitle.ToLowerInvariant().Contains(queryLower))
            .Take(50)
            .ToList();

        var artists = _libraryService.AllArtists
            .Where(a => a.Name.ToLowerInvariant().Contains(queryLower))
            .Take(20)
            .ToList();

        var albums = _libraryService.AllAlbums
            .Where(a => a.Title.ToLowerInvariant().Contains(queryLower) ||
                       a.ArtistName.ToLowerInvariant().Contains(queryLower))
            .Take(20)
            .ToList();

        var playlists = _libraryService.Playlists
            .Where(p => p.Title.ToLowerInvariant().Contains(queryLower))
            .Take(20)
            .ToList();

        return Task.FromResult(new SearchResults
        {
            Query = query,
            Tracks = tracks,
            Artists = artists,
            Albums = albums,
            Playlists = playlists
        });
    }

    public Task<Artist?> GetArtistAsync(string artistId, CancellationToken cancellationToken = default)
    {
        var artist = _libraryService.AllArtists.FirstOrDefault(a => a.Id == artistId);
        return Task.FromResult(artist);
    }

    public Task<List<Track>> GetArtistTracksAsync(string artistId, CancellationToken cancellationToken = default)
    {
        var tracks = _libraryService.AllTracks
            .Where(t => t.ArtistId == artistId)
            .OrderBy(t => t.DiscNumber)
            .ThenBy(t => t.TrackNumber)
            .ToList();
        return Task.FromResult(tracks);
    }

    public Task<List<Album>> GetArtistReleasesAsync(string artistId, CancellationToken cancellationToken = default)
    {
        var albums = _libraryService.AllAlbums
            .Where(a => a.ArtistId == artistId)
            .OrderByDescending(a => a.ReleaseDate)
            .ToList();
        return Task.FromResult(albums);
    }

    public Task<Album?> GetAlbumAsync(string albumId, CancellationToken cancellationToken = default)
    {
        var album = _libraryService.AllAlbums.FirstOrDefault(a => a.Id == albumId);
        return Task.FromResult(album);
    }

    public Task<Playlist?> GetPlaylistAsync(string playlistId, CancellationToken cancellationToken = default)
    {
        var playlist = _libraryService.Playlists.FirstOrDefault(p => p.Id == playlistId);
        return Task.FromResult(playlist);
    }

    public Task<string?> ResolvePlaybackUrlAsync(Track track, CancellationToken cancellationToken = default)
    {
        if (track.Source == TrackSource.Local && !string.IsNullOrEmpty(track.LocalFilePath))
        {
            return Task.FromResult<string?>(track.LocalFilePath);
        }
        return Task.FromResult<string?>(null);
    }

    public Task<byte[]?> DownloadTrackAsync(Track track, CancellationToken cancellationToken = default)
    {
        // Local tracks are already downloaded
        if (track.Source == TrackSource.Local && !string.IsNullOrEmpty(track.LocalFilePath))
        {
            return Task.FromResult<byte[]?>(null);
        }
        return Task.FromResult<byte[]?>(null);
    }
}
