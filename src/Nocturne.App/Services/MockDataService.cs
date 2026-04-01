using Nocturne.App.Models;
using Nocturne.App.Models.Enums;

namespace Nocturne.App.Services;

public sealed class MockDataService : IMockDataService
{
    private const string ArtistHero = "https://images.unsplash.com/photo-1516280440614-37939bbacd81?auto=format&fit=crop&w=1800&q=80";
    private const string ArtistAvatar = "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?auto=format&fit=crop&w=900&q=80";
    private const string ArtistTwoHero = "https://images.unsplash.com/photo-1501386761578-eac5c94b800a?auto=format&fit=crop&w=1800&q=80";
    private const string ArtistTwoAvatar = "https://images.unsplash.com/photo-1506794778202-cad84cf45f1d?auto=format&fit=crop&w=900&q=80";
    private const string ArtistThreeHero = "https://images.unsplash.com/photo-1493225457124-a3eb161ffa5f?auto=format&fit=crop&w=1800&q=80";
    private const string ArtistThreeAvatar = "https://images.unsplash.com/photo-1504593811423-6dd665756598?auto=format&fit=crop&w=900&q=80";
    private const string AlbumCover = "https://images.unsplash.com/photo-1493225457124-a3eb161ffa5f?auto=format&fit=crop&w=1200&q=80";
    private const string AlbumTwoCover = "https://images.unsplash.com/photo-1511379938547-c1f69419868d?auto=format&fit=crop&w=1200&q=80";
    private const string AlbumThreeCover = "https://images.unsplash.com/photo-1511671782779-c97d3d27a1d4?auto=format&fit=crop&w=1200&q=80";
    private const string PlaylistCover = "https://images.unsplash.com/photo-1511379938547-c1f69419868d?auto=format&fit=crop&w=1200&q=80";
    private const string PlaylistTwoCover = "https://images.unsplash.com/photo-1516280030429-27679b3dc9cf?auto=format&fit=crop&w=1200&q=80";
    private const string PlaylistThreeCover = "https://images.unsplash.com/photo-1507838153414-b4b713384a76?auto=format&fit=crop&w=1200&q=80";

    public UserLibrary CreateInitialLibrary()
    {
        var artists = CreateArtists();
        var albums = CreateAlbums();
        var playlists = CreatePlaylists(albums);
        var allTracks = albums.SelectMany(album => album.Tracks)
            .Concat(playlists.SelectMany(playlist => playlist.Tracks))
            .DistinctBy(track => track.ProviderTrackId ?? track.Id)
            .ToList();

        return new UserLibrary
        {
            Tracks = new ObservableCollection<Track>(allTracks),
            Albums = new ObservableCollection<Album>(albums),
            Artists = new ObservableCollection<Artist>(artists),
            Playlists = new ObservableCollection<Playlist>(playlists),
            SearchHistory = ["night drive", "soundcloud ambient", "city pulse", "dark pop"],
            LastOpenedAt = DateTimeOffset.UtcNow
        };
    }

    public Artist CreateFeaturedArtist() => CreateArtists().First();

    public Album CreateFeaturedAlbum() => CreateAlbums().First();

    public Playlist CreateFeaturedPlaylist() => CreatePlaylists(CreateAlbums()).First();

    public SearchResults CreateSearchPreview(string query)
    {
        var trimmedQuery = string.IsNullOrWhiteSpace(query) ? "night drive" : query.Trim();
        var artists = CreateArtists();
        var albums = CreateAlbums();
        var playlists = CreatePlaylists(albums);
        var tracks = albums.SelectMany(album => album.Tracks)
            .Concat(playlists.SelectMany(playlist => playlist.Tracks))
            .DistinctBy(track => track.ProviderTrackId ?? track.Id)
            .Take(10)
            .ToList();

        return new SearchResults
        {
            Query = trimmedQuery,
            SourceFilter = SearchSourceFilter.All,
            Tracks = tracks,
            Artists = artists,
            Albums = albums,
            Playlists = playlists
        };
    }

