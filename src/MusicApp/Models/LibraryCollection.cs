using MusicApp.Enums;

namespace MusicApp.Models;

public class LibraryCollection
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public CollectionType Type { get; set; }
    public string? Description { get; set; }
    public string? CoverArtUrl { get; set; }
    public int ItemCount { get; set; }
    public DateTime? LastAccessed { get; set; }
    public bool IsSystem { get; set; }

    public static LibraryCollection CreateFavorites(int itemCount)
    {
        return new LibraryCollection
        {
            Id = "favorites",
            Title = "Liked Songs",
            Type = CollectionType.Favorites,
            ItemCount = itemCount,
            IsSystem = true
        };
    }

    public static LibraryCollection CreateOffline(int itemCount)
    {
        return new LibraryCollection
        {
            Id = "offline",
            Title = "Offline Tracks",
            Type = CollectionType.Offline,
            ItemCount = itemCount,
            IsSystem = true
        };
    }

    public static LibraryCollection CreateRecent(int itemCount)
    {
        return new LibraryCollection
        {
            Id = "recent",
            Title = "Recently Played",
            Type = CollectionType.Recent,
            ItemCount = itemCount,
            IsSystem = true
        };
    }

    public static LibraryCollection CreatePlaylist(string title, int itemCount)
    {
        return new LibraryCollection
        {
            Title = title,
            Type = CollectionType.Playlist,
            ItemCount = itemCount,
            IsSystem = false
        };
    }
}
