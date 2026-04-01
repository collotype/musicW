namespace MusicApp.Models;

public class SearchResults
{
    public string Query { get; set; } = string.Empty;
    public List<Track> Tracks { get; set; } = new();
    public List<Artist> Artists { get; set; } = new();
    public List<Album> Albums { get; set; } = new();
    public List<Playlist> Playlists { get; set; } = new();
    public bool HasMoreTracks { get; set; }
    public bool HasMoreArtists { get; set; }
    public bool HasMoreAlbums { get; set; }
    public bool HasMorePlaylists { get; set; }
    public string? ErrorMessage { get; set; }

    public bool HasAnyResults => Tracks.Count > 0 || Artists.Count > 0 || Albums.Count > 0 || Playlists.Count > 0;
}