    private static List<Artist> CreateArtists()
    {
        return
        [
            new Artist
            {
                Id = "artist-sable-echo",
                Name = "Sable Echo",
                Subtitle = "Electronic noir from Moscow",
                AvatarUrl = ArtistAvatar,
                HeaderImageUrl = ArtistHero,
                Country = "Russia",
                Followers = 840_200,
                MonthlyListeners = 2_460_000,
                TrackCount = 12,
                About = "Dense low-end, washed neon pads, and slow-burn hooks built for night streets.",
                Genres = ["Dark Pop", "Wave", "Electronic", "Alt-R&B"]
            },
            new Artist
            {
                Id = "artist-night-thesis",
                Name = "Night Thesis",
                Subtitle = "Steel-blue synth and after-hours percussion",
                AvatarUrl = ArtistTwoAvatar,
                HeaderImageUrl = ArtistTwoHero,
                Country = "Germany",
                Followers = 384_000,
                MonthlyListeners = 1_400_000,
                TrackCount = 9,
                About = "Cold percussion, cinematic drops, and melodic lines designed for long train rides.",
                Genres = ["Electronic", "Techno Pop", "Ambient Club"]
            },
            new Artist
            {
                Id = "artist-glass-mile",
                Name = "Glass Mile",
                Subtitle = "Luminous alt-pop for concrete skylines",
                AvatarUrl = ArtistThreeAvatar,
                HeaderImageUrl = ArtistThreeHero,
                Country = "United Kingdom",
                Followers = 215_000,
                MonthlyListeners = 980_000,
                TrackCount = 11,
                About = "Bright hooks and restrained pulse, with enough weight to stay cinematic.",
                Genres = ["Alt-Pop", "Electronic", "Indie Noir"]
            }
        ];
    }

