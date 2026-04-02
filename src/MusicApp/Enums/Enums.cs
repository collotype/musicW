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
    Local = 1,
    SoundCloud = 2
}

public enum SearchTab
{
    Tracks = 0,
    Artists = 1,
    Albums = 2,
    Playlists = 3
}

public enum PlaybackStatus
{
    Stopped = 0,
    Playing = 1,
    Paused = 2
}

public enum NavigationPage
{
    Home = 0,
    MyWave = 1,
    Search = 2,
    Library = 3,
    Queue = 4,
    Artist = 5,
    Album = 6,
    Playlist = 7,
    Settings = 8
}

public enum LibrarySection
{
    Overview = 0,
    AllTracks = 1,
    LikedTracks = 2,
    FavoriteArtists = 3,
    Albums = 4,
    Playlists = 5,
    Offline = 6,
    RecentlyPlayed = 7,
    Pinned = 8
}

public enum ContextPanelMode
{
    Queue = 0,
    Lyrics = 1,
    Details = 2,
    Comments = 3,
    Wave = 4
}

public enum MyWavePresentation
{
    Flow = 0,
    Snippets = 1
}

public enum WaveSeedType
{
    Home = 0,
    Track = 1,
    Artist = 2,
    Album = 3,
    Playlist = 4,
    Library = 5
}
