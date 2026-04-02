using MusicApp.Enums;
using MusicApp.Models;
using MusicApp.Persistence;

namespace MusicApp.Services;

public class LibraryService : ILibraryService
{
    private static readonly StringComparer TextComparer = StringComparer.OrdinalIgnoreCase;
    private static readonly HashSet<string> LegacySeedPlaylistTitles = new(TextComparer)
    {
        "Chill Vibes",
        "Workout Mix",
        "Focus Mode"
    };

    private static readonly HashSet<string> LegacySeedPlaylistDescriptions = new(TextComparer)
    {
        "Perfect for relaxing",
        "High energy tracks",
        "Deep work music"
    };

    private readonly AppDataStore _dataStore;
    private readonly List<Track> _tracks = new();
    private readonly List<Artist> _artists = new();
    private readonly List<Album> _albums = new();
    private readonly List<Playlist> _playlists = new();
    private readonly HashSet<string> _favoriteArtistIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _savedAlbumIds = new(StringComparer.OrdinalIgnoreCase);

    public event EventHandler? LibraryChanged;

    public List<Track> AllTracks => _tracks;
    public List<Artist> AllArtists => _artists;
    public List<Album> AllAlbums => _albums;
    public List<Playlist> Playlists => _playlists;
    public List<Artist> FavoriteArtists => _artists.Where(artist => artist.IsFollowed).ToList();
    public List<Album> SavedAlbums => _albums.Where(album => album.IsLiked).ToList();
    public List<Playlist> PinnedPlaylists => _playlists.Where(playlist => playlist.IsPinned).OrderByDescending(playlist => playlist.LastModifiedDate ?? playlist.CreatedDate).ToList();
    public List<Track> LikedTracks => _tracks.Where(track => track.IsLiked).OrderByDescending(track => track.LastPlayedAt ?? DateTime.MinValue).ToList();
    public List<Track> OfflineTracks => _tracks.Where(track => track.IsDownloaded).OrderByDescending(track => track.DateAdded).ToList();

