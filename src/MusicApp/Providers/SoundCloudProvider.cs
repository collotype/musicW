using MusicApp.Enums;
using MusicApp.Models;
using MusicApp.Services;
using Newtonsoft.Json.Linq;
using System.Net.Http;

namespace MusicApp.Providers;

public class SoundCloudProvider : IMusicProvider
{
    private readonly HttpClient _httpClient = new();
    private readonly ISettingsService _settingsService;
    private string? _clientId;
    private DateTime? _clientIdExpiry;

    public string ProviderName => "SoundCloud";
    public bool IsAvailable => _settingsService.Settings.EnableSoundCloud;
    public bool SupportsPlayback => true;
    public bool SupportsSearch => true;

    public SoundCloudProvider(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "MusicApp/1.0");
    }

    public async Task<SearchResults> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var results = new SearchResults { Query = query };

        try
        {
            await EnsureClientIdAsync(cancellationToken);

            if (string.IsNullOrEmpty(_clientId))
            {
                results.ErrorMessage = "SoundCloud search is unavailable because a client ID could not be resolved.";
                return results;
            }

            // Search tracks
            var tracksUrl = $"https://api-v2.soundcloud.com/search/tracks?q={Uri.EscapeDataString(query)}&limit=20&client_id={_clientId}";
            var tracksJson = await _httpClient.GetStringAsync(tracksUrl, cancellationToken);
            var tracksData = ParseCollectionArray(tracksJson);
            results.Tracks = ParseTracks(tracksData).ToList();

            // Search users (artists)
            var usersUrl = $"https://api-v2.soundcloud.com/search/users?q={Uri.EscapeDataString(query)}&limit=10&client_id={_clientId}";
            var usersJson = await _httpClient.GetStringAsync(usersUrl, cancellationToken);
            var usersData = ParseCollectionArray(usersJson);
            results.Artists = ParseArtists(usersData).ToList();

            // Search playlists
            var playlistsUrl = $"https://api-v2.soundcloud.com/search/playlists?q={Uri.EscapeDataString(query)}&limit=10&client_id={_clientId}";
            var playlistsJson = await _httpClient.GetStringAsync(playlistsUrl, cancellationToken);
            var playlistsData = ParseCollectionArray(playlistsJson);
            results.Playlists = ParsePlaylists(playlistsData).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SoundCloud search error: {ex.Message}");
            results.ErrorMessage = ex.Message;
        }

        return results;
    }

    public async Task<Artist?> GetArtistAsync(string artistId, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureClientIdAsync(cancellationToken);

            if (string.IsNullOrEmpty(_clientId))
            {
                return null;
            }

            var url = $"https://api-v2.soundcloud.com/users/{artistId}?client_id={_clientId}";
            var json = await _httpClient.GetStringAsync(url, cancellationToken);
            var data = JObject.Parse(json);
            return ParseArtist(data);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SoundCloud get artist error: {ex.Message}");
            return null;
        }
    }

    public async Task<List<Track>> GetArtistTracksAsync(string artistId, CancellationToken cancellationToken = default)
    {
        var tracks = new List<Track>();

        try
        {
            await EnsureClientIdAsync(cancellationToken);

            if (string.IsNullOrEmpty(_clientId))
            {
                return tracks;
            }

            var url = $"https://api-v2.soundcloud.com/users/{artistId}/tracks?limit=50&client_id={_clientId}";
            var json = await _httpClient.GetStringAsync(url, cancellationToken);
            var data = ParseCollectionArray(json);
            tracks = ParseTracks(data).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SoundCloud get artist tracks error: {ex.Message}");
        }

        return tracks;
    }

    public async Task<List<Album>> GetArtistReleasesAsync(string artistId, CancellationToken cancellationToken = default)
    {
        var albums = new List<Album>();

        try
        {
            await EnsureClientIdAsync(cancellationToken);

            if (string.IsNullOrEmpty(_clientId))
            {
                return albums;
            }

            // SoundCloud uses playlists as albums
            var url = $"https://api-v2.soundcloud.com/users/{artistId}/playlists?limit=50&client_id={_clientId}";
            var json = await _httpClient.GetStringAsync(url, cancellationToken);
            var data = ParseCollectionArray(json);
            albums = ParseAlbums(data).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SoundCloud get artist releases error: {ex.Message}");
        }

        return albums;
    }

    public async Task<Album?> GetAlbumAsync(string albumId, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureClientIdAsync(cancellationToken);

            if (string.IsNullOrEmpty(_clientId))
            {
                return null;
            }

            var url = $"https://api-v2.soundcloud.com/playlists/{albumId}?client_id={_clientId}";
            var json = await _httpClient.GetStringAsync(url, cancellationToken);
            var data = JObject.Parse(json);
            return ParseAlbum(data);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SoundCloud get album error: {ex.Message}");
            return null;
        }
    }

    public async Task<Playlist?> GetPlaylistAsync(string playlistId, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureClientIdAsync(cancellationToken);

            if (string.IsNullOrEmpty(_clientId))
            {
                return null;
            }

            var url = $"https://api-v2.soundcloud.com/playlists/{playlistId}?client_id={_clientId}";
            var json = await _httpClient.GetStringAsync(url, cancellationToken);
            var data = JObject.Parse(json);
            return ParsePlaylist(data);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SoundCloud get playlist error: {ex.Message}");
            return null;
        }
    }

    public async Task<string?> ResolvePlaybackUrlAsync(Track track, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(track.ProviderTrackId) || string.IsNullOrEmpty(_clientId))
        {
            return null;
        }

        try
        {
            await EnsureClientIdAsync(cancellationToken);

            // Try progressive (direct MP3) URL
            var progressiveUrl = $"https://api.soundcloud.com/i1/tracks/{track.ProviderTrackId}/streams?client_id={_clientId}";
            var progJson = await _httpClient.GetStringAsync(progressiveUrl, cancellationToken);
            var progData = JObject.Parse(progJson);
            var mp3Url = progData["http_mp3_128"]?["url"]?.ToString();
            if (!string.IsNullOrEmpty(mp3Url))
            {
                return mp3Url;
            }

            var url = $"https://api-v2.soundcloud.com/media/soundcloud:tracks:{track.ProviderTrackId}/stream/hls?client_id={_clientId}";
            var json = await _httpClient.GetStringAsync(url, cancellationToken);
            var data = JObject.Parse(json);
            return data["url"]?.ToString();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SoundCloud resolve playback error: {ex.Message}");
            return null;
        }
    }

    public Task<byte[]?> DownloadTrackAsync(Track track, CancellationToken cancellationToken = default)
    {
        // SoundCloud download would require additional handling
        return Task.FromResult<byte[]?>(null);
    }

    private static JArray ParseCollectionArray(string json)
    {
        var token = JToken.Parse(json);

        return token switch
        {
            JArray array => array,
            JObject obj when obj["collection"] is JArray collection => collection,
            _ => new JArray()
        };
    }

    private async Task EnsureClientIdAsync(CancellationToken cancellationToken = default)
    {
        // Use configured client ID if available
        if (!string.IsNullOrEmpty(_settingsService.Settings.SoundCloudClientId))
        {
            _clientId = _settingsService.Settings.SoundCloudClientId;
            return;
        }

        // Refresh client ID if expired or null
        if (_clientId == null || (_clientIdExpiry.HasValue && DateTime.UtcNow > _clientIdExpiry.Value))
        {
            try
            {
                // Fetch a fresh client ID from SoundCloud's public pages
                var html = await _httpClient.GetStringAsync("https://soundcloud.com/", cancellationToken);

                // Extract client_id from script tags
                var scriptMatches = System.Text.RegularExpressions.Regex.Matches(
                    html,
                    @"client_id\s*:\s*""([a-f0-9]{32})""");

                if (scriptMatches.Count > 0)
                {
                    _clientId = scriptMatches[0].Groups[1].Value;
                    _clientIdExpiry = DateTime.UtcNow + TimeSpan.FromHours(6);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get SoundCloud client_id: {ex.Message}");
            }
        }
    }

    private IEnumerable<Track> ParseTracks(JArray data)
    {
        foreach (var item in data)
        {
            var durationMs = item["duration"]?.Value<long>() ?? 0;
            var artworkUrl = item["artwork_url"]?.ToString();

            // Get larger artwork if available
            if (!string.IsNullOrEmpty(artworkUrl))
            {
                artworkUrl = artworkUrl.Replace("-large.", "-t500x500.");
            }

            yield return new Track
            {
                Id = item["id"]?.ToString() ?? Guid.NewGuid().ToString(),
                ProviderTrackId = item["id"]?.ToString(),
                Source = TrackSource.SoundCloud,
                StorageLocation = StorageLocation.Remote,
                Title = item["title"]?.ToString() ?? "Unknown",
                ArtistName = item["user"]?["username"]?.ToString() ?? "Unknown Artist",
                ArtistId = item["user"]?["id"]?.ToString() ?? string.Empty,
                ArtistImageUrl = item["user"]?["avatar_url"]?.ToString(),
                AlbumTitle = item["label_name"]?.ToString() ?? string.Empty,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                CoverArtUrl = artworkUrl,
                RemotePageUrl = item["permalink_url"]?.ToString(),
                Genres = new List<string> { item["genre"]?.ToString() ?? string.Empty }.Where(g => !string.IsNullOrEmpty(g)).ToList(),
                PlayCount = item["playback_count"]?.Value<long>(),
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
                Name = item["username"]?.ToString() ?? "Unknown",
                ImageUrl = item["avatar_url"]?.ToString(),
                Followers = item["followers_count"]?.Value<long>(),
                TrackCount = item["track_count"]?.Value<int>() ?? 0,
                IsFollowed = false
            };
        }
    }

    private Artist? ParseArtist(JObject data)
    {
        return new Artist
        {
            Id = data["id"]?.ToString() ?? Guid.NewGuid().ToString(),
            Name = data["username"]?.ToString() ?? "Unknown",
            ImageUrl = data["avatar_url"]?.ToString(),
            Biography = data["description"]?.ToString(),
            Followers = data["followers_count"]?.Value<long>(),
            MonthlyListeners = data["followers_count"]?.Value<long>(),
            TrackCount = data["track_count"]?.Value<int>() ?? 0,
            IsFollowed = false
        };
    }

    private IEnumerable<Album> ParseAlbums(JArray data)
    {
        foreach (var item in data)
        {
            var tracks = new List<Track>();
            var tracksData = item["tracks"] as JArray;
            if (tracksData != null)
            {
                tracks = ParseTracks(tracksData).ToList();
            }

            yield return new Album
            {
                Id = item["id"]?.ToString() ?? Guid.NewGuid().ToString(),
                ProviderAlbumId = item["id"]?.ToString(),
                Title = item["title"]?.ToString() ?? "Unknown",
                ArtistName = item["user"]?["username"]?.ToString() ?? "Unknown Artist",
                ArtistId = item["user"]?["id"]?.ToString() ?? string.Empty,
                CoverArtUrl = item["artwork_url"]?.ToString(),
                Tracks = tracks,
                AlbumType = "Playlist"
            };
        }
    }

    private Album? ParseAlbum(JObject data)
    {
        var tracks = new List<Track>();
        var tracksData = data["tracks"] as JArray;
        if (tracksData != null)
        {
            tracks = ParseTracks(tracksData).ToList();
        }

        return new Album
        {
            Id = data["id"]?.ToString() ?? Guid.NewGuid().ToString(),
            ProviderAlbumId = data["id"]?.ToString(),
            Title = data["title"]?.ToString() ?? "Unknown",
            ArtistName = data["user"]?["username"]?.ToString() ?? "Unknown Artist",
            ArtistId = data["user"]?["id"]?.ToString() ?? string.Empty,
            CoverArtUrl = data["artwork_url"]?.ToString(),
            Tracks = tracks,
            AlbumType = "Playlist"
        };
    }

    private IEnumerable<Playlist> ParsePlaylists(JArray data)
    {
        foreach (var item in data)
        {
            var tracks = new List<Track>();
            var tracksData = item["tracks"] as JArray;
            if (tracksData != null)
            {
                tracks = ParseTracks(tracksData).ToList();
            }

            yield return new Playlist
            {
                Id = item["id"]?.ToString() ?? Guid.NewGuid().ToString(),
                ProviderPlaylistId = item["id"]?.ToString(),
                Title = item["title"]?.ToString() ?? "Unknown",
                Description = item["description"]?.ToString(),
                CoverArtUrl = item["artwork_url"]?.ToString(),
                OwnerName = item["user"]?["username"]?.ToString() ?? "Unknown",
                OwnerId = item["user"]?["id"]?.ToString(),
                Tracks = tracks,
                IsPublic = item["sharing"]?.ToString() == "public"
            };
        }
    }

    private Playlist? ParsePlaylist(JObject data)
    {
        var tracks = new List<Track>();
        var tracksData = data["tracks"] as JArray;
        if (tracksData != null)
        {
            tracks = ParseTracks(tracksData).ToList();
        }

        return new Playlist
        {
            Id = data["id"]?.ToString() ?? Guid.NewGuid().ToString(),
            ProviderPlaylistId = data["id"]?.ToString(),
            Title = data["title"]?.ToString() ?? "Unknown",
            Description = data["description"]?.ToString(),
            CoverArtUrl = data["artwork_url"]?.ToString(),
            OwnerName = data["user"]?["username"]?.ToString() ?? "Unknown",
            OwnerId = data["user"]?["id"]?.ToString(),
            Tracks = tracks,
            IsPublic = data["sharing"]?.ToString() == "public"
        };
    }
}
