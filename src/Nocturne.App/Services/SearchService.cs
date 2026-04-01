using Nocturne.App.Models;
using Nocturne.App.Models.Enums;

namespace Nocturne.App.Services;

public sealed class SearchService(
    ILibraryService libraryService,
    IOnlineMusicService onlineMusicService,
    IMockDataService mockDataService) : ISearchService
{
    public async Task<SearchResults> SearchAsync(string query, SearchSourceFilter sourceFilter, CancellationToken cancellationToken)
    {
        var trimmedQuery = query.Trim();
        if (string.IsNullOrWhiteSpace(trimmedQuery))
        {
            return mockDataService.CreateSearchPreview(query);
        }

        var localResultsTask = sourceFilter is SearchSourceFilter.All or SearchSourceFilter.Local
            ? Task.FromResult(SearchLocal(trimmedQuery, sourceFilter))
            : Task.FromResult(new SearchResults { Query = trimmedQuery, SourceFilter = sourceFilter });

        var onlineResultsTask = sourceFilter == SearchSourceFilter.Local
            ? Task.FromResult(new SearchResults { Query = trimmedQuery, SourceFilter = sourceFilter })
            : onlineMusicService.SearchAsync(trimmedQuery, sourceFilter, cancellationToken);

        await Task.WhenAll(localResultsTask, onlineResultsTask);

        var localResults = await localResultsTask;
        var onlineResults = await onlineResultsTask;

        return new SearchResults
        {
            Query = trimmedQuery,
            SourceFilter = sourceFilter,
            Tracks = localResults.Tracks.Concat(onlineResults.Tracks).ToList(),
            Artists = localResults.Artists.Concat(onlineResults.Artists).ToList(),
            Albums = localResults.Albums.Concat(onlineResults.Albums).ToList(),
            Playlists = localResults.Playlists.Concat(onlineResults.Playlists).ToList()
        };
    }

    private SearchResults SearchLocal(string query, SearchSourceFilter sourceFilter)
    {
        var comparison = StringComparison.OrdinalIgnoreCase;

        return new SearchResults
        {
            Query = query,
            SourceFilter = sourceFilter,
            Tracks = libraryService.Library.Tracks
                .Where(track =>
                    track.Title.Contains(query, comparison) ||
                    track.ArtistName.Contains(query, comparison) ||
                    (track.AlbumTitle?.Contains(query, comparison) ?? false))
                .Take(12)
                .ToList(),
            Artists = libraryService.Library.Artists
                .Where(artist => artist.Name.Contains(query, comparison))
                .Take(8)
                .ToList(),
            Albums = libraryService.Library.Albums
                .Where(album =>
                    album.Title.Contains(query, comparison) ||
                    album.ArtistName.Contains(query, comparison))
                .Take(8)
                .ToList(),
            Playlists = libraryService.Library.Playlists
                .Where(playlist => playlist.Title.Contains(query, comparison))
                .Take(8)
                .ToList()
        };
    }
}
