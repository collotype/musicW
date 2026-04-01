using MusicApp.Enums;
using MusicApp.Models;
using MusicApp.Services;

namespace MusicApp.Data;

public static class MockDataSeeder
{
    public static async Task SeedAsync(ILibraryService libraryService)
    {
        // Seed Artists
        var artists = GetMockArtists();
        foreach (var artist in artists)
        {
            // Add artist's tracks to library which will rebuild artists
            foreach (var track in artist.TopTracks)
            {
                await libraryService.AddTrackAsync(track);
            }
        }

        // Seed Albums
        var albums = GetMockAlbums();
        foreach (var album in albums)
        {
            foreach (var track in album.Tracks)
            {
                await libraryService.AddTrackAsync(track);
            }
        }

        // Seed Playlists
        await libraryService.CreatePlaylistAsync("Chill Vibes", "Perfect for relaxing");
        await libraryService.CreatePlaylistAsync("Workout Mix", "High energy tracks");
        await libraryService.CreatePlaylistAsync("Focus Mode", "Deep work music");

        var playlists = libraryService.Playlists;
        if (playlists.Count >= 3)
        {
            // Add some tracks to playlists
            var tracks = libraryService.AllTracks.Take(10).ToList();
            foreach (var track in tracks)
            {
                await libraryService.AddToPlaylistAsync(playlists[0].Id, track);
            }
            foreach (var track in tracks.Skip(5).Take(5))
            {
                await libraryService.AddToPlaylistAsync(playlists[1].Id, track);
            }
        }

        // Like some tracks
        var tracksToLike = libraryService.AllTracks.Take(15).ToList();
        foreach (var track in tracksToLike)
        {
            await libraryService.ToggleLikeAsync(track.Id);
        }
    }