    private static List<Album> CreateAlbums()
    {
        return
        [
            new Album
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
                Tracks = BuildAlbumTracks("Concrete Hearts", "Sable Echo", AlbumCover, ArtistAvatar, "se").ToList()
            },
            new Album
            {
                Id = "album-ghost-platform",
                Source = TrackSource.SoundCloud,
                StorageLocation = StorageLocation.Remote,
                Title = "Ghost Platform",
                ArtistName = "Night Thesis",
                CoverArtUrl = AlbumTwoCover,
                HeaderImageUrl = ArtistTwoHero,
                Description = "A stripped, metallic set built around blurred arps and low-end movement.",
                ReleaseDate = new DateTimeOffset(new DateTime(2024, 7, 19)),
                Genres = ["Ambient Club", "Electronic"],
                Tracks = BuildNightThesisTracks().ToList()
            },
            new Album
            {
                Id = "album-after-image",
                Source = TrackSource.Local,
                StorageLocation = StorageLocation.Library,
                Title = "After Image",
                ArtistName = "Glass Mile",
                CoverArtUrl = AlbumThreeCover,
                HeaderImageUrl = ArtistThreeHero,
                Description = "Muted hooks, long shadows, and a sense of dawn breaking through static.",
                ReleaseDate = new DateTimeOffset(new DateTime(2026, 1, 30)),
                Genres = ["Alt-Pop", "Indie Noir"],
                Tracks = BuildGlassMileTracks().ToList()
            }
        ];
    }

    private static List<Playlist> CreatePlaylists(IEnumerable<Album> albums)
    {
        var albumList = albums.ToList();
        var concreteHearts = albumList[0].Tracks;
        var ghostPlatform = albumList[1].Tracks;
        var afterImage = albumList[2].Tracks;

        return
        [
            new Playlist
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
                    concreteHearts[0],
                    ghostPlatform[0],
                    afterImage[1],
                    ghostPlatform[2],
                    concreteHearts[4],
                    afterImage[4]
                ])
            },
            new Playlist
            {
                Id = "playlist-offline-vault",
                Title = "Offline Vault",
                OwnerName = "Collotype",
                Description = "Saved to disk for flights, basements, and signal dead zones.",
                CoverArtUrl = PlaylistTwoCover,
                IsEditable = true,
                IsOffline = true,
                Tracks = new ObservableCollection<Track>(
                [
                    CloneTrack(concreteHearts[0], true, true),
                    CloneTrack(concreteHearts[1], false, true),
                    CloneTrack(afterImage[0], true, true),
                    CloneTrack(afterImage[3], false, true),
                    CloneTrack(concreteHearts[5], false, true)
                ])
            },
            new Playlist
            {
                Id = "playlist-metadata-drift",
                Title = "Metadata Drift",
                OwnerName = "Guest Curator",
                Description = "A mixed-source curation that blends playable tracks with Spotify references.",
                CoverArtUrl = PlaylistThreeCover,
                IsEditable = false,
                IsOffline = false,
                Tracks = new ObservableCollection<Track>(
                [
                    CloneTrack(ghostPlatform[1]),
                    CloneTrack(afterImage[2]),
                    CloneTrack(concreteHearts[3]),
                    BuildTrack("Cold Stations", "Glass Mile", "After Hours Circuit", 244, TrackSource.Spotify, coverArtUrl: PlaylistThreeCover),
                    BuildTrack("Harborline", "Night Thesis", "Ghost Platform", 232, TrackSource.Spotify, coverArtUrl: AlbumTwoCover),
                    CloneTrack(afterImage[5])
                ])
            }
        ];
    }

    private static IEnumerable<Track> BuildAlbumTracks(string albumTitle, string artistName, string coverArtUrl, string artistImageUrl, string prefix)
    {
        yield return BuildTrack("Velvet Artery", artistName, albumTitle, 214, TrackSource.Local, liked: true, downloaded: true, coverArtUrl: coverArtUrl, artistImageUrl: artistImageUrl, providerTrackId: $"{prefix}-01", genres: ["Dark Pop", "Wave"]);
        yield return BuildTrack("City Static", artistName, albumTitle, 197, TrackSource.Local, downloaded: true, coverArtUrl: coverArtUrl, artistImageUrl: artistImageUrl, providerTrackId: $"{prefix}-02", genres: ["Electronic"]);
        yield return BuildTrack("Neon Tax", artistName, albumTitle, 245, TrackSource.SoundCloud, coverArtUrl: coverArtUrl, artistImageUrl: artistImageUrl, providerTrackId: "1672459458", genres: ["Wave"]);
        yield return BuildTrack("Soft Exit", artistName, albumTitle, 208, TrackSource.Local, coverArtUrl: coverArtUrl, artistImageUrl: artistImageUrl, providerTrackId: $"{prefix}-04", genres: ["Alt-R&B"]);
        yield return BuildTrack("Midtown Signal", artistName, albumTitle, 262, TrackSource.Spotify, coverArtUrl: coverArtUrl, artistImageUrl: artistImageUrl, providerTrackId: $"{prefix}-05", genres: ["Electronic"]);
        yield return BuildTrack("Tunnel Bloom", artistName, albumTitle, 233, TrackSource.Local, downloaded: true, coverArtUrl: coverArtUrl, artistImageUrl: artistImageUrl, providerTrackId: $"{prefix}-06", genres: ["Dark Pop"]);
    }

    private static IEnumerable<Track> BuildNightThesisTracks()
    {
        yield return BuildTrack("Monorail", "Night Thesis", "Ghost Platform", 198, TrackSource.SoundCloud, coverArtUrl: AlbumTwoCover, artistImageUrl: ArtistTwoAvatar, providerTrackId: "nt-01", genres: ["Electronic"]);
        yield return BuildTrack("Harborline", "Night Thesis", "Ghost Platform", 232, TrackSource.Spotify, coverArtUrl: AlbumTwoCover, artistImageUrl: ArtistTwoAvatar, providerTrackId: "nt-02", genres: ["Ambient Club"]);
        yield return BuildTrack("Signal Bloom", "Night Thesis", "Ghost Platform", 203, TrackSource.SoundCloud, coverArtUrl: AlbumTwoCover, artistImageUrl: ArtistTwoAvatar, providerTrackId: "nt-03", genres: ["Ambient Club"]);
        yield return BuildTrack("Blue Concrete", "Night Thesis", "Ghost Platform", 241, TrackSource.SoundCloud, coverArtUrl: AlbumTwoCover, artistImageUrl: ArtistTwoAvatar, providerTrackId: "nt-04", genres: ["Electronic"]);
        yield return BuildTrack("Terminal Sleep", "Night Thesis", "Ghost Platform", 224, TrackSource.Local, coverArtUrl: AlbumTwoCover, artistImageUrl: ArtistTwoAvatar, providerTrackId: "nt-05", genres: ["Electronic"]);
        yield return BuildTrack("Factory Glow", "Night Thesis", "Ghost Platform", 212, TrackSource.Local, downloaded: true, coverArtUrl: AlbumTwoCover, artistImageUrl: ArtistTwoAvatar, providerTrackId: "nt-06", genres: ["Electronic"]);
    }

    private static IEnumerable<Track> BuildGlassMileTracks()
    {
        yield return BuildTrack("Chrome Silence", "Glass Mile", "After Image", 221, TrackSource.Local, liked: true, coverArtUrl: AlbumThreeCover, artistImageUrl: ArtistThreeAvatar, providerTrackId: "gm-01", genres: ["Alt-Pop"]);
        yield return BuildTrack("Backlit", "Glass Mile", "After Image", 238, TrackSource.Local, downloaded: true, coverArtUrl: AlbumThreeCover, artistImageUrl: ArtistThreeAvatar, providerTrackId: "gm-02", genres: ["Indie Noir"]);
        yield return BuildTrack("Cold Stations", "Glass Mile", "After Image", 244, TrackSource.Spotify, coverArtUrl: AlbumThreeCover, artistImageUrl: ArtistThreeAvatar, providerTrackId: "gm-03", genres: ["Alt-Pop"]);
        yield return BuildTrack("Silver Wire", "Glass Mile", "After Image", 207, TrackSource.Local, coverArtUrl: AlbumThreeCover, artistImageUrl: ArtistThreeAvatar, providerTrackId: "gm-04", genres: ["Indie Noir"]);
        yield return BuildTrack("Paper Halo", "Glass Mile", "After Image", 226, TrackSource.SoundCloud, coverArtUrl: AlbumThreeCover, artistImageUrl: ArtistThreeAvatar, providerTrackId: "gm-05", genres: ["Alt-Pop"]);
        yield return BuildTrack("Eastbound", "Glass Mile", "After Image", 248, TrackSource.Local, downloaded: true, coverArtUrl: AlbumThreeCover, artistImageUrl: ArtistThreeAvatar, providerTrackId: "gm-06", genres: ["Alt-Pop"]);
    }

    private static Track CloneTrack(Track source, bool? liked = null, bool? downloaded = null)
    {
        return new Track
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = source.Title,
            ArtistName = source.ArtistName,
            AlbumTitle = source.AlbumTitle,
            Duration = source.Duration,
            CoverArtUrl = source.CoverArtUrl,
            ArtistImageUrl = source.ArtistImageUrl,
            Source = source.Source,
            StorageLocation = downloaded == true ? StorageLocation.Library : source.StorageLocation,
            ProviderTrackId = source.ProviderTrackId,
            ProviderArtistId = source.ProviderArtistId,
            ProviderAlbumId = source.ProviderAlbumId,
            IsLiked = liked ?? source.IsLiked,
            IsDownloaded = downloaded ?? source.IsDownloaded,
            Genres = source.Genres.ToList(),
            Tags = source.Tags.ToList(),
            ReleaseDate = source.ReleaseDate
        };
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
        string? artistImageUrl = null,
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
            ArtistImageUrl = artistImageUrl ?? ArtistAvatar,
            Source = source,
            ProviderTrackId = providerTrackId,
            ProviderArtistId = $"artist-{artistName.ToLowerInvariant().Replace(' ', '-')}",
            ProviderAlbumId = $"album-{albumTitle.ToLowerInvariant().Replace(' ', '-')}",
            StorageLocation = downloaded || source == TrackSource.Local ? StorageLocation.Library : StorageLocation.Remote,
            IsLiked = liked,
            IsDownloaded = downloaded,
            Genres = genres?.ToList() ?? ["Electronic"],
            Tags = ["late night", "premium", "dark"],
            ReleaseDate = new DateTimeOffset(new DateTime(2025, 10, 11))
        };
    }
}
