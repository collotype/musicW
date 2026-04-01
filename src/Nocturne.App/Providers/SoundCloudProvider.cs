using Nocturne.App.Helpers;
using Nocturne.App.Models;
using Nocturne.App.Models.Enums;
using Nocturne.App.Services;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Nocturne.App.Providers;

public sealed class SoundCloudProvider(
    IHttpClientFactory httpClientFactory,
    ISettingsService settingsService) : IMusicProvider
{
    private static readonly Regex AssetRegex = new(@"https://a-v2\.sndcdn\.com/assets/[^""']+\.js", RegexOptions.Compiled);
    private static readonly Regex[] ClientIdRegexes =
    [
        new Regex(@"client_id:""(?<id>[^""]+)""", RegexOptions.Compiled),
        new Regex(@"client_id=""(?<id>[^""]+)""", RegexOptions.Compiled),
        new Regex(@"client_id:'(?<id>[^']+)'", RegexOptions.Compiled)
    ];

    private readonly SemaphoreSlim _clientIdGate = new(1, 1);
    private string? _clientId;

    public TrackSource Source => TrackSource.SoundCloud;

    public bool SupportsPlayback => true;

    public async Task<SearchResults> SearchAsync(string query, CancellationToken cancellationToken)
    {
        var clientId = await GetClientIdAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return new SearchResults { Query = query, SourceFilter = SearchSourceFilter.SoundCloud };
        }

        var trackTask = GetJsonAsync($"https://api-v2.soundcloud.com/search/tracks?q={Uri.EscapeDataString(query)}&limit=12&client_id={clientId}", cancellationToken);
        var artistTask = GetJsonAsync($"https://api-v2.soundcloud.com/search/users?q={Uri.EscapeDataString(query)}&limit=8&client_id={clientId}", cancellationToken);
        var albumTask = GetJsonAsync($"https://api-v2.soundcloud.com/search/albums?q={Uri.EscapeDataString(query)}&limit=8&client_id={clientId}", cancellationToken);
        var playlistTask = GetJsonAsync($"https://api-v2.soundcloud.com/search/playlists?q={Uri.EscapeDataString(query)}&limit=8&client_id={clientId}", cancellationToken);

        await Task.WhenAll(trackTask, artistTask, albumTask, playlistTask);

        return new SearchResults
        {
            Query = query,
            SourceFilter = SearchSourceFilter.SoundCloud,
            Tracks = MapCollection(await trackTask, MapTrack),
            Artists = MapCollection(await artistTask, MapArtistFromUser),
            Albums = MapCollection(await albumTask, MapAlbumFromPlaylist),
            Playlists = MapCollection(await playlistTask, MapPlaylist)
        };
    }

    public async Task<Artist?> GetArtistAsync(string providerArtistId, CancellationToken cancellationToken)
    {
        var clientId = await GetClientIdAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return null;
        }

        var root = await GetJsonAsync($"https://api-v2.soundcloud.com/users/{providerArtistId}?client_id={clientId}", cancellationToken);
        return root is null ? null : MapArtistFromUser(root.Value);
    }

    public async Task<IReadOnlyList<Track>> GetArtistTracksAsync(string providerArtistId, CancellationToken cancellationToken)
    {
        var clientId = await GetClientIdAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return [];
        }

        var root = await GetJsonAsync($"https://api-v2.soundcloud.com/users/{providerArtistId}/tracks?limit=24&client_id={clientId}", cancellationToken);
        return MapCollection(root, MapTrack);
    }

    public async Task<IReadOnlyList<Album>> GetArtistReleasesAsync(string providerArtistId, CancellationToken cancellationToken)
    {
        var clientId = await GetClientIdAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return [];
        }

        var root = await GetJsonAsync($"https://api-v2.soundcloud.com/users/{providerArtistId}/albums?limit=12&client_id={clientId}", cancellationToken);
        return MapCollection(root, MapAlbumFromPlaylist);
    }

    public async Task<Album?> GetAlbumAsync(string providerAlbumId, CancellationToken cancellationToken)
    {
        var playlist = await GetPlaylistAsync(providerAlbumId, cancellationToken);
        if (playlist is null)
        {
            return null;
        }

        return new Album
        {
            Id = playlist.Id,
            Title = playlist.Title,
            ArtistName = playlist.OwnerName,
            CoverArtUrl = playlist.CoverArtUrl,
            HeaderImageUrl = playlist.CoverArtUrl,
            Source = TrackSource.SoundCloud,
            StorageLocation = StorageLocation.Remote,
            ProviderAlbumId = playlist.ProviderPlaylistId,
            Tracks = playlist.Tracks.ToList(),
            Description = playlist.Description
        };
    }

    public async Task<Playlist?> GetPlaylistAsync(string providerPlaylistId, CancellationToken cancellationToken)
    {
        var clientId = await GetClientIdAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return null;
        }

        var root = await GetJsonAsync($"https://api-v2.soundcloud.com/playlists/{providerPlaylistId}?client_id={clientId}", cancellationToken);
        if (root is null)
        {
            return null;
        }

        var playlist = MapPlaylist(root.Value);
        if (root.Value.TryGetProperty("tracks", out var tracksElement) && tracksElement.ValueKind == JsonValueKind.Array)
        {
            playlist.Tracks = new ObservableCollection<Track>(tracksElement.EnumerateArray().Select(MapTrack));
        }

        return playlist;
    }

    public async Task<ResolvedPlaybackStream?> ResolvePlaybackAsync(Track track, CancellationToken cancellationToken)
    {
        var clientId = await GetClientIdAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return null;
        }

        var playbackCandidates = track.PlaybackCandidates.ToList();
        var authorization = track.TrackAuthorization;

        if (playbackCandidates.Count == 0 && !string.IsNullOrWhiteSpace(track.ProviderTrackId))
        {
            var root = await GetJsonAsync($"https://api-v2.soundcloud.com/tracks/{track.ProviderTrackId}?client_id={clientId}", cancellationToken);
            if (root is not null)
            {
                var refreshedTrack = MapTrack(root.Value);
                playbackCandidates = refreshedTrack.PlaybackCandidates.ToList();
                authorization = refreshedTrack.TrackAuthorization;
            }
        }

        var chosenCandidate = playbackCandidates
            .OrderByDescending(candidate => candidate.IsProgressive)
            .FirstOrDefault();

        if (chosenCandidate is null)
        {
            return null;
        }

        var resolvedStreamUrl = await ResolveTranscodingUrlAsync(chosenCandidate, authorization, clientId, cancellationToken);
        if (string.IsNullOrWhiteSpace(resolvedStreamUrl))
        {
            return null;
        }

        return new ResolvedPlaybackStream
        {
            StreamUrl = resolvedStreamUrl,
            ProviderName = "SoundCloud",
            StreamType = chosenCandidate.IsProgressive ? "progressive" : chosenCandidate.Protocol
        };
    }

    public async Task<string?> DownloadTrackAsync(Track track, string destinationFolder, CancellationToken cancellationToken)
    {
        var resolved = await ResolvePlaybackAsync(track, cancellationToken);
        if (resolved is null)
        {
            return null;
        }

        using var client = httpClientFactory.CreateClient("soundcloud");
        var data = await client.GetByteArrayAsync(resolved.StreamUrl, cancellationToken);
        var safeFileName = string.Concat(track.Title.Select(character => Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));
        var path = Path.Combine(destinationFolder, $"{safeFileName}.mp3");
        await File.WriteAllBytesAsync(path, data, cancellationToken);
        return path;
    }

    private async Task<string?> GetClientIdAsync(CancellationToken cancellationToken)
    {
        if (!settingsService.Current.SoundCloudEnabled)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(_clientId))
        {
            return _clientId;
        }

        await _clientIdGate.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(_clientId))
            {
                return _clientId;
            }

            using var client = httpClientFactory.CreateClient("soundcloud");
            var html = await client.GetStringAsync("https://soundcloud.com", cancellationToken);
            var assets = AssetRegex.Matches(html).Select(match => match.Value).Distinct().Take(8);

            foreach (var assetUrl in assets)
            {
                var assetText = await client.GetStringAsync(assetUrl, cancellationToken);
                foreach (var regex in ClientIdRegexes)
                {
                    var match = regex.Match(assetText);
                    if (match.Success)
                    {
                        _clientId = match.Groups["id"].Value;
                        return _clientId;
                    }
                }
            }

            return null;
        }
        finally
        {
            _clientIdGate.Release();
        }
    }

    private async Task<JsonElement?> GetJsonAsync(string url, CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient("soundcloud");
        using var response = await client.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return document.RootElement.Clone();
    }

    private static List<T> MapCollection<T>(JsonElement? root, Func<JsonElement, T> mapper)
    {
        if (root is null)
        {
            return [];
        }

        var element = root.Value;
        if (element.ValueKind == JsonValueKind.Array)
        {
            return element.EnumerateArray().Select(mapper).ToList();
        }

        if (element.TryGetProperty("collection", out var collection) && collection.ValueKind == JsonValueKind.Array)
        {
            return collection.EnumerateArray().Select(mapper).ToList();
        }

        return [];
    }

    private static Track MapTrack(JsonElement element)
    {
        var user = element.TryGetProperty("user", out var userElement) ? userElement : default;
        var media = element.TryGetProperty("media", out var mediaElement) ? mediaElement : default;
        var artistName = user.ValueKind == JsonValueKind.Object && user.TryGetProperty("username", out var username)
            ? username.GetString() ?? "SoundCloud"
            : "SoundCloud";

        return new Track
        {
            Id = $"soundcloud-{TryGetString(element, "id")}",
            Title = TryGetString(element, "title") ?? string.Empty,
            ArtistName = artistName,
            AlbumTitle = TryGetString(element, "publisher_metadata", "album_title"),
            Duration = TimeSpan.FromMilliseconds(TryGetInt64(element, "duration")),
            CoverArtUrl = TryGetString(element, "artwork_url") ?? TryGetString(user, "avatar_url"),
            ArtistImageUrl = TryGetString(user, "avatar_url"),
            Source = TrackSource.SoundCloud,
            StorageLocation = StorageLocation.Remote,
            ProviderTrackId = TryGetString(element, "id"),
            ProviderArtistId = TryGetString(user, "id"),
            ProviderTrackUrn = TryGetString(element, "urn"),
            TrackAuthorization = TryGetString(element, "track_authorization"),
            RemotePageUrl = TryGetString(element, "permalink_url"),
            Genres = SplitTags(TryGetString(element, "genre")),
            Tags = SplitTags(TryGetString(element, "tag_list")),
            PlaybackCandidates = MapPlaybackCandidates(media)
        };
    }

    private static Artist MapArtistFromUser(JsonElement element)
    {
        var visuals = element.TryGetProperty("visuals", out var visualsElement) ? visualsElement : default;
        var headerImage = TryGetVisualUrl(visuals) ?? TryGetString(element, "avatar_url");

        return new Artist
        {
            Id = $"soundcloud-artist-{TryGetString(element, "id")}",
            Name = TryGetString(element, "username") ?? string.Empty,
            AvatarUrl = TryGetString(element, "avatar_url"),
            HeaderImageUrl = headerImage,
            Source = TrackSource.SoundCloud,
            ProviderArtistId = TryGetString(element, "id"),
            RemotePageUrl = TryGetString(element, "permalink_url"),
            Followers = TryGetInt64(element, "followers_count"),
            TrackCount = (int)TryGetInt64(element, "track_count"),
            About = TryGetString(element, "description") ?? string.Empty,
            Country = TryGetString(element, "country"),
            Genres = SplitTags(TryGetString(element, "genre"))
        };
    }

    private static Album MapAlbumFromPlaylist(JsonElement element)
    {
        return new Album
        {
            Id = $"soundcloud-album-{TryGetString(element, "id")}",
            Title = TryGetString(element, "title") ?? string.Empty,
            ArtistName = TryGetString(element, "user", "username") ?? "SoundCloud",
            CoverArtUrl = TryGetString(element, "artwork_url"),
            HeaderImageUrl = TryGetString(element, "artwork_url"),
            Source = TrackSource.SoundCloud,
            StorageLocation = StorageLocation.Remote,
            ProviderAlbumId = TryGetString(element, "id"),
            RemotePageUrl = TryGetString(element, "permalink_url"),
            Description = TryGetString(element, "description") ?? string.Empty
        };
    }

    private static Playlist MapPlaylist(JsonElement element)
    {
        return new Playlist
        {
            Id = $"soundcloud-playlist-{TryGetString(element, "id")}",
            Title = TryGetString(element, "title") ?? string.Empty,
            OwnerName = TryGetString(element, "user", "username") ?? "SoundCloud",
            Description = TryGetString(element, "description") ?? string.Empty,
            CoverArtUrl = TryGetString(element, "artwork_url"),
            Source = TrackSource.SoundCloud,
            StorageLocation = StorageLocation.Remote,
            ProviderPlaylistId = TryGetString(element, "id"),
            RemotePageUrl = TryGetString(element, "permalink_url"),
            IsEditable = false
        };
    }

    private async Task<string?> ResolveTranscodingUrlAsync(
        PlaybackCandidate candidate,
        string? trackAuthorization,
        string clientId,
        CancellationToken cancellationToken)
    {
        var url = $"{candidate.Url}?client_id={clientId}";
        if (!string.IsNullOrWhiteSpace(trackAuthorization))
        {
            url += $"&track_authorization={Uri.EscapeDataString(trackAuthorization)}";
        }

        var root = await GetJsonAsync(url, cancellationToken);
        return root is not null && root.Value.TryGetProperty("url", out var streamUrl)
            ? streamUrl.GetString()
            : null;
    }

    private static IReadOnlyList<PlaybackCandidate> MapPlaybackCandidates(JsonElement media)
    {
        if (media.ValueKind != JsonValueKind.Object || !media.TryGetProperty("transcodings", out var transcodings))
        {
            return [];
        }

        return transcodings
            .EnumerateArray()
            .Select(candidate => new PlaybackCandidate
            {
                Url = TryGetString(candidate, "url") ?? string.Empty,
                Protocol = TryGetString(candidate, "format", "protocol") ?? string.Empty,
                MimeType = TryGetString(candidate, "format", "mime_type") ?? string.Empty,
                IsProgressive = (TryGetString(candidate, "format", "protocol") ?? string.Empty).Contains("progressive", StringComparison.OrdinalIgnoreCase)
            })
            .ToList();
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var value)
            ? value.GetString() ?? value.ToString()
            : null;
    }

    private static string? TryGetString(JsonElement element, string propertyName, string nestedPropertyName)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var nested))
        {
            return null;
        }

        return TryGetString(nested, nestedPropertyName);
    }

    private static long TryGetInt64(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var value))
        {
            if (value.TryGetInt64(out var numeric))
            {
                return numeric;
            }

            if (long.TryParse(value.ToString(), out var parsed))
            {
                return parsed;
            }
        }

        return 0;
    }

    private static string? TryGetVisualUrl(JsonElement visualsElement)
    {
        if (visualsElement.ValueKind != JsonValueKind.Object || !visualsElement.TryGetProperty("visuals", out var visuals) || visuals.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var visual in visuals.EnumerateArray())
        {
            var url = TryGetString(visual, "visual_url");
            if (!string.IsNullOrWhiteSpace(url))
            {
                return url;
            }
        }

        return null;
    }

    private static List<string> SplitTags(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value
            .Split([' ', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(6)
            .ToList();
    }
}
