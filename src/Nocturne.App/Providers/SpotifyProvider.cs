using Nocturne.App.Models;
using Nocturne.App.Models.Enums;
using Nocturne.App.Services;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Nocturne.App.Providers;

public sealed class SpotifyProvider(
    IHttpClientFactory httpClientFactory,
    ISettingsService settingsService) : IMusicProvider
{
    private readonly SemaphoreSlim _tokenGate = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _accessTokenExpiresAt;

    public TrackSource Source => TrackSource.Spotify;

    public bool SupportsPlayback => false;

    public async Task<SearchResults> SearchAsync(string query, CancellationToken cancellationToken)
    {
        if (!IsConfigured())
        {
            return new SearchResults { Query = query, SourceFilter = SearchSourceFilter.Spotify };
        }

        var token = await GetAccessTokenAsync(cancellationToken);
        using var client = httpClientFactory.CreateClient("spotify");
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(query)}&type=track,artist,album,playlist&limit=8&market={settingsService.Current.SpotifyMarket}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;

        return new SearchResults
        {
            Query = query,
            SourceFilter = SearchSourceFilter.Spotify,
            Tracks = MapTracks(root, "tracks", "items"),
            Artists = MapArtists(root, "artists", "items"),
            Albums = MapAlbums(root, "albums", "items"),
            Playlists = MapPlaylists(root, "playlists", "items")
        };
    }

    public async Task<Artist?> GetArtistAsync(string providerArtistId, CancellationToken cancellationToken)
    {
        var root = await GetJsonAsync($"https://api.spotify.com/v1/artists/{providerArtistId}", cancellationToken);
        return root is null ? null : MapArtist(root.Value);
    }

    public async Task<IReadOnlyList<Track>> GetArtistTracksAsync(string providerArtistId, CancellationToken cancellationToken)
    {
        var root = await GetJsonAsync($"https://api.spotify.com/v1/artists/{providerArtistId}/top-tracks?market={settingsService.Current.SpotifyMarket}", cancellationToken);
        return root is null ? [] : MapTrackArray(root.Value, "tracks");
    }

    public async Task<IReadOnlyList<Album>> GetArtistReleasesAsync(string providerArtistId, CancellationToken cancellationToken)
    {
        var root = await GetJsonAsync($"https://api.spotify.com/v1/artists/{providerArtistId}/albums?market={settingsService.Current.SpotifyMarket}&limit=12", cancellationToken);
        return root is null ? [] : MapAlbumArray(root.Value, "items");
    }

    public async Task<Album?> GetAlbumAsync(string providerAlbumId, CancellationToken cancellationToken)
    {
        var root = await GetJsonAsync($"https://api.spotify.com/v1/albums/{providerAlbumId}", cancellationToken);
        if (root is null)
        {
            return null;
        }

        var album = MapAlbum(root.Value);
        album.Tracks = MapTrackArray(root.Value.GetProperty("tracks"), "items").ToList();
        return album;
    }

    public async Task<Playlist?> GetPlaylistAsync(string providerPlaylistId, CancellationToken cancellationToken)
    {
        var root = await GetJsonAsync($"https://api.spotify.com/v1/playlists/{providerPlaylistId}", cancellationToken);
        if (root is null)
        {
            return null;
        }

        var playlist = MapPlaylist(root.Value);
        if (root.Value.TryGetProperty("tracks", out var tracksElement) &&
            tracksElement.TryGetProperty("items", out var itemsElement))
        {
            playlist.Tracks = new ObservableCollection<Track>(
                itemsElement.EnumerateArray()
                    .Select(item => item.TryGetProperty("track", out var trackElement) ? MapTrack(trackElement) : null)
                    .OfType<Track>());
        }

        return playlist;
    }

    public Task<ResolvedPlaybackStream?> ResolvePlaybackAsync(Track track, CancellationToken cancellationToken)
    {
        return Task.FromResult<ResolvedPlaybackStream?>(null);
    }

    public Task<string?> DownloadTrackAsync(Track track, string destinationFolder, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(null);
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_accessToken) && _accessTokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
        {
            return _accessToken;
        }

        await _tokenGate.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(_accessToken) && _accessTokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
            {
                return _accessToken;
            }

            using var client = httpClientFactory.CreateClient("spotify");
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{settingsService.Current.SpotifyClientId}:{settingsService.Current.SpotifyClientSecret}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials"
            });

            using var response = await client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            _accessToken = document.RootElement.GetProperty("access_token").GetString();
            _accessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(document.RootElement.GetProperty("expires_in").GetInt32());

            return _accessToken ?? string.Empty;
        }
        finally
        {
            _tokenGate.Release();
        }
    }

    private async Task<JsonElement?> GetJsonAsync(string url, CancellationToken cancellationToken)
    {
        if (!IsConfigured())
        {
            return null;
        }

        var token = await GetAccessTokenAsync(cancellationToken);
        using var client = httpClientFactory.CreateClient("spotify");
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return document.RootElement.Clone();
    }

    private bool IsConfigured()
    {
        return !string.IsNullOrWhiteSpace(settingsService.Current.SpotifyClientId) &&
               !string.IsNullOrWhiteSpace(settingsService.Current.SpotifyClientSecret);
    }

    private static List<Track> MapTracks(JsonElement root, string containerName, string itemsName)
    {
        if (!root.TryGetProperty(containerName, out var container) || !container.TryGetProperty(itemsName, out var items))
        {
            return [];
        }

        return items.EnumerateArray().Select(MapTrack).ToList();
    }

    private static List<Artist> MapArtists(JsonElement root, string containerName, string itemsName)
    {
        if (!root.TryGetProperty(containerName, out var container) || !container.TryGetProperty(itemsName, out var items))
        {
            return [];
        }

        return items.EnumerateArray().Select(MapArtist).ToList();
    }

    private static List<Album> MapAlbums(JsonElement root, string containerName, string itemsName)
    {
        if (!root.TryGetProperty(containerName, out var container) || !container.TryGetProperty(itemsName, out var items))
        {
            return [];
        }

        return items.EnumerateArray().Select(MapAlbum).ToList();
    }

    private static List<Playlist> MapPlaylists(JsonElement root, string containerName, string itemsName)
    {
        if (!root.TryGetProperty(containerName, out var container) || !container.TryGetProperty(itemsName, out var items))
        {
            return [];
        }

        return items.EnumerateArray().Select(MapPlaylist).ToList();
    }

    private static IReadOnlyList<Track> MapTrackArray(JsonElement root, string itemsName)
    {
        if (!root.TryGetProperty(itemsName, out var items))
        {
            return [];
        }

        return items.EnumerateArray().Select(MapTrack).ToList();
    }

    private static IReadOnlyList<Album> MapAlbumArray(JsonElement root, string itemsName)
    {
        if (!root.TryGetProperty(itemsName, out var items))
        {
            return [];
        }

        return items.EnumerateArray().Select(MapAlbum).ToList();
    }

    private static Track MapTrack(JsonElement element)
    {
        var artists = element.TryGetProperty("artists", out var artistsElement)
            ? artistsElement.EnumerateArray().Select(item => item.GetProperty("name").GetString()).OfType<string>().ToList()
            : [];

        var albumElement = element.TryGetProperty("album", out var albumValue) ? albumValue : default;
        var albumImages = albumElement.ValueKind == JsonValueKind.Object && albumElement.TryGetProperty("images", out var imageArray)
            ? imageArray
            : default;

        return new Track
        {
            Title = element.GetProperty("name").GetString() ?? string.Empty,
            ArtistName = artists.Count > 0 ? string.Join(", ", artists) : "Spotify",
            AlbumTitle = albumElement.ValueKind == JsonValueKind.Object ? albumElement.GetProperty("name").GetString() : null,
            Duration = TimeSpan.FromMilliseconds(element.TryGetProperty("duration_ms", out var duration) ? duration.GetInt32() : 0),
            CoverArtUrl = GetImageUrl(albumImages),
            Source = TrackSource.Spotify,
            StorageLocation = StorageLocation.Remote,
            ProviderTrackId = element.GetProperty("id").GetString(),
            ProviderAlbumId = albumElement.ValueKind == JsonValueKind.Object && albumElement.TryGetProperty("id", out var albumId) ? albumId.GetString() : null,
            ProviderArtistId = element.TryGetProperty("artists", out var artistNodes) && artistNodes.GetArrayLength() > 0 ? artistNodes[0].GetProperty("id").GetString() : null,
            RemotePageUrl = TryGetNestedString(element, "external_urls", "spotify"),
            IsDownloaded = false
        };
    }

    private static Artist MapArtist(JsonElement element)
    {
        return new Artist
        {
            Id = element.GetProperty("id").GetString() ?? Guid.NewGuid().ToString("N"),
            Name = element.GetProperty("name").GetString() ?? string.Empty,
            AvatarUrl = element.TryGetProperty("images", out var images) ? GetImageUrl(images) : null,
            Source = TrackSource.Spotify,
            ProviderArtistId = element.GetProperty("id").GetString(),
            RemotePageUrl = TryGetNestedString(element, "external_urls", "spotify"),
            Followers = element.TryGetProperty("followers", out var followers) && followers.TryGetProperty("total", out var total) ? total.GetInt64() : 0,
            Genres = element.TryGetProperty("genres", out var genres) ? genres.EnumerateArray().Select(item => item.GetString()).OfType<string>().ToList() : []
        };
    }

    private static Album MapAlbum(JsonElement element)
    {
        var artistName = element.TryGetProperty("artists", out var artists) && artists.GetArrayLength() > 0
            ? artists[0].GetProperty("name").GetString()
            : "Spotify";

        return new Album
        {
            Id = element.GetProperty("id").GetString() ?? Guid.NewGuid().ToString("N"),
            Title = element.GetProperty("name").GetString() ?? string.Empty,
            ArtistName = artistName ?? "Spotify",
            CoverArtUrl = element.TryGetProperty("images", out var images) ? GetImageUrl(images) : null,
            Source = TrackSource.Spotify,
            StorageLocation = StorageLocation.Remote,
            ProviderAlbumId = element.GetProperty("id").GetString(),
            RemotePageUrl = TryGetNestedString(element, "external_urls", "spotify"),
            ReleaseDate = ParseDate(element.TryGetProperty("release_date", out var releaseDate) ? releaseDate.GetString() : null)
        };
    }

    private static Playlist MapPlaylist(JsonElement element)
    {
        return new Playlist
        {
            Id = element.GetProperty("id").GetString() ?? Guid.NewGuid().ToString("N"),
            Title = element.GetProperty("name").GetString() ?? string.Empty,
            OwnerName = element.TryGetProperty("owner", out var owner) && owner.TryGetProperty("display_name", out var ownerName)
                ? ownerName.GetString() ?? "Spotify"
                : "Spotify",
            CoverArtUrl = element.TryGetProperty("images", out var images) ? GetImageUrl(images) : null,
            Source = TrackSource.Spotify,
            StorageLocation = StorageLocation.Remote,
            ProviderPlaylistId = element.GetProperty("id").GetString(),
            RemotePageUrl = TryGetNestedString(element, "external_urls", "spotify"),
            IsEditable = false
        };
    }

    private static string? GetImageUrl(JsonElement images)
    {
        if (images.ValueKind != JsonValueKind.Array || images.GetArrayLength() == 0)
        {
            return null;
        }

        return images[0].TryGetProperty("url", out var url) ? url.GetString() : null;
    }

    private static string? TryGetNestedString(JsonElement element, string propertyName, string nestedPropertyName)
    {
        if (!element.TryGetProperty(propertyName, out var nested))
        {
            return null;
        }

        return nested.TryGetProperty(nestedPropertyName, out var value) ? value.GetString() : null;
    }

    private static DateTimeOffset? ParseDate(string? value)
    {
        return DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
    }
}
