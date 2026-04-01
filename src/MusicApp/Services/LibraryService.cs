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

    public event EventHandler? LibraryChanged;

    public List<Track> AllTracks => _tracks;
    public List<Artist> AllArtists => _artists;
    public List<Album> AllAlbums => _albums;
    public List<Playlist> Playlists => _playlists;
    public List<Track> LikedTracks => _tracks.Where(t => t.IsLiked).ToList();
    public List<Track> OfflineTracks => _tracks.Where(t => t.IsDownloaded).ToList();

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

        RebuildArtistsAndAlbums();

        if (librarySanitized)
        {
            _dataStore.Tracks = _tracks.ToList();
            _dataStore.Playlists = _playlists.ToList();
            await _dataStore.SaveAllAsync();
        }

        LibraryChanged?.Invoke(this, EventArgs.Empty);
    }

    public Task AddTrackAsync(Track track)
    {
        var existing = _tracks.FirstOrDefault(t => t.Id == track.Id);
        if (existing != null)
        {
            var index = _tracks.IndexOf(existing);
            _tracks[index] = track;
        }
        else
        {
            _tracks.Add(track);
        }

        RebuildArtistsAndAlbums();
        _dataStore.Tracks = _tracks;
        LibraryChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task RemoveTrackAsync(string trackId)
    {
        var track = _tracks.FirstOrDefault(t => t.Id == trackId);
        if (track != null)
        {
            _tracks.Remove(track);
            RebuildArtistsAndAlbums();
            _dataStore.Tracks = _tracks;
            LibraryChanged?.Invoke(this, EventArgs.Empty);
        }
        return Task.CompletedTask;
    }

    public async Task ToggleLikeAsync(string trackId)
    {
        var track = _tracks.FirstOrDefault(t => t.Id == trackId);
        if (track != null)
        {
            track.IsLiked = !track.IsLiked;
            await AddTrackAsync(track);
        }
    }

    public Task<bool> IsLikedAsync(string trackId)
    {
        var track = _tracks.FirstOrDefault(t => t.Id == trackId);
        return Task.FromResult(track?.IsLiked ?? false);
    }

    public Task CreatePlaylistAsync(string title, string? description = null)
    {
        var playlist = new Playlist
        {
            Title = title,
            Description = description,
            OwnerName = "You"
        };
        _playlists.Add(playlist);
        _dataStore.Playlists = _playlists;
        LibraryChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public async Task AddToPlaylistAsync(string playlistId, Track track)
    {
        var playlist = _playlists.FirstOrDefault(p => p.Id == playlistId);
        if (playlist != null && !playlist.Tracks.Any(t => t.Id == track.Id))
        {
            playlist.Tracks.Add(track);
            playlist.LastModifiedDate = DateTime.Now;
            _dataStore.Playlists = _playlists;
            LibraryChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task RemoveFromPlaylistAsync(string playlistId, string trackId)
    {
        var playlist = _playlists.FirstOrDefault(p => p.Id == playlistId);
        if (playlist != null)
        {
            var track = playlist.Tracks.FirstOrDefault(t => t.Id == trackId);
            if (track != null)
            {
                playlist.Tracks.Remove(track);
                playlist.LastModifiedDate = DateTime.Now;
                _dataStore.Playlists = _playlists;
                LibraryChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public async Task DeletePlaylistAsync(string playlistId)
    {
        var playlist = _playlists.FirstOrDefault(p => p.Id == playlistId);
        if (playlist != null && !playlist.IsSystemPlaylist)
        {
            _playlists.Remove(playlist);
            _dataStore.Playlists = _playlists;
            LibraryChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public Task<Playlist?> GetPlaylistAsync(string playlistId)
    {
        var playlist = _playlists.FirstOrDefault(p => p.Id == playlistId);
        return Task.FromResult(playlist);
    }

    public Task<Artist?> GetArtistAsync(string artistId)
    {
        var artist = _artists.FirstOrDefault(a => a.Id == artistId);
        return Task.FromResult(artist);
    }

    public Task<Album?> GetAlbumAsync(string albumId)
    {
        var album = _albums.FirstOrDefault(a => a.Id == albumId);
        return Task.FromResult(album);
    }

    private void RebuildArtistsAndAlbums()
    {
        // Build artists
        _artists.Clear();
        var artistGroups = _tracks.GroupBy(t => t.ArtistName);
        foreach (var group in artistGroups)
        {
            var artist = new Artist
            {
                Id = group.First().ArtistId,
                Name = group.Key,
                TrackCount = group.Count(),
                TopTracks = group.OrderByDescending(t => t.PlayCount ?? 0).Take(5).ToList(),
                Genres = group.SelectMany(t => t.Genres).Distinct().ToList()
            };
            _artists.Add(artist);
        }

        // Build albums
        _albums.Clear();
        var albumGroups = _tracks.GroupBy(t => t.AlbumTitle).Where(g => !string.IsNullOrEmpty(g.Key));
        foreach (var group in albumGroups)
        {
            var firstTrack = group.First();
            var album = new Album
            {
                Id = firstTrack.AlbumId,
                Title = group.Key,
                ArtistName = firstTrack.ArtistName,
                ArtistId = firstTrack.ArtistId,
                CoverArtUrl = firstTrack.CoverArtUrl,
                ReleaseDate = firstTrack.ReleaseDate,
                Tracks = group.OrderBy(t => t.DiscNumber).ThenBy(t => t.TrackNumber).ToList(),
                Genres = group.SelectMany(t => t.Genres).Distinct().ToList()
            };
            _albums.Add(album);
        }
    }

    private static List<Track> SanitizeStoredTracks(IEnumerable<Track> tracks)
    {
        return tracks
            .Where(track => !IsLegacySeedTrack(track))
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
