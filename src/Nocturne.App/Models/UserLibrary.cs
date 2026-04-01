namespace Nocturne.App.Models;

public sealed class UserLibrary
{
    public ObservableCollection<Track> Tracks { get; set; } = [];

    public ObservableCollection<Playlist> Playlists { get; set; } = [];

    public ObservableCollection<Album> Albums { get; set; } = [];

    public ObservableCollection<Artist> Artists { get; set; } = [];

    public List<string> SearchHistory { get; set; } = [];

    public DateTimeOffset LastOpenedAt { get; set; } = DateTimeOffset.UtcNow;

    public string? LastSelectedPlaylistId { get; set; }
}