    private static List<Artist> GetMockArtists()
    {
        return new List<Artist>
        {
            new Artist
            {
                Id = "artist-1",
                Name = "Lunar Echoes",
                Biography = "Electronic music producer crafting atmospheric soundscapes.",
                ImageUrl = "https://picsum.photos/seed/artist1/400/400",
                Followers = 125000,
                MonthlyListeners = 89000,
                TrackCount = 45,
                AlbumCount = 8,
                Genres = new List<string> { "Electronic", "Ambient", "Chillout" },
                TopTracks = new List<Track>
                {
                    CreateTrack("Midnight Dreams", "Lunar Echoes", "Nocturnal", TimeSpan.FromMinutes(4), 1, "https://picsum.photos/seed/track1/400/400"),
                    CreateTrack("Starlight", "Lunar Echoes", "Nocturnal", TimeSpan.FromMinutes(3), 2, "https://picsum.photos/seed/track2/400/400"),
                    CreateTrack("Nebula", "Lunar Echoes", "Cosmic Waves", TimeSpan.FromMinutes(5), 1, "https://picsum.photos/seed/track3/400/400"),
                    CreateTrack("Aurora", "Lunar Echoes", "Cosmic Waves", TimeSpan.FromMinutes(4), 2, "https://picsum.photos/seed/track4/400/400"),
                    CreateTrack("Eclipse", "Lunar Echoes", "Nocturnal", TimeSpan.FromMinutes(3), 3, "https://picsum.photos/seed/track5/400/400")
                },
                Albums = new List<Album>(),
                RelatedArtists = new List<Artist>()
            },
            new Artist
            {
                Id = "artist-2",
                Name = "Velvet Horizon",
                Biography = "Indie rock band from Portland blending classic and modern sounds.",
                ImageUrl = "https://picsum.photos/seed/artist2/400/400",
                Followers = 234000,
                MonthlyListeners = 156000,
                TrackCount = 62,
                AlbumCount = 5,
                Genres = new List<string> { "Indie Rock", "Alternative", "Dream Pop" },
                TopTracks = new List<Track>
                {
                    CreateTrack("Golden Hour", "Velvet Horizon", "Sunset Boulevard", TimeSpan.FromMinutes(3), 1, "https://picsum.photos/seed/track6/400/400"),
                    CreateTrack("Fading Light", "Velvet Horizon", "Sunset Boulevard", TimeSpan.FromMinutes(4), 2, "https://picsum.photos/seed/track7/400/400"),
                    CreateTrack("Ocean Drive", "Velvet Horizon", "Coastal", TimeSpan.FromMinutes(3), 1, "https://picsum.photos/seed/track8/400/400"),
                    CreateTrack("Wildfire", "Velvet Horizon", "Coastal", TimeSpan.FromMinutes(4), 2, "https://picsum.photos/seed/track9/400/400"),
                    CreateTrack("Horizon Line", "Velvet Horizon", "Sunset Boulevard", TimeSpan.FromMinutes(5), 3, "https://picsum.photos/seed/track10/400/400")
                },
                Albums = new List<Album>(),
                RelatedArtists = new List<Artist>()
            },
            new Artist
            {
                Id = "artist-3",
                Name = "Crystal Minds",
                Biography = "Hip-hop collective pushing boundaries with innovative production.",
                ImageUrl = "https://picsum.photos/seed/artist3/400/400",
                Followers = 567000,
                MonthlyListeners = 423000,
                TrackCount = 89,
                AlbumCount = 12,
                Genres = new List<string> { "Hip-Hop", "Rap", "Trap" },
                TopTracks = new List<Track>
                {
                    CreateTrack("Mind State", "Crystal Minds", "Conscious", TimeSpan.FromMinutes(3), 1, "https://picsum.photos/seed/track11/400/400"),
                    CreateTrack("Elevated", "Crystal Minds", "Conscious", TimeSpan.FromMinutes(4), 2, "https://picsum.photos/seed/track12/400/400"),
                    CreateTrack("Frequency", "Crystal Minds", "Wavelength", TimeSpan.FromMinutes(3), 1, "https://picsum.photos/seed/track13/400/400"),
                    CreateTrack("Clarity", "Crystal Minds", "Wavelength", TimeSpan.FromMinutes(4), 2, "https://picsum.photos/seed/track14/400/400"),
                    CreateTrack("Vision", "Crystal Minds", "Conscious", TimeSpan.FromMinutes(3), 3, "https://picsum.photos/seed/track15/400/400")
                },
                Albums = new List<Album>(),
                RelatedArtists = new List<Artist>()
            },
            new Artist
            {
                Id = "artist-4",
                Name = "Ember Rose",
                Biography = "Soul singer with a voice that captivates audiences worldwide.",
                ImageUrl = "https://picsum.photos/seed/artist4/400/400",
                Followers = 892000,
                MonthlyListeners = 654000,
                TrackCount = 34,
                AlbumCount = 4,
                Genres = new List<string> { "R&B", "Soul", "Neo-Soul" },
                TopTracks = new List<Track>
                {
                    CreateTrack("Burning Love", "Ember Rose", "Fire & Ice", TimeSpan.FromMinutes(4), 1, "https://picsum.photos/seed/track16/400/400"),
                    CreateTrack("Frozen Heart", "Ember Rose", "Fire & Ice", TimeSpan.FromMinutes(3), 2, "https://picsum.photos/seed/track17/400/400"),
                    CreateTrack("Melting Point", "Ember Rose", "Fire & Ice", TimeSpan.FromMinutes(4), 3, "https://picsum.photos/seed/track18/400/400"),
                    CreateTrack("Ashes", "Ember Rose", "Phoenix", TimeSpan.FromMinutes(5), 1, "https://picsum.photos/seed/track19/400/400"),
                    CreateTrack("Rise Up", "Ember Rose", "Phoenix", TimeSpan.FromMinutes(4), 2, "https://picsum.photos/seed/track20/400/400")
                },
                Albums = new List<Album>(),
                RelatedArtists = new List<Artist>()
            },
            new Artist
            {
                Id = "artist-5",
                Name = "Neon Pulse",
                Biography = "Synthwave duo recreating the sounds of the 80s for a new generation.",
                ImageUrl = "https://picsum.photos/seed/artist5/400/400",
                Followers = 345000,
                MonthlyListeners = 278000,
                TrackCount = 56,
                AlbumCount = 7,
                Genres = new List<string> { "Synthwave", "Retrowave", "Electronic" },
                TopTracks = new List<Track>
                {
                    CreateTrack("Nightcall", "Neon Pulse", "Retrograde", TimeSpan.FromMinutes(4), 1, "https://picsum.photos/seed/track21/400/400"),
                    CreateTrack("Digital Dreams", "Neon Pulse", "Retrograde", TimeSpan.FromMinutes(3), 2, "https://picsum.photos/seed/track22/400/400"),
                    CreateTrack("Cyber City", "Neon Pulse", "Future Past", TimeSpan.FromMinutes(5), 1, "https://picsum.photos/seed/track23/400/400"),
                    CreateTrack("Analog Heart", "Neon Pulse", "Future Past", TimeSpan.FromMinutes(4), 2, "https://picsum.photos/seed/track24/400/400"),
                    CreateTrack("Voltage", "Neon Pulse", "Retrograde", TimeSpan.FromMinutes(3), 3, "https://picsum.photos/seed/track25/400/400")
                },
                Albums = new List<Album>(),
                RelatedArtists = new List<Artist>()
            }
        };
    }

