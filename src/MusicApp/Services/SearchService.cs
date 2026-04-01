using MusicApp.Models;

namespace MusicApp.Services;

public class SearchService : ISearchService
{
    private readonly ILibraryService _libraryService;
    private readonly IMusicProviderService _providerService;

    public SearchService(ILibraryService libraryService, IMusicProviderService providerService)
    {
        _libraryService = libraryService;
        _providerService = providerService;
    }

    public async Task<SearchResults> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new SearchResults { Query = query };
        }

        var localResults = await SearchLocalAsync(query);
        var onlineResults = await SearchOnlineAsync(query, cancellationToken);

        // Merge results
        var results = new SearchResults
        {
            Query = query,
            Tracks = localResults.Tracks.Concat(onlineResults.Tracks).DistinctBy(t => t.Id).ToList(),
            Artists = localResults.Artists.Concat(onlineResults.Artists).DistinctBy(a => a.Id).ToList(),
            Albums = localResults.Albums.Concat(onlineResults.Albums).DistinctBy(a => a.Id).ToList(),
            Playlists = localResults.Playlists.Concat(onlineResults.Playlists).DistinctBy(p => p.Id).ToList(),
            HasMoreTracks = localResults.HasMoreTracks || onlineResults.HasMoreTracks,
            HasMoreArtists = localResults.HasMoreArtists || onlineResults.HasMoreArtists,
            HasMoreAlbums = localResults.HasMoreAlbums || onlineResults.HasMoreAlbums,
            HasMorePlaylists = localResults.HasMorePlaylists || onlineResults.HasMorePlaylists
        };

        return results;
    }

    public Task<SearchResults> SearchLocalAsync(string query)
    {
        var queryLower = query.ToLowerInvariant();

        var tracks = _libraryService.AllTracks
            .Where(t => MatchesQuery(t, queryLower))
            .Take(20)
            .ToList();

        var artists = _libraryService.AllArtists
            .Where(a => a.Name.ToLowerInvariant().Contains(queryLower))
            .Take(10)
            .ToList();

        var albums = _libraryService.AllAlbums
            .Where(a => a.Title.ToLowerInvariant().Contains(queryLower) ||
                       a.ArtistName.ToLowerInvariant().Contains(queryLower))
            .Take(10)
            .ToList();

        var playlists = _libraryService.Playlists
            .Where(p => p.Title.ToLowerInvariant().Contains(queryLower))
            .Take(10)
            .ToList();

        return Task.FromResult(new SearchResults
        {
            Query = query,
            Tracks = tracks,
            Artists = artists,
            Albums = albums,
            Playlists = playlists,
            HasMoreTracks = tracks.Count == 20,
            HasMoreArtists = artists.Count == 10,
            HasMoreAlbums = albums.Count == 10,
            HasMorePlaylists = playlists.Count == 10
        });
    }

    public async Task<SearchResults> SearchOnlineAsync(string query, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _providerService.SearchAsync(query, cancellationToken);
        }
        catch (Exception ex)
        {
            return new SearchResults
            {
                Query = query,
                ErrorMessage = ex.Message
            };
        }
    }

    private static bool MatchesQuery(Track track, string queryLower)
    {
        return track.Title.ToLowerInvariant().Contains(queryLower) ||
               track.ArtistName.ToLowerInvariant().Contains(queryLower) ||
               track.AlbumTitle.ToLowerInvariant().Contains(queryLower) ||
               track.Genres.Any(g => g.ToLowerInvariant().Contains(queryLower)) ||
               track.Tags.Any(t => t.ToLowerInvariant().Contains(queryLower));
    }
}
