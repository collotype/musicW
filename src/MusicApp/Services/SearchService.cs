using MusicApp.Enums;
using MusicApp.Models;
using MusicApp.Providers;

namespace MusicApp.Services;

public class SearchService : ISearchService
{
    private const string ProviderSeparator = "::";

    private readonly ILibraryService _libraryService;
    private readonly IMusicProviderService _providerService;

    public SearchService(ILibraryService libraryService, IMusicProviderService providerService)
    {
        _libraryService = libraryService;
        _providerService = providerService;
    }

    public async Task<SearchResults> SearchAsync(string query, SearchResultType filter = SearchResultType.All, CancellationToken cancellationToken = default)
    {
        var normalizedQuery = query.Trim();
        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return new SearchResults { Query = normalizedQuery };
        }

        var results = new SearchResults { Query = normalizedQuery };
        var errors = new List<string>();

        if (filter is SearchResultType.All or SearchResultType.Local)
        {
            MergeInto(results, await SearchLocalAsync(normalizedQuery));
        }

        if (filter == SearchResultType.All)
        {
            foreach (var provider in GetRemoteProviders())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var providerResults = await SearchProviderAsync(provider, normalizedQuery, cancellationToken);
                MergeInto(results, providerResults);

                if (!string.IsNullOrWhiteSpace(providerResults.ErrorMessage))
                {
                    errors.Add(providerResults.ErrorMessage);
                }
            }
        }
        else if (filter != SearchResultType.Local)
        {
            var provider = GetProviderForFilter(filter);
            if (provider == null)
            {
                results.ErrorMessage = $"{GetProviderName(filter)} search is unavailable.";
                return results;
            }

            var providerResults = await SearchProviderAsync(provider, normalizedQuery, cancellationToken);
            MergeInto(results, providerResults);

            if (!string.IsNullOrWhiteSpace(providerResults.ErrorMessage))
            {
                errors.Add(providerResults.ErrorMessage);
            }
        }

        if (errors.Count > 0 && (filter != SearchResultType.All || !results.HasAnyResults))
        {
            results.ErrorMessage = string.Join(Environment.NewLine, errors.Distinct());
        }

        return results;
    }

    public Task<SearchResults> SearchLocalAsync(string query)
    {
        var normalizedQuery = query.Trim();
        var queryLower = normalizedQuery.ToLowerInvariant();

        var tracks = _libraryService.AllTracks
            .Where(t => MatchesQuery(t, queryLower))
            .Take(20)
            .ToList();

        var artists = _libraryService.AllArtists
            .Where(a => Contains(a.Name, queryLower) || a.Genres.Any(g => Contains(g, queryLower)))
            .Take(10)
            .ToList();

        var albums = _libraryService.AllAlbums
            .Where(a => Contains(a.Title, queryLower) ||
                        Contains(a.ArtistName, queryLower) ||
                        a.Genres.Any(g => Contains(g, queryLower)))
            .Take(10)
            .ToList();

        var playlists = _libraryService.Playlists
            .Where(p => Contains(p.Title, queryLower) || Contains(p.Description, queryLower))
            .Take(10)
            .ToList();

        return Task.FromResult(new SearchResults
        {
            Query = normalizedQuery,
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
        var results = new SearchResults { Query = query.Trim() };
        var errors = new List<string>();

        foreach (var provider in GetRemoteProviders())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var providerResults = await SearchProviderAsync(provider, query, cancellationToken);
            MergeInto(results, providerResults);

            if (!string.IsNullOrWhiteSpace(providerResults.ErrorMessage))
            {
                errors.Add(providerResults.ErrorMessage);
            }
        }

        if (errors.Count > 0 && !results.HasAnyResults)
        {
            results.ErrorMessage = string.Join(Environment.NewLine, errors.Distinct());
        }

        return results;
    }

    private IEnumerable<IMusicProvider> GetRemoteProviders()
    {
        return _providerService.Providers
            .Where(p => p.IsAvailable && p.SupportsSearch && !string.Equals(p.ProviderName, "Local", StringComparison.OrdinalIgnoreCase));
    }

    private IMusicProvider? GetProviderForFilter(SearchResultType filter)
    {
        var providerName = GetProviderName(filter);
        return _providerService.Providers.FirstOrDefault(
            p => p.IsAvailable &&
                 p.SupportsSearch &&
                 string.Equals(p.ProviderName, providerName, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetProviderName(SearchResultType filter)
    {
        return filter switch
        {
            SearchResultType.Local => "Local",
            SearchResultType.SoundCloud => "SoundCloud",
            SearchResultType.Spotify => "Spotify",
            _ => string.Empty
        };
    }

    private async Task<SearchResults> SearchProviderAsync(IMusicProvider provider, string query, CancellationToken cancellationToken)
    {
        try
        {
            var providerResults = await provider.SearchAsync(query, cancellationToken);
            DecorateProviderResults(providerResults, provider.ProviderName);
            return providerResults;
        }
        catch (Exception ex)
        {
            return new SearchResults
            {
                Query = query,
                ErrorMessage = $"{provider.ProviderName} search failed: {ex.Message}"
            };
        }
    }

    private void DecorateProviderResults(SearchResults results, string providerName)
    {
        if (string.Equals(providerName, "Local", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        foreach (var track in results.Tracks)
        {
            track.Id = CreateNavigationId(providerName, track.Id);
        }

        foreach (var artist in results.Artists)
        {
            artist.Id = CreateNavigationId(providerName, artist.Id);
        }

        foreach (var album in results.Albums)
        {
            album.Id = CreateNavigationId(providerName, album.Id);
        }

        foreach (var playlist in results.Playlists)
        {
            playlist.Id = CreateNavigationId(providerName, playlist.Id);
        }
    }

    private static void MergeInto(SearchResults destination, SearchResults source)
    {
        destination.Tracks = destination.Tracks
            .Concat(source.Tracks)
            .DistinctBy(t => t.Id)
            .ToList();

        destination.Artists = destination.Artists
            .Concat(source.Artists)
            .DistinctBy(a => a.Id)
            .ToList();

        destination.Albums = destination.Albums
            .Concat(source.Albums)
            .DistinctBy(a => a.Id)
            .ToList();

        destination.Playlists = destination.Playlists
            .Concat(source.Playlists)
            .DistinctBy(p => p.Id)
            .ToList();

        destination.HasMoreTracks |= source.HasMoreTracks;
        destination.HasMoreArtists |= source.HasMoreArtists;
        destination.HasMoreAlbums |= source.HasMoreAlbums;
        destination.HasMorePlaylists |= source.HasMorePlaylists;
    }

    private static string CreateNavigationId(string providerName, string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Guid.NewGuid().ToString();
        }

        if (id.Contains(ProviderSeparator, StringComparison.Ordinal))
        {
            return id;
        }

        return $"{providerName}{ProviderSeparator}{id}";
    }

    private static bool MatchesQuery(Track track, string queryLower)
    {
        return Contains(track.Title, queryLower) ||
               Contains(track.ArtistName, queryLower) ||
               Contains(track.AlbumTitle, queryLower) ||
               track.Genres.Any(g => Contains(g, queryLower)) ||
               track.Tags.Any(t => Contains(t, queryLower));
    }

    private static bool Contains(string? value, string queryLower)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.Contains(queryLower, StringComparison.OrdinalIgnoreCase);
    }
}
