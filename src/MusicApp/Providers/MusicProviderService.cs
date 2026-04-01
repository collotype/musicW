using MusicApp.Models;
using MusicApp.Services;
using MusicApp.Enums;

namespace MusicApp.Providers;

public class MusicProviderService : IMusicProviderService
{
    private readonly IEnumerable<IMusicProvider> _providers;

    public List<IMusicProvider> Providers => _providers.ToList();

    public MusicProviderService(IEnumerable<IMusicProvider> providers)
    {
        _providers = providers;
    }

    public async Task<SearchResults> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var results = new SearchResults { Query = query };

        foreach (var provider in _providers.Where(p => p.IsAvailable && p.SupportsSearch))
        {
            try
            {
                var providerResults = await provider.SearchAsync(query, cancellationToken);
                results.Tracks.AddRange(providerResults.Tracks);
                results.Artists.AddRange(providerResults.Artists);
                results.Albums.AddRange(providerResults.Albums);
                results.Playlists.AddRange(providerResults.Playlists);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Provider {provider.ProviderName} search error: {ex.Message}");
            }
        }

        return results;
    }

    public async Task<Artist?> GetArtistAsync(string artistId, string providerName)
    {
        var provider = _providers.FirstOrDefault(p => p.ProviderName == providerName && p.IsAvailable);
        if (provider != null)
        {
            return await provider.GetArtistAsync(artistId);
        }
        return null;
    }

    public async Task<List<Track>> GetArtistTracksAsync(string artistId, string providerName)
    {
        var provider = _providers.FirstOrDefault(p => p.ProviderName == providerName && p.IsAvailable);
        if (provider != null)
        {
            return await provider.GetArtistTracksAsync(artistId);
        }
        return new List<Track>();
    }

    public async Task<List<Album>> GetArtistReleasesAsync(string artistId, string providerName)
    {
        var provider = _providers.FirstOrDefault(p => p.ProviderName == providerName && p.IsAvailable);
        if (provider != null)
        {
            return await provider.GetArtistReleasesAsync(artistId);
        }
        return new List<Album>();
    }

    public async Task<Album?> GetAlbumAsync(string albumId, string providerName)
    {
        var provider = _providers.FirstOrDefault(p => p.ProviderName == providerName && p.IsAvailable);
        if (provider != null)
        {
            return await provider.GetAlbumAsync(albumId);
        }
        return null;
    }

    public async Task<Playlist?> GetPlaylistAsync(string playlistId, string providerName)
    {
        var provider = _providers.FirstOrDefault(p => p.ProviderName == providerName && p.IsAvailable);
        if (provider != null)
        {
            return await provider.GetPlaylistAsync(playlistId);
        }
        return null;
    }

    public async Task<string?> ResolvePlaybackUrlAsync(Track track, CancellationToken cancellationToken = default)
    {
        if (track.Source == TrackSource.Local && !string.IsNullOrEmpty(track.LocalFilePath))
        {
            return track.LocalFilePath;
        }

        var provider = _providers.FirstOrDefault(p => p.ProviderName == GetProviderName(track.Source) && p.IsAvailable);
        if (provider != null && provider.SupportsPlayback)
        {
            return await provider.ResolvePlaybackUrlAsync(track, cancellationToken);
        }

        // Fall back to any available provider that supports playback
        foreach (var p in _providers.Where(p => p.IsAvailable && p.SupportsPlayback))
        {
            try
            {
                var url = await p.ResolvePlaybackUrlAsync(track, cancellationToken);
                if (!string.IsNullOrEmpty(url))
                {
                    return url;
                }
            }
            catch
            {
                // Try next provider
            }
        }

        return null;
    }

    private static string GetProviderName(Enums.TrackSource source)
    {
        return source switch
        {
            Enums.TrackSource.Local => "Local",
            Enums.TrackSource.SoundCloud => "SoundCloud",
            Enums.TrackSource.Spotify => "Spotify",
            _ => string.Empty
        };
    }
}
