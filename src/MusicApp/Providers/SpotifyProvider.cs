using MusicApp.Enums;
using MusicApp.Models;
using MusicApp.Services;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

namespace MusicApp.Providers;

public class SpotifyProvider : IMusicProvider
{
    private readonly HttpClient _httpClient = new();
    private readonly ISettingsService _settingsService;
    private string? _accessToken;
    private DateTime? _tokenExpiry;

    public string ProviderName => "Spotify";
    public bool IsAvailable => _settingsService.Settings.EnableSpotify;
    public bool SupportsPlayback => false; // Metadata only
    public bool SupportsSearch => true;

    public SpotifyProvider(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task<SearchResults> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var results = new SearchResults { Query = query };

        if (!await EnsureAuthenticatedAsync(cancellationToken))
        {
            return results;
        }

        try
        {
            var url = $"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(query)}&type=track,artist,album,playlist&limit=20";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JObject.Parse(json);

            if (data["tracks"]?["items"] is JArray tracksData)
            {
                results.Tracks = ParseTracks(tracksData).ToList();
            }

            if (data["artists"]?["items"] is JArray artistsData)
            {
                results.Artists = ParseArtists(artistsData).ToList();
            }

            if (data["albums"]?["items"] is JArray albumsData)
            {
                results.Albums = ParseAlbums(albumsData).ToList();
            }

            if (data["playlists"]?["items"] is JArray playlistsData)
            {
                results.Playlists = ParsePlaylists(playlistsData).ToList();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Spotify search error: {ex.Message}");
            results.ErrorMessage = "Spotify search unavailable (metadata only)";
        }

        return results;
    }

    public async Task<Artist?> GetArtistAsync(string artistId, CancellationToken cancellationToken = default)
    {
        if (!await EnsureAuthenticatedAsync(cancellationToken))
        {
            return null;
        }

        try
        {
            var url = $"https://api.spotify.com/v1/artists/{artistId}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JObject.Parse(json);
            return ParseArtist(data);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Spotify get artist error: {ex.Message}");
            return null;
        }
    }

    public async Task<List<Track>> GetArtistTracksAsync(string artistId, CancellationToken cancellationToken = default)
    {
        var tracks = new List<Track>();

        if (!await EnsureAuthenticatedAsync(cancellationToken))
        {
            return tracks;
        }

        try
        {
            // Get artist's top tracks
            var url = $"https://api.spotify.com/v1/artists/{artistId}/top_tracks?market=US";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JObject.Parse(json);

            if (data["tracks"] is JArray tracksData)
            {
                tracks = ParseTracks(tracksData).ToList();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Spotify get artist tracks error: {ex.Message}");
        }

        return tracks;
    }

    public async Task<List<Album>> GetArtistReleasesAsync(string artistId, CancellationToken cancellationToken = default)
    {
        var albums = new List<Album>();

        if (!await EnsureAuthenticatedAsync(cancellationToken))
        {
            return albums;
        }

        try
        {
            var url = $"https://api.spotify.com/v1/artists/{artistId}/albums?include_groups=album,single&limit=20";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JObject.Parse(json);

            if (data["items"] is JArray albumsData)
            {
                albums = ParseAlbums(albumsData).ToList();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Spotify get artist releases error: {ex.Message}");
        }

        return albums;
    }

    public async Task<Album?> GetAlbumAsync(string albumId, CancellationToken cancellationToken = default)
    {
        if (!await EnsureAuthenticatedAsync(cancellationToken))
        {
            return null;
        }

        try
        {
            var url = $"https://api.spotify.com/v1/albums/{albumId}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JObject.Parse(json);
            return ParseAlbum(data);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Spotify get album error: {ex.Message}");
            return null;
        }
    }

    public async Task<Playlist?> GetPlaylistAsync(string playlistId, CancellationToken cancellationToken = default)
    {
        if (!await EnsureAuthenticatedAsync(cancellationToken))
        {
            return null;
        }

        try
        {
            var url = $"https://api.spotify.com/v1/playlists/{playlistId}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JObject.Parse(json);
            return ParsePlaylist(data);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Spotify get playlist error: {ex.Message}");
            return null;
        }
    }

    public Task<string?> ResolvePlaybackUrlAsync(Track track, CancellationToken cancellationToken = default)
    {
        // Spotify does not support direct playback - metadata only
        return Task.FromResult<string?>(null);
    }

    public Task<byte[]?> DownloadTrackAsync(Track track, CancellationToken cancellationToken = default)
    {
        // Spotify does not support downloads
        return Task.FromResult<byte[]?>(null);
    }

    private async Task<bool> EnsureAuthenticatedAsync(CancellationToken cancellationToken = default)
    {
        // Check if we have a valid token
        if (!string.IsNullOrEmpty(_accessToken) && _tokenExpiry.HasValue && DateTime.UtcNow < _tokenExpiry.Value)
        {
            return true;
        }

        // Check if credentials are configured
        var clientId = _settingsService.Settings.SpotifyClientId;
        var clientSecret = _settingsService.Settings.SpotifyClientSecret;

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            return false;
        }

        try
        {
            // Request access token using client credentials flow
            var tokenUrl = "https://accounts.spotify.com/api/token";
            var credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

            var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JObject.Parse(json);

            _accessToken = data["access_token"]?.ToString();
            var expiresIn = data["expires_in"]?.Value<int>() ?? 3600;
            _tokenExpiry = DateTime.UtcNow + TimeSpan.FromSeconds(expiresIn - 60);

            return !string.IsNullOrEmpty(_accessToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Spotify auth error: {ex.Message}");
            return false;
        }
    }

    private IEnumerable<Track> ParseTracks(JArray data)
    {
        foreach (var item in data)
        {
            var durationMs = item["duration_ms"]?.Value<int>() ?? 0;
            var albumArt = item["album"]?["images"]?.FirstOrDefault()?["url"]?.ToString();

            yield return new Track
            {
                Id = item["id"]?.ToString() ?? Guid.NewGuid().ToString(),
                ProviderTrackId = item["id"]?.ToString(),
                Source = TrackSource.Spotify,
                StorageLocation = StorageLocation.Remote,
                Title = item["name"]?.ToString() ?? "Unknown",
                ArtistName = item["artists"]?.FirstOrDefault()?["name"]?.ToString() ?? "Unknown Artist",
                ArtistId = item["artists"]?.FirstOrDefault()?["id"]?.ToString() ?? string.Empty,
                AlbumTitle = item["album"]?["name"]?.ToString() ?? string.Empty,
                AlbumId = item["album"]?["id"]?.ToString() ?? string.Empty,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                CoverArtUrl = albumArt,
                RemotePageUrl = item["external_urls"]?["spotify"]?.ToString(),
                TrackNumber = item["track_number"]?.Value<int>() ?? 0,
                DiscNumber = item["disc_number"]?.Value<int>() ?? 1,
                IsLiked = false,
                IsDownloaded = false
            };
        }
    }

    private IEnumerable<Artist> ParseArtists(JArray data)
    {
        foreach (var item in data)
        {
            yield return new Artist
            {
                Id = item["id"]?.ToString() ?? Guid.NewGuid().ToString(),
                Name = item["name"]?.ToString() ?? "Unknown",
                ImageUrl = item["images"]?.FirstOrDefault()?["url"]?.ToString(),
                Followers = item["followers"]?["total"]?.Value<long>(),
                Genres = item["genres"]?.ToObject<List<string>>() ?? new List<string>(),
                IsFollowed = false
            };
        }
    }

    private Artist? ParseArtist(JObject data)
    {
        return new Artist
        {
            Id = data["id"]?.ToString() ?? Guid.NewGuid().ToString(),
            Name = data["name"]?.ToString() ?? "Unknown",
            ImageUrl = data["images"]?.FirstOrDefault()?["url"]?.ToString(),
            Followers = data["followers"]?["total"]?.Value<long>(),
            MonthlyListeners = data["followers"]?["total"]?.Value<long>(),
            Genres = data["genres"]?.ToObject<List<string>>() ?? new List<string>(),
            IsFollowed = false
        };
    }

    private IEnumerable<Album> ParseAlbums(JArray data)
    {
        foreach (var item in data)
        {
            yield return new Album
            {
                Id = item["id"]?.ToString() ?? Guid.NewGuid().ToString(),
                ProviderAlbumId = item["id"]?.ToString(),
                Title = item["name"]?.ToString() ?? "Unknown",
                ArtistName = item["artists"]?.FirstOrDefault()?["name"]?.ToString() ?? "Unknown Artist",
                ArtistId = item["artists"]?.FirstOrDefault()?["id"]?.ToString() ?? string.Empty,
                CoverArtUrl = item["images"]?.FirstOrDefault()?["url"]?.ToString(),
                ReleaseDate = !string.IsNullOrEmpty(item["release_date"]?.ToString())
                    ? DateTime.Parse(item["release_date"].ToString())
                    : null,
                AlbumType = item["album_type"]?.ToString() ?? "Album"
            };
        }
    }

    private Album? ParseAlbum(JObject data)
    {
        var tracks = new List<Track>();
        if (data["tracks"]?["items"] is JArray tracksData)
        {
            tracks = ParseTracks(tracksData).ToList();
        }

        return new Album
        {
            Id = data["id"]?.ToString() ?? Guid.NewGuid().ToString(),
            ProviderAlbumId = data["id"]?.ToString(),
            Title = data["name"]?.ToString() ?? "Unknown",
            ArtistName = data["artists"]?.FirstOrDefault()?["name"]?.ToString() ?? "Unknown Artist",
            ArtistId = data["artists"]?.FirstOrDefault()?["id"]?.ToString() ?? string.Empty,
            CoverArtUrl = data["images"]?.FirstOrDefault()?["url"]?.ToString(),
            ReleaseDate = !string.IsNullOrEmpty(data["release_date"]?.ToString())
                ? DateTime.Parse(data["release_date"].ToString())
                : null,
            AlbumType = data["album_type"]?.ToString() ?? "Album",
            Tracks = tracks
        };
    }

    private IEnumerable<Playlist> ParsePlaylists(JArray data)
    {
        foreach (var item in data)
        {
            yield return new Playlist
            {
                Id = item["id"]?.ToString() ?? Guid.NewGuid().ToString(),
                ProviderPlaylistId = item["id"]?.ToString(),
                Title = item["name"]?.ToString() ?? "Unknown",
                Description = item["description"]?.ToString(),
                CoverArtUrl = item["images"]?.FirstOrDefault()?["url"]?.ToString(),
                OwnerName = item["owner"]?["display_name"]?.ToString() ?? "Unknown",
                OwnerId = item["owner"]?["id"]?.ToString(),
                IsPublic = item["public"]?.Value<bool>() ?? true
            };
        }
    }

    private Playlist? ParsePlaylist(JObject data)
    {
        var tracks = new List<Track>();
        if (data["tracks"]?["items"] is JArray tracksData)
        {
            foreach (var trackItem in tracksData)
            {
                if (trackItem["track"] is JObject trackData)
                {
                    var parsedTracks = ParseTracks(new JArray { trackData });
                    tracks.AddRange(parsedTracks);
                }
            }
        }

        return new Playlist
        {
            Id = data["id"]?.ToString() ?? Guid.NewGuid().ToString(),
            ProviderPlaylistId = data["id"]?.ToString(),
            Title = data["name"]?.ToString() ?? "Unknown",
            Description = data["description"]?.ToString(),
            CoverArtUrl = data["images"]?.FirstOrDefault()?["url"]?.ToString(),
            OwnerName = data["owner"]?["display_name"]?.ToString() ?? "Unknown",
            OwnerId = data["owner"]?["id"]?.ToString(),
            Tracks = tracks,
            IsPublic = data["public"]?.Value<bool>() ?? true
        };
    }
}
