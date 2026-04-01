namespace MusicApp.Enums;

public enum TrackSource
{
    Unknown = 0,
    Local = 1,
    SoundCloud = 2,
    Spotify = 3
}

public enum StorageLocation
{
    Library = 0,
    Temp = 1,
    Remote = 2
}

public enum RepeatMode
{
    None = 0,
    All = 1,
    One = 2
}

public enum CollectionType
{
    Playlist = 0,
    Album = 1,
    Artist = 2,
    Favorites = 3,
    Offline = 4,
    Recent = 5
}

public enum SearchResultType
{
    All = 0,
    Tracks = 1,
    Artists = 2,
    Albums = 3,
    Playlists = 4
}

public enum PlaybackStatus
{
    Stopped = 0,
    Playing = 1,
    Paused = 2
}

public enum NavigationPage
{
    Library = 0,
    Search = 1,
    Artist = 2,
    Album = 3,
    Playlist = 4,
    Settings = 5
}
