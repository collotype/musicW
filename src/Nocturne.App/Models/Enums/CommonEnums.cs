namespace Nocturne.App.Models.Enums;

public enum TrackSource
{
    Local,
    SoundCloud,
    Spotify,
    Unknown
}

public enum StorageLocation
{
    Library,
    Temp,
    Remote
}

public enum RepeatMode
{
    Off,
    All,
    One
}

public enum SortMode
{
    RecentlyAdded,
    Alphabetical,
    Artist,
    Duration,
    Popularity
}

public enum CollectionType
{
    Library,
    Favorites,
    OfflineTracks,
    Playlist,
    RecentlyAdded,
    Artists,
    Albums
}

public enum SearchSourceFilter
{
    All,
    Local,
    SoundCloud,
    Spotify
}

public enum NavigationTarget
{
    Library,
    Search,
    Favorites,
    OfflineTracks,
    Settings
}

public enum NotificationLevel
{
    Info,
    Success,
    Warning,
    Error
}
