using Nocturne.App.Models;

namespace Nocturne.App.Services;

public interface ILibraryService
{
    UserLibrary Library { get; }

    event EventHandler? LibraryChanged;

    Task InitializeAsync();

    IReadOnlyList<Track> GetFavorites();

    IReadOnlyList<Track> GetOfflineTracks();

    IReadOnlyList<Track> GetRecentlyAdded(int take = 12);

    IReadOnlyList<Album> GetAlbums();

    IReadOnlyList<Artist> GetArtists();

    IReadOnlyList<Playlist> GetPlaylists();

    Task ToggleLikeAsync(Track track);

    Task AddTrackToLibraryAsync(Track track);

    Task AddPlaylistAsync(Playlist playlist);

    Artist? FindArtist(string artistId);

    Album? FindAlbum(string albumId);

    Playlist? FindPlaylist(string playlistId);

    Track? FindTrack(string trackId);

    IReadOnlyList<LibraryCollection> BuildSidebarCollections();
}
