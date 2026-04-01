using Nocturne.App.Models;
using Nocturne.App.Models.Enums;

namespace Nocturne.App.Services;

public sealed class MockDataService : IMockDataService
{
    private const string ArtistHero = "https://images.unsplash.com/photo-1516280440614-37939bbacd81?auto=format&fit=crop&w=1400&q=80";
    private const string ArtistAvatar = "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?auto=format&fit=crop&w=600&q=80";
    private const string AlbumCover = "https://images.unsplash.com/photo-1493225457124-a3eb161ffa5f?auto=format&fit=crop&w=900&q=80";
    private const string PlaylistCover = "https://images.unsplash.com/photo-1511379938547-c1f69419868d?auto=format&fit=crop&w=900&q=80";
    private const string RelatedArtistImage = "https://images.unsplash.com/photo-1506794778202-cad84cf45f1d?auto=format&fit=crop&w=600&q=80";

    public UserLibrary CreateInitialLibrary()
    {
        var artist = CreateFeaturedArtist();
        var album = CreateFeaturedAlbum();
        var playlist = CreateFeaturedPlaylist();

        return new UserLibrary
        {
            Tracks = new ObservableCollection<Track>(album.Tracks.Concat(playlist.Tracks).DistinctBy(track => track.Title)),
            Albums = new ObservableCollection<Album>([album]),
            Artists = new ObservableCollection<Artist>([artist]),
            Playlists = new ObservableCollection<Playlist>([playlist]),
            SearchHistory = ["night drive", "soundcloud ambient", "city pulse"]
        };
    }

    public Artist CreateFeaturedArtist()
    {
        var tracks = BuildAlbumTracks().ToList();
        var album = CreateFeaturedAlbum();

        return new Artist
        {
            Id = "artist-sable-echo",
            Name = "Sable Echo",
            Subtitle = "Electronic noir from Moscow",
            AvatarUrl = ArtistAvatar,
            HeaderImageUrl = ArtistHero,
            Country = "Russia",
            Followers = 840_200,
            MonthlyListeners = 2_460_000,
            TrackCount = tracks.Count,
            About = "Dense low-end, washed neon pads, and slow-burn hooks built for night streets.",
            Genres = ["Dark Pop", "Wave", "Electronic", "Alt-R&B"],
            TopTracks = tracks,
            Albums = [album],
            RelatedArtists =
            [
                new Artist { Id = "artist-night-thesis", Name = "Night Thesis", AvatarUrl = RelatedArtistImage, MonthlyListeners = 1_400_000, Followers = 384_000 },
                new Artist { Id = "artist-glass-mile", Name = "Glass Mile", AvatarUrl = ArtistAvatar, MonthlyListeners = 980_000, Followers = 215_000 },
                new Artist { Id = "artist-fade-loop", Name = "Fade Loop", AvatarUrl = RelatedArtistImage, MonthlyListeners = 1_220_000, Followers = 301_000 }
            ]
        };
    }

    public Album CreateFeaturedAlbum()
    {
        return new Album
        {
            Id = "album-concrete-hearts",
            Source = TrackSource.Local,
            StorageLocation = StorageLocation.Library,
            Title = "Concrete Hearts",
            ArtistName = "Sable Echo",
            CoverArtUrl = AlbumCover,
            HeaderImageUrl = ArtistHero,
            Description = "A cold-glow record of midnight drums, tactile synths, and vocal fragments.",
            ReleaseDate = new DateTimeOffset(new DateTime(2025, 10, 11)),
            Genres = ["Dark Pop", "Wave"],
            Tracks = BuildAlbumTracks().ToList()
        };
    }

    public Playlist CreateFeaturedPlaylist()
    {
        return new Playlist
        {
            Id = "playlist-after-hours",
            Title = "After Hours Circuit",
            OwnerName = "Collotype",
            Description = "Low-lit drivers, warehouse pulse, and post-midnight melodies.",
            CoverArtUrl = PlaylistCover,
            IsEditable = true,
            IsOffline = false,
            Tracks = new ObservableCollection<Track>(
            [
                BuildTrack("Chrome Silence", "Sable Echo", "After Hours Circuit", 221, TrackSource.Local, liked: true, downloaded: true, coverArtUrl: PlaylistCover),
                BuildTrack("Monorail", "Night Thesis", "After Hours Circuit", 198, TrackSource.SoundCloud, coverArtUrl: AlbumCover),
                BuildTrack("Cold Stations", "Glass Mile", "After Hours Circuit", 244, TrackSource.Spotify, coverArtUrl: ArtistHero),
                BuildTrack("Signal Bloom", "Fade Loop", "After Hours Circuit", 203, TrackSource.SoundCloud, coverArtUrl: PlaylistCover),
                BuildTrack("Backlit", "Sable Echo", "After Hours Circuit", 238, TrackSource.Local, downloaded: true, coverArtUrl: AlbumCover)
            ])
        };
    }

    public SearchResults CreateSearchPreview(string query)
    {
        var artist = CreateFeaturedArtist();
        var album = CreateFeaturedAlbum();
        var playlist = CreateFeaturedPlaylist();

        return new SearchResults
        {
            Query = query,
            SourceFilter = SearchSourceFilter.All,
            Tracks = album.Tracks.Take(5).ToList(),
            Artists = [artist],
            Albums = [album],
            Playlists = [playlist]
        };
    }

    private static IEnumerable<Track> BuildAlbumTracks()
    {
        yield return BuildTrack("Velvet Artery", "Sable Echo", "Concrete Hearts", 214, TrackSource.Local, liked: true, downloaded: true, coverArtUrl: AlbumCover, genres: ["Dark Pop", "Wave"]);
        yield return BuildTrack("City Static", "Sable Echo", "Concrete Hearts", 197, TrackSource.Local, downloaded: true, coverArtUrl: AlbumCover, genres: ["Electronic"]);
        yield return BuildTrack("Neon Tax", "Sable Echo", "Concrete Hearts", 245, TrackSource.SoundCloud, coverArtUrl: AlbumCover, providerTrackId: "1672459458");
        yield return BuildTrack("Soft Exit", "Sable Echo", "Concrete Hearts", 208, TrackSource.Local, coverArtUrl: AlbumCover, genres: ["Alt-R&B"]);
        yield return BuildTrack("Midtown Signal", "Sable Echo", "Concrete Hearts", 262, TrackSource.Spotify, coverArtUrl: AlbumCover);
        yield return BuildTrack("Tunnel Bloom", "Sable Echo", "Concrete Hearts", 233, TrackSource.Local, downloaded: true, coverArtUrl: AlbumCover);
    }

    private static Track BuildTrack(
        string title,
        string artistName,
        string albumTitle,
        int seconds,
        TrackSource source,
        bool liked = false,
        bool downloaded = false,
        string? coverArtUrl = null,
        string? providerTrackId = null,
        IReadOnlyList<string>? genres = null)
    {
        return new Track
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = title,
            ArtistName = artistName,
            AlbumTitle = albumTitle,
            Duration = TimeSpan.FromSeconds(seconds),
            CoverArtUrl = coverArtUrl,
            ArtistImageUrl = ArtistAvatar,
            Source = source,
            ProviderTrackId = providerTrackId,
            StorageLocation = source == TrackSource.Local ? StorageLocation.Library : StorageLocation.Remote,
            IsLiked = liked,
            IsDownloaded = downloaded,
            Genres = genres?.ToList() ?? ["Electronic"],
            Tags = ["late night", "premium", "dark"],
            ReleaseDate = new DateTimeOffset(new DateTime(2025, 10, 11))
        };
    }
}
