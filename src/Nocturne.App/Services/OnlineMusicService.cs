using Nocturne.App.Helpers;
using Nocturne.App.Models;
using Nocturne.App.Models.Enums;
using Nocturne.App.Providers;

namespace Nocturne.App.Services;

public sealed class OnlineMusicService(IEnumerable<IMusicProvider> providers) : IOnlineMusicService
{
    private readonly IReadOnlyDictionary<TrackSource, IMusicProvider> _providers =
        providers.ToDictionary(provider => provider.Source);

    public async Task<SearchResults> SearchAsync(string query, SearchSourceFilter sourceFilter, CancellationToken cancellationToken)
    {
        var selectedProviders = ResolveProviders(sourceFilter)
            .Where(provider => provider.Source != TrackSource.Local)
            .ToList();

        var tasks = selectedProviders.Select(provider => SafeSearchAsync(provider, query, cancellationToken));
        var results = await Task.WhenAll(tasks);

        var merged = new SearchResults
        {
            Query = query,
            SourceFilter = sourceFilter
        };

        foreach (var result in results)
        {
            merged.Tracks.AddRange(result.Tracks);
            merged.Artists.AddRange(result.Artists);
            merged.Albums.AddRange(result.Albums);
            merged.Playlists.AddRange(result.Playlists);
        }

        return merged;
    }

    public Task<Artist?> GetArtistAsync(TrackSource source, string providerArtistId, CancellationToken cancellationToken) =>
        GetProvider(source).GetArtistAsync(providerArtistId, cancellationToken);

    public Task<IReadOnlyList<Track>> GetArtistTracksAsync(TrackSource source, string providerArtistId, CancellationToken cancellationToken) =>
        GetProvider(source).GetArtistTracksAsync(providerArtistId, cancellationToken);

    public Task<IReadOnlyList<Album>> GetArtistReleasesAsync(TrackSource source, string providerArtistId, CancellationToken cancellationToken) =>
        GetProvider(source).GetArtistReleasesAsync(providerArtistId, cancellationToken);

    public Task<Album?> GetAlbumAsync(TrackSource source, string providerAlbumId, CancellationToken cancellationToken) =>
        GetProvider(source).GetAlbumAsync(providerAlbumId, cancellationToken);

    public Task<Playlist?> GetPlaylistAsync(TrackSource source, string providerPlaylistId, CancellationToken cancellationToken) =>
        GetProvider(source).GetPlaylistAsync(providerPlaylistId, cancellationToken);

    public Task<ResolvedPlaybackStream?> ResolvePlaybackAsync(Track track, CancellationToken cancellationToken) =>
        GetProvider(track.Source).ResolvePlaybackAsync(track, cancellationToken);

    public Task<string?> DownloadTrackAsync(Track track, CancellationToken cancellationToken)
    {
        var downloadsFolder = Path.Combine(AppPaths.Root, "Downloads");
        Directory.CreateDirectory(downloadsFolder);
        return GetProvider(track.Source).DownloadTrackAsync(track, downloadsFolder, cancellationToken);
    }

    private async Task<SearchResults> SafeSearchAsync(IMusicProvider provider, string query, CancellationToken cancellationToken)
    {
        try
        {
            return await provider.SearchAsync(query, cancellationToken);
        }
        catch
        {
            return new SearchResults
            {
                Query = query,
                SourceFilter = SearchSourceFilter.All
            };
        }
    }

    private IMusicProvider GetProvider(TrackSource source)
    {
        if (_providers.TryGetValue(source, out var provider))
        {
            return provider;
        }

        throw new InvalidOperationException($"No provider registered for {source}.");
    }

    private IEnumerable<IMusicProvider> ResolveProviders(SearchSourceFilter sourceFilter)
    {
        return sourceFilter switch
        {
            SearchSourceFilter.Local => _providers.Values.Where(provider => provider.Source == TrackSource.Local),
            SearchSourceFilter.SoundCloud => _providers.Values.Where(provider => provider.Source == TrackSource.SoundCloud),
            SearchSourceFilter.Spotify => _providers.Values.Where(provider => provider.Source == TrackSource.Spotify),
            _ => _providers.Values.Where(provider => provider.Source is TrackSource.SoundCloud or TrackSource.Spotify)
        };
    }
}