    private static List<Album> GetMockAlbums()
    {
        return new List<Album>
        {
            new Album
            {
                Id = "album-1",
                Title = "Nocturnal",
                ArtistName = "Lunar Echoes",
                ArtistId = "artist-1",
                CoverArtUrl = "https://picsum.photos/seed/album1/400/400",
                ReleaseDate = new DateTime(2024, 3, 15),
                AlbumType = "Album",
                Tracks = new List<Track>
                {
                    CreateTrack("Midnight Dreams", "Lunar Echoes", "Nocturnal", TimeSpan.FromMinutes(4), 1, "https://picsum.photos/seed/album1/400/400"),
                    CreateTrack("Starlight", "Lunar Echoes", "Nocturnal", TimeSpan.FromMinutes(3), 2, "https://picsum.photos/seed/album1/400/400"),
                    CreateTrack("Eclipse", "Lunar Echoes", "Nocturnal", TimeSpan.FromMinutes(3), 3, "https://picsum.photos/seed/album1/400/400"),
                    CreateTrack("Moonbeam", "Lunar Echoes", "Nocturnal", TimeSpan.FromMinutes(4), 4, "https://picsum.photos/seed/album1/400/400"),
                    CreateTrack("Dawn Chorus", "Lunar Echoes", "Nocturnal", TimeSpan.FromMinutes(5), 5, "https://picsum.photos/seed/album1/400/400")
                }
            },
            new Album
            {
                Id = "album-2",
                Title = "Sunset Boulevard",
                ArtistName = "Velvet Horizon",
                ArtistId = "artist-2",
                CoverArtUrl = "https://picsum.photos/seed/album2/400/400",
                ReleaseDate = new DateTime(2024, 1, 20),
                AlbumType = "Album",
                Tracks = new List<Track>
                {
                    CreateTrack("Golden Hour", "Velvet Horizon", "Sunset Boulevard", TimeSpan.FromMinutes(3), 1, "https://picsum.photos/seed/album2/400/400"),
                    CreateTrack("Fading Light", "Velvet Horizon", "Sunset Boulevard", TimeSpan.FromMinutes(4), 2, "https://picsum.photos/seed/album2/400/400"),
                    CreateTrack("Horizon Line", "Velvet Horizon", "Sunset Boulevard", TimeSpan.FromMinutes(5), 3, "https://picsum.photos/seed/album2/400/400"),
                    CreateTrack("Boulevard", "Velvet Horizon", "Sunset Boulevard", TimeSpan.FromMinutes(4), 4, "https://picsum.photos/seed/album2/400/400"),
                    CreateTrack("Last Call", "Velvet Horizon", "Sunset Boulevard", TimeSpan.FromMinutes(3), 5, "https://picsum.photos/seed/album2/400/400")
                }
            },
            new Album
            {
                Id = "album-3",
                Title = "Conscious",
                ArtistName = "Crystal Minds",
                ArtistId = "artist-3",
                CoverArtUrl = "https://picsum.photos/seed/album3/400/400",
                ReleaseDate = new DateTime(2023, 11, 10),
                AlbumType = "Album",
                Tracks = new List<Track>
                {
                    CreateTrack("Mind State", "Crystal Minds", "Conscious", TimeSpan.FromMinutes(3), 1, "https://picsum.photos/seed/album3/400/400"),
                    CreateTrack("Elevated", "Crystal Minds", "Conscious", TimeSpan.FromMinutes(4), 2, "https://picsum.photos/seed/album3/400/400"),
                    CreateTrack("Vision", "Crystal Minds", "Conscious", TimeSpan.FromMinutes(3), 3, "https://picsum.photos/seed/album3/400/400"),
                    CreateTrack("Awareness", "Crystal Minds", "Conscious", TimeSpan.FromMinutes(4), 4, "https://picsum.photos/seed/album3/400/400"),
                    CreateTrack("Enlightenment", "Crystal Minds", "Conscious", TimeSpan.FromMinutes(5), 5, "https://picsum.photos/seed/album3/400/400")
                }
            },
            new Album
            {
                Id = "album-4",
                Title = "Fire & Ice",
                ArtistName = "Ember Rose",
                ArtistId = "artist-4",
                CoverArtUrl = "https://picsum.photos/seed/album4/400/400",
                ReleaseDate = new DateTime(2024, 2, 14),
                AlbumType = "Album",
                Tracks = new List<Track>
                {
                    CreateTrack("Burning Love", "Ember Rose", "Fire & Ice", TimeSpan.FromMinutes(4), 1, "https://picsum.photos/seed/album4/400/400"),
                    CreateTrack("Frozen Heart", "Ember Rose", "Fire & Ice", TimeSpan.FromMinutes(3), 2, "https://picsum.photos/seed/album4/400/400"),
                    CreateTrack("Melting Point", "Ember Rose", "Fire & Ice", TimeSpan.FromMinutes(4), 3, "https://picsum.photos/seed/album4/400/400"),
                    CreateTrack("Cold Shoulder", "Ember Rose", "Fire & Ice", TimeSpan.FromMinutes(3), 4, "https://picsum.photos/seed/album4/400/400"),
                    CreateTrack("Warm Embrace", "Ember Rose", "Fire & Ice", TimeSpan.FromMinutes(4), 5, "https://picsum.photos/seed/album4/400/400")
                }
            }
        };
    }

    private static Track CreateTrack(string title, string artist, string album, TimeSpan duration, int trackNumber, string coverArtUrl)
    {
        return new Track
        {
            Id = Guid.NewGuid().ToString(),
            Source = TrackSource.Local,
            StorageLocation = StorageLocation.Library,
            Title = title,
            ArtistName = artist,
            ArtistId = $"artist-{artist.GetHashCode() % 10}",
            AlbumTitle = album,
            AlbumId = $"album-{album.GetHashCode() % 10}",
            Duration = duration,
            TrackNumber = trackNumber,
            CoverArtUrl = coverArtUrl,
            IsLiked = false,
            IsDownloaded = true,
            Genres = new List<string> { "Electronic" },
            PlayCount = (long)(Random.Shared.Next(1000, 100000))
        };
    }
}
