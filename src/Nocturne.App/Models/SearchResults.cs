using Nocturne.App.Models.Enums;

namespace Nocturne.App.Models;

public sealed class SearchResults
{
    public string Query { get; set; } = string.Empty;

    public SearchSourceFilter SourceFilter { get; set; } = SearchSourceFilter.All;

    public List<Track> Tracks { get; set; } = [];

    public List<Artist> Artists { get; set; } = [];

    public List<Album> Albums { get; set; } = [];

    public List<Playlist> Playlists { get; set; } = [];

    public bool IsEmpty =>
        Tracks.Count == 0 &&
        Artists.Count == 0 &&
        Albums.Count == 0 &&
        Playlists.Count == 0;
}