    public LibraryService(AppDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public async Task InitializeAsync()
    {
        var storedTracks = _dataStore.Tracks.ToList();
        var storedPlaylists = _dataStore.Playlists.ToList();
        var sanitizedTracks = SanitizeStoredTracks(storedTracks);
        var sanitizedPlaylists = SanitizeStoredPlaylists(storedPlaylists, sanitizedTracks, out var playlistsChanged);
        var librarySanitized = sanitizedTracks.Count != storedTracks.Count || playlistsChanged;

        _tracks.Clear();
        _tracks.AddRange(sanitizedTracks);

        _playlists.Clear();
        _playlists.AddRange(sanitizedPlaylists);

        _favoriteArtistIds.Clear();
        foreach (var id in _dataStore.FavoriteArtistIds.Where(id => !string.IsNullOrWhiteSpace(id)))
        {
            _favoriteArtistIds.Add(id);
        }

        _savedAlbumIds.Clear();
        foreach (var id in _dataStore.SavedAlbumIds.Where(id => !string.IsNullOrWhiteSpace(id)))
        {
            _savedAlbumIds.Add(id);
        }

        RebuildArtistsAndAlbums();

        if (librarySanitized)
        {
            await PersistCoreStateAsync();
        }

        LibraryChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task AddTrackAsync(Track track)
    {
        var existing = FindExistingTrack(track);
        if (existing != null)
        {
            track.Id = existing.Id;
            track.IsLiked = track.IsLiked || existing.IsLiked;
            track.IsDownloaded = track.IsDownloaded || existing.IsDownloaded;
            track.PlayCount ??= existing.PlayCount;
            track.DateAdded = existing.DateAdded == default ? DateTime.UtcNow : existing.DateAdded;
            track.LastPlayedAt ??= existing.LastPlayedAt;
            track.LastPlaybackPosition ??= existing.LastPlaybackPosition;

            var index = _tracks.IndexOf(existing);
            _tracks[index] = track;
        }
        else
        {
            if (track.DateAdded == default)
            {
                track.DateAdded = DateTime.UtcNow;
            }

            _tracks.Add(track);
        }

        RebuildArtistsAndAlbums();
        await PersistCoreStateAsync();
        LibraryChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task RemoveTrackAsync(string trackId)
    {
        var track = _tracks.FirstOrDefault(item => item.Id == trackId);
        if (track == null)
        {
            return;
        }

        _tracks.Remove(track);
        foreach (var playlist in _playlists)
        {
            playlist.Tracks.RemoveAll(item => item.Id == trackId);
        }

        RebuildArtistsAndAlbums();
        await PersistCoreStateAsync();
        LibraryChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task ToggleLikeAsync(string trackId)
    {
        var track = _tracks.FirstOrDefault(item => item.Id == trackId);
        if (track == null)
        {
            return;
        }

        track.IsLiked = !track.IsLiked;
        await PersistCoreStateAsync();
        LibraryChanged?.Invoke(this, EventArgs.Empty);
    }

    public Task<bool> IsLikedAsync(string trackId)
    {
        var track = _tracks.FirstOrDefault(item => item.Id == trackId);
        return Task.FromResult(track?.IsLiked ?? false);
    }

    public async Task CreatePlaylistAsync(string title, string? description = null)
    {
        _playlists.Add(new Playlist
        {
            Title = title,
            Description = description,
            OwnerName = "You",
            CreatedDate = DateTime.Now
        });

        await PersistCoreStateAsync();
        LibraryChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task AddToPlaylistAsync(string playlistId, Track track)
    {
        var playlist = _playlists.FirstOrDefault(item => item.Id == playlistId);
        if (playlist == null)
        {
            return;
        }

        if (_tracks.All(existing => existing.Id != track.Id))
        {
            await AddTrackAsync(track);
        }

        if (playlist.Tracks.Any(item => item.Id == track.Id))
        {
            return;
        }

        playlist.Tracks.Add(track.Clone());
        playlist.LastModifiedDate = DateTime.Now;

        await PersistCoreStateAsync();
        LibraryChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task RemoveFromPlaylistAsync(string playlistId, string trackId)
    {
        var playlist = _playlists.FirstOrDefault(item => item.Id == playlistId);
        if (playlist == null)
        {
            return;
        }

        playlist.Tracks.RemoveAll(track => track.Id == trackId);
        playlist.LastModifiedDate = DateTime.Now;

        await PersistCoreStateAsync();
        LibraryChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task ReorderPlaylistTrackAsync(string playlistId, int fromIndex, int toIndex)
    {
        var playlist = _playlists.FirstOrDefault(item => item.Id == playlistId);
        if (playlist == null || fromIndex < 0 || toIndex < 0 || fromIndex >= playlist.Tracks.Count || toIndex >= playlist.Tracks.Count || fromIndex == toIndex)
        {
            return;
        }

        var track = playlist.Tracks[fromIndex];
        playlist.Tracks.RemoveAt(fromIndex);
        playlist.Tracks.Insert(toIndex, track);
        playlist.LastModifiedDate = DateTime.Now;

        await PersistCoreStateAsync();
        LibraryChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task TogglePlaylistPinAsync(string playlistId)
    {
        var playlist = _playlists.FirstOrDefault(item => item.Id == playlistId);
        if (playlist == null)
        {
            return;
        }

        playlist.IsPinned = !playlist.IsPinned;
        playlist.LastModifiedDate = DateTime.Now;

        await PersistCoreStateAsync();
        LibraryChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task UpdatePlaylistCoverAsync(string playlistId, string? coverArtUrl)
    {
        var playlist = _playlists.FirstOrDefault(item => item.Id == playlistId);
        if (playlist == null || playlist.IsSystemPlaylist)
        {
            return;
        }

        playlist.CoverArtUrl = string.IsNullOrWhiteSpace(coverArtUrl) ? null : coverArtUrl;
        playlist.LastModifiedDate = DateTime.Now;

        await PersistCoreStateAsync();
        LibraryChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task DeletePlaylistAsync(string playlistId)
    {
        var playlist = _playlists.FirstOrDefault(item => item.Id == playlistId);
        if (playlist == null || playlist.IsSystemPlaylist)
        {
            return;
        }

        _playlists.Remove(playlist);
        await PersistCoreStateAsync();
        LibraryChanged?.Invoke(this, EventArgs.Empty);
    }

    public Task<Playlist?> GetPlaylistAsync(string playlistId)
    {
        return Task.FromResult(_playlists.FirstOrDefault(item => item.Id == playlistId));
    }

    public Task<Artist?> GetArtistAsync(string artistId)
    {
        return Task.FromResult(_artists.FirstOrDefault(item => item.Id == artistId));
    }

    public Task<Album?> GetAlbumAsync(string albumId)
    {
        return Task.FromResult(_albums.FirstOrDefault(item => item.Id == albumId));
    }

    public async Task ToggleFavoriteArtistAsync(string artistId)
    {
        if (_favoriteArtistIds.Contains(artistId))
        {
            _favoriteArtistIds.Remove(artistId);
        }
        else
        {
            _favoriteArtistIds.Add(artistId);
        }

        RebuildArtistsAndAlbums();
        await PersistCoreStateAsync();
        LibraryChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task ToggleSaveAlbumAsync(string albumId)
    {
        if (_savedAlbumIds.Contains(albumId))
        {
            _savedAlbumIds.Remove(albumId);
        }
        else
        {
            _savedAlbumIds.Add(albumId);
        }

        RebuildArtistsAndAlbums();
        await PersistCoreStateAsync();
        LibraryChanged?.Invoke(this, EventArgs.Empty);
    }

    private Track? FindExistingTrack(Track track)
    {
        return _tracks.FirstOrDefault(existing =>
            existing.Id == track.Id ||
            (!string.IsNullOrWhiteSpace(track.LocalFilePath) &&
             !string.IsNullOrWhiteSpace(existing.LocalFilePath) &&
             string.Equals(existing.LocalFilePath, track.LocalFilePath, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrWhiteSpace(track.ProviderTrackId) &&
             existing.Source == track.Source &&
             string.Equals(existing.ProviderTrackId, track.ProviderTrackId, StringComparison.OrdinalIgnoreCase)));
    }

    private void RebuildArtistsAndAlbums()
    {
        _artists.Clear();
        foreach (var group in _tracks.GroupBy(track => new { track.ArtistId, track.ArtistName }))
        {
            var artistId = string.IsNullOrWhiteSpace(group.Key.ArtistId) ? group.Key.ArtistName : group.Key.ArtistId;
            var tracks = group.OrderByDescending(track => track.PlayCount ?? 0).ThenBy(track => track.Title).ToList();

            _artists.Add(new Artist
            {
                Id = artistId,
                Name = group.Key.ArtistName,
                ImageUrl = tracks.FirstOrDefault(track => !string.IsNullOrWhiteSpace(track.ArtistImageUrl))?.ArtistImageUrl,
                TrackCount = group.Count(),
                AlbumCount = group.Select(track => track.AlbumId).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                TopTracks = tracks.Take(5).Select(track => track.Clone()).ToList(),
                Genres = group.SelectMany(track => track.Genres).Where(genre => !string.IsNullOrWhiteSpace(genre)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                IsFollowed = _favoriteArtistIds.Contains(artistId)
            });
        }

        _albums.Clear();
        foreach (var group in _tracks.GroupBy(track => new { track.AlbumId, track.AlbumTitle, track.ArtistId, track.ArtistName }))
        {
            if (string.IsNullOrWhiteSpace(group.Key.AlbumTitle))
            {
                continue;
            }

            var firstTrack = group.First();
            var albumId = string.IsNullOrWhiteSpace(group.Key.AlbumId) ? group.Key.AlbumTitle : group.Key.AlbumId;
            var tracks = group.OrderBy(track => track.DiscNumber).ThenBy(track => track.TrackNumber).ToList();

            _albums.Add(new Album
            {
                Id = albumId,
                Title = group.Key.AlbumTitle,
                ArtistName = group.Key.ArtistName,
                ArtistId = group.Key.ArtistId,
                CoverArtUrl = firstTrack.CoverArtUrl,
                ReleaseDate = firstTrack.ReleaseDate,
                Tracks = tracks.Select(track => track.Clone()).ToList(),
                Genres = group.SelectMany(track => track.Genres).Where(genre => !string.IsNullOrWhiteSpace(genre)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                IsLiked = _savedAlbumIds.Contains(albumId),
                IsDownloaded = tracks.All(track => track.IsDownloaded)
            });
        }
    }

    private async Task PersistCoreStateAsync()
    {
        _dataStore.Tracks = _tracks.ToList();
        _dataStore.Playlists = _playlists.ToList();
        _dataStore.FavoriteArtistIds = _favoriteArtistIds.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToList();
        _dataStore.SavedAlbumIds = _savedAlbumIds.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToList();
        await _dataStore.SaveAllAsync();
    }

    private static List<Track> SanitizeStoredTracks(IEnumerable<Track> tracks)
    {
        return tracks
            .Where(track => !IsLegacySeedTrack(track))
            .Select(track =>
            {
                if (track.DateAdded == default)
                {
                    track.DateAdded = DateTime.UtcNow;
                }

                return track;
            })
            .ToList();
    }

    private static List<Playlist> SanitizeStoredPlaylists(
        IEnumerable<Playlist> playlists,
        IReadOnlyCollection<Track> validTracks,
        out bool playlistsChanged)
    {
        var validTrackIds = validTracks.Select(track => track.Id).ToHashSet(StringComparer.Ordinal);
        var sanitizedPlaylists = new List<Playlist>();
        playlistsChanged = false;

        foreach (var playlist in playlists)
        {
            var originalTrackCount = playlist.Tracks.Count;
            var cleanedTracks = playlist.Tracks
                .Where(track => validTrackIds.Contains(track.Id) && !IsLegacySeedTrack(track))
                .ToList();

            var isLegacySeedPlaylist =
                LegacySeedPlaylistTitles.Contains(playlist.Title) &&
                !string.IsNullOrWhiteSpace(playlist.Description) &&
                LegacySeedPlaylistDescriptions.Contains(playlist.Description);

            if (isLegacySeedPlaylist && cleanedTracks.Count == 0)
            {
                playlistsChanged = true;
                continue;
            }

            playlist.Tracks = cleanedTracks;
            sanitizedPlaylists.Add(playlist);

            if (cleanedTracks.Count != originalTrackCount)
            {
                playlistsChanged = true;
            }
        }

        if (sanitizedPlaylists.Count != playlists.Count())
        {
            playlistsChanged = true;
        }

        return sanitizedPlaylists;
    }

    private static bool IsLegacySeedTrack(Track track)
    {
        if (track.Source != TrackSource.Local)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(track.LocalFilePath))
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(track.ProviderTrackId) &&
               string.IsNullOrWhiteSpace(track.RemotePageUrl) &&
               !string.IsNullOrWhiteSpace(track.CoverArtUrl) &&
               track.CoverArtUrl.StartsWith("https://picsum.photos/seed/", StringComparison.OrdinalIgnoreCase);
    }
}
