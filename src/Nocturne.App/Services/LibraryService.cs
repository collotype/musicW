using Nocturne.App.Helpers;
using Nocturne.App.Models;
using Nocturne.App.Models.Enums;
using Nocturne.App.Persistence;

namespace Nocturne.App.Services;

public sealed class LibraryService(
    ISettingsService settingsService,
    IMockDataService mockDataService,
    ILocalMusicScannerService localMusicScannerService) : ILibraryService
{
    public UserLibrary Library { get; private set; } = new();

    public event EventHandler? LibraryChanged;

    public async Task InitializeAsync()
    {
        Library = await JsonFileStore.ReadAsync<UserLibrary>(AppPaths.LibraryFile) ?? mockDataService.CreateInitialLibrary();
        await SaveAsync();

        _ = Task.Run(async () =>
        {
            try
            {
                var scannedTracks = await localMusicScannerService.ScanAsync(settingsService.Current.LocalMusicFolders, CancellationToken.None);
                if (scannedTracks.Count > 0)
                {
                    var existingKeys = new HashSet<string>(
                        Library.Tracks.Select(track => track.LocalFilePath ?? track.Id),
                        StringComparer.OrdinalIgnoreCase);

                    foreach (var track in scannedTracks.Where(track => !existingKeys.Contains(track.LocalFilePath ?? track.Id)))
                    {
                        Library.Tracks.Add(track);
                    }

                    await SaveAsync();
                    LibraryChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            catch
            {
            }
        });
    }

    public IReadOnlyList<Track> GetFavorites() =>
        Library.Tracks.Where(track => track.IsLiked).OrderByDescending(track => track.AddedAt).ToList();

    public IReadOnlyList<Track> GetOfflineTracks() =>
        Library.Tracks.Where(track => track.IsDownloaded || track.StorageLocation == StorageLocation.Library).OrderBy(track => track.Title).ToList();

    public IReadOnlyList<Track> GetRecentlyAdded(int take = 12) =>
        Library.Tracks.OrderByDescending(track => track.AddedAt).Take(take).ToList();

    public IReadOnlyList<Album> GetAlbums() => Library.Albums.ToList();

    public IReadOnlyList<Artist> GetArtists() => Library.Artists.ToList();

    public IReadOnlyList<Playlist> GetPlaylists() => Library.Playlists.ToList();

    public async Task ToggleLikeAsync(Track track)
    {
        track.IsLiked = !track.IsLiked;
        await EnsureTrackPresenceAsync(track);
        await SaveAsync();
        LibraryChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task AddTrackToLibraryAsync(Track track)
    {
        track.StorageLocation = StorageLocation.Library;
        await EnsureTrackPresenceAsync(track);
        await SaveAsync();
        LibraryChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task AddPlaylistAsync(Playlist playlist)
    {
        if (Library.Playlists.All(existing => existing.Id != playlist.Id))
        {
            Library.Playlists.Add(playlist);
        }

        await SaveAsync();
        LibraryChanged?.Invoke(this, EventArgs.Empty);
    }

    public Artist? FindArtist(string artistId) => Library.Artists.FirstOrDefault(artist => artist.Id == artistId);

    public Album? FindAlbum(string albumId) => Library.Albums.FirstOrDefault(album => album.Id == albumId);

    public Playlist? FindPlaylist(string playlistId) => Library.Playlists.FirstOrDefault(playlist => playlist.Id == playlistId);

    public Track? FindTrack(string trackId) => Library.Tracks.FirstOrDefault(track => track.Id == trackId);

    public IReadOnlyList<SidebarCollectionItem> BuildSidebarCollections()
    {
        var items = new List<SidebarCollectionItem>
        {
            new() { Id = "library", Title = "Library", Subtitle = "Overview", Type = CollectionType.Library, Glyph = "\uE8F1" },
            new() { Id = "favorites", Title = "Favorites", Subtitle = $"{GetFavorites().Count} liked", Type = CollectionType.Favorites, Glyph = "\uEB51" },
            new() { Id = "offline", Title = "Offline Tracks", Subtitle = $"{GetOfflineTracks().Count} saved", Type = CollectionType.OfflineTracks, Glyph = "\uE8FE" }
        };

        items.AddRange(
            Library.Playlists.Select(playlist => new SidebarCollectionItem
            {
                Id = playlist.Id,
                Title = playlist.Title,
                Subtitle = playlist.OwnerName,
                CoverArtUrl = playlist.CoverArtUrl,
                Type = CollectionType.Playlist,
                Glyph = "\uE142"
            }));

        return items;
    }

    private Task EnsureTrackPresenceAsync(Track track)
    {
        var existing = Library.Tracks.FirstOrDefault(candidate =>
            candidate.Id == track.Id ||
            (!string.IsNullOrWhiteSpace(candidate.ProviderTrackId) && candidate.ProviderTrackId == track.ProviderTrackId) ||
            (!string.IsNullOrWhiteSpace(candidate.LocalFilePath) && candidate.LocalFilePath == track.LocalFilePath));

        if (existing is null)
        {
            Library.Tracks.Add(CloneTrack(track));
            return Task.CompletedTask;
        }

        existing.IsLiked = track.IsLiked;
        existing.IsDownloaded = track.IsDownloaded;
        existing.StorageLocation = track.StorageLocation;
        existing.StreamUrl = track.StreamUrl;
        return Task.CompletedTask;
    }

    private static Track CloneTrack(Track source)
    {
        return new Track
        {
            Id = source.Id,
            Title = source.Title,
            ArtistName = source.ArtistName,
            AlbumTitle = source.AlbumTitle,
            Duration = source.Duration,
            CoverArtUrl = source.CoverArtUrl,
            ArtistImageUrl = source.ArtistImageUrl,
            LocalFilePath = source.LocalFilePath,
            RemotePageUrl = source.RemotePageUrl,
            ProviderTrackId = source.ProviderTrackId,
            ProviderArtistId = source.ProviderArtistId,
            ProviderAlbumId = source.ProviderAlbumId,
            ProviderPlaylistId = source.ProviderPlaylistId,
            ProviderTrackUrn = source.ProviderTrackUrn,
            TrackAuthorization = source.TrackAuthorization,
            PlaybackCandidates = source.PlaybackCandidates,
            Source = source.Source,
            StorageLocation = source.StorageLocation,
            Genres = source.Genres.ToList(),
            Tags = source.Tags.ToList(),
            IsLiked = source.IsLiked,
            IsDownloaded = source.IsDownloaded,
            StreamUrl = source.StreamUrl,
            ReleaseDate = source.ReleaseDate,
            AddedAt = source.AddedAt,
            PlaybackCount = source.PlaybackCount
        };
    }

    private Task SaveAsync() => JsonFileStore.WriteAsync(AppPaths.LibraryFile, Library);
}
