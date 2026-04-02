using MusicApp.Enums;
using MusicApp.Models;

namespace MusicApp.Services;

public class RecommendationService : IRecommendationService
{
    private readonly ILibraryService _libraryService;
    private readonly ISettingsService _settingsService;

    public RecommendationService(ILibraryService libraryService, ISettingsService settingsService)
    {
        _libraryService = libraryService;
        _settingsService = settingsService;
    }

    public WaveTunerSettings CreateTunerFromSettings()
    {
        var settings = _settingsService.Settings;
        return new WaveTunerSettings
        {
            Activity = settings.WaveActivity,
            Mood = settings.WaveMood,
            Language = settings.WaveLanguage,
            Familiarity = settings.DiscoveryBalance,
            Popularity = settings.PopularityBalance,
            ArtistVariety = settings.ArtistVariety,
            Energy = settings.EnergyBalance
        };
    }

    public Task SaveTunerAsync(WaveTunerSettings tuner)
    {
        return _settingsService.UpdateSettingsAsync(settings =>
        {
            settings.WaveActivity = tuner.Activity;
            settings.WaveMood = tuner.Mood;
            settings.WaveLanguage = tuner.Language;
            settings.DiscoveryBalance = tuner.Familiarity;
            settings.PopularityBalance = tuner.Popularity;
            settings.ArtistVariety = tuner.ArtistVariety;
            settings.EnergyBalance = tuner.Energy;
        });
    }

    public List<Track> GetContinueListening(int count = 8)
    {
        return _libraryService.AllTracks
            .Where(track => track.LastPlayedAt.HasValue)
            .OrderByDescending(track => track.LastPlayedAt)
            .ThenByDescending(track => track.PlayCount ?? 0)
            .Take(count)
            .Select(track => track.Clone())
            .ToList();
    }

    public List<Album> GetSuggestedAlbums(int count = 8)
    {
        var favoriteArtistIds = _libraryService.FavoriteArtists.Select(artist => artist.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return _libraryService.AllAlbums
            .OrderByDescending(album => favoriteArtistIds.Contains(album.ArtistId))
            .ThenByDescending(album => album.IsLiked)
            .ThenByDescending(album => album.ReleaseDate ?? DateTime.MinValue)
            .Take(count)
            .Select(album => CloneAlbum(album))
            .ToList();
    }

    public List<Artist> GetFavoriteArtistSuggestions(int count = 8)
    {
        var favorites = _libraryService.FavoriteArtists;
        if (favorites.Count > 0)
        {
            return favorites
                .OrderByDescending(artist => artist.TrackCount)
                .ThenBy(artist => artist.Name)
                .Take(count)
                .Select(CloneArtist)
                .ToList();
        }

        return _libraryService.AllArtists
            .OrderByDescending(artist => artist.TopTracks.Sum(track => track.PlayCount ?? 0))
            .ThenByDescending(artist => artist.TrackCount)
            .Take(count)
            .Select(CloneArtist)
            .ToList();
    }

    public List<Playlist> GetHighlightedPlaylists(int count = 6)
    {
        return _libraryService.Playlists
            .OrderByDescending(playlist => playlist.IsPinned)
            .ThenByDescending(playlist => playlist.LastModifiedDate ?? playlist.CreatedDate)
            .ThenByDescending(playlist => playlist.TotalTracks)
            .Take(count)
            .Select(ClonePlaylist)
            .ToList();
    }

    public List<Track> GetDiscoveryTracks(int count = 12)
    {
        return CreateWave(WaveSeed.Home(), CreateTunerFromSettings(), count)
            .Select(item => item.Track)
            .ToList();
    }

    public List<WaveRecommendation> CreateWave(WaveSeed seed, WaveTunerSettings tuner, int count = 24)
    {
        var candidates = _libraryService.AllTracks
            .Where(track => !string.IsNullOrWhiteSpace(track.Title))
            .ToList();

        if (candidates.Count == 0)
        {
            return new List<WaveRecommendation>();
        }

        var likedTracks = _libraryService.LikedTracks;
        var recentTracks = GetContinueListening(16);
        var preferredGenres = likedTracks
            .Concat(recentTracks)
            .SelectMany(track => track.Genres)
            .Where(genre => !string.IsNullOrWhiteSpace(genre))
            .GroupBy(genre => genre, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .Take(6)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var preferredArtistIds = likedTracks
            .Concat(recentTracks)
            .Select(track => track.ArtistId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var seedArtistIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seedAlbumIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seedTrackIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seedTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        switch (seed.Type)
        {
            case WaveSeedType.Track:
                seedTrackIds.Add(seed.Id);
                break;
            case WaveSeedType.Artist:
                seedArtistIds.Add(seed.Id);
                break;
            case WaveSeedType.Album:
                seedAlbumIds.Add(seed.Id);
                break;
            case WaveSeedType.Playlist:
                if (_libraryService.Playlists.FirstOrDefault(playlist => playlist.Id == seed.Id) is { } playlist)
                {
                    foreach (var artistId in playlist.Tracks.Select(track => track.ArtistId).Where(id => !string.IsNullOrWhiteSpace(id)))
                    {
                        seedArtistIds.Add(artistId);
                    }

                    foreach (var albumId in playlist.Tracks.Select(track => track.AlbumId).Where(id => !string.IsNullOrWhiteSpace(id)))
                    {
                        seedAlbumIds.Add(albumId);
                    }

                    foreach (var genre in playlist.Tracks.SelectMany(track => track.Genres).Where(genre => !string.IsNullOrWhiteSpace(genre)))
                    {
                        preferredGenres.Add(genre);
                    }
                }
                break;
            case WaveSeedType.Library:
                preferredArtistIds.UnionWith(_libraryService.FavoriteArtists.Select(artist => artist.Id));
                break;
        }

        seedTitles.Add(seed.Title);

        var scored = candidates
            .Select(track => new WaveRecommendation
            {
                Track = track.Clone(),
                Score = ScoreTrack(track, seedArtistIds, seedAlbumIds, seedTrackIds, seedTitles, preferredArtistIds, preferredGenres, tuner),
                Reason = BuildReason(track, seed, preferredArtistIds, preferredGenres)
            })
            .OrderByDescending(item => item.Score)
            .ThenByDescending(item => item.Track.LastPlayedAt ?? DateTime.MinValue)
            .ThenByDescending(item => item.Track.DateAdded)
            .ToList();

        var results = new List<WaveRecommendation>();
        var artistFrequency = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var recommendation in scored)
        {
            if (results.Count >= count)
            {
                break;
            }

            var artistId = string.IsNullOrWhiteSpace(recommendation.Track.ArtistId) ? recommendation.Track.ArtistName : recommendation.Track.ArtistId;
            artistFrequency.TryGetValue(artistId, out var appearances);
            var maxAppearances = tuner.ArtistVariety >= 0.7 ? 1 : tuner.ArtistVariety >= 0.4 ? 2 : 3;

            if (appearances >= maxAppearances && scored.Count > count)
            {
                continue;
            }

            artistFrequency[artistId] = appearances + 1;
            results.Add(recommendation);
        }

        return results;
    }

    public List<SnippetMoment> CreateSnippets(WaveSeed seed, WaveTunerSettings tuner, int count = 8)
    {
        return CreateWave(seed, tuner, count)
            .Select(item =>
            {
                var startTime = item.Track.Duration.TotalSeconds <= 60
                    ? TimeSpan.Zero
                    : TimeSpan.FromSeconds(Math.Min(item.Track.Duration.TotalSeconds * 0.25, 45));

                return new SnippetMoment
                {
                    Track = item.Track.Clone(),
                    StartTime = startTime,
                    Headline = item.Track.Genres.FirstOrDefault() ?? item.Track.ArtistName,
                    Detail = item.Reason
                };
            })
            .ToList();
    }

    public List<QueueItem> GetSmartQueueTracks(Track? currentTrack, int count = 6, IEnumerable<string>? excludeTrackIds = null)
    {
        var excluded = excludeTrackIds?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (currentTrack != null)
        {
            excluded.Add(currentTrack.Id);
        }

        var seed = currentTrack == null
            ? WaveSeed.Home()
            : new WaveSeed
            {
                Type = WaveSeedType.Track,
                Id = currentTrack.Id,
                Title = currentTrack.Title,
                Subtitle = currentTrack.ArtistName
            };

        return CreateWave(seed, CreateTunerFromSettings(), count * 2)
            .Where(item => !excluded.Contains(item.Track.Id))
            .Take(count)
            .Select(item => QueueItem.FromTrack(
                item.Track,
                "Smart Queue",
                item.Track.Id,
                isRecommendation: true,
                recommendationReason: item.Reason))
            .ToList();
    }

    private static double ScoreTrack(
        Track track,
        HashSet<string> seedArtistIds,
        HashSet<string> seedAlbumIds,
        HashSet<string> seedTrackIds,
        HashSet<string> seedTitles,
        HashSet<string> preferredArtistIds,
        HashSet<string> preferredGenres,
        WaveTunerSettings tuner)
    {
        var score = 0d;
        var normalizedPlayCount = Math.Min((track.PlayCount ?? 0) / 100.0, 20);
        var recencyBoost = track.LastPlayedAt.HasValue
            ? Math.Max(0, 30 - (DateTime.UtcNow - track.LastPlayedAt.Value).TotalDays) / 10
            : 0;
        var freshnessBoost = Math.Max(0, 14 - (DateTime.UtcNow - track.DateAdded).TotalDays) / 14;

        score += track.IsLiked ? 22 * tuner.Familiarity : 0;
        score += normalizedPlayCount * (0.4 + tuner.Popularity);
        score += recencyBoost * (0.3 + tuner.Familiarity);
        score += freshnessBoost * (1.3 - tuner.Familiarity);

        if (seedArtistIds.Contains(track.ArtistId))
        {
            score += 18;
        }

        if (seedAlbumIds.Contains(track.AlbumId))
        {
            score += 16;
        }

        if (seedTrackIds.Contains(track.Id))
        {
            score += 24;
        }

        if (seedTitles.Contains(track.Title))
        {
            score += 10;
        }

        if (preferredArtistIds.Contains(track.ArtistId))
        {
            score += 12 * tuner.Familiarity;
        }

        var matchingGenres = track.Genres.Count(genre => preferredGenres.Contains(genre));
        score += matchingGenres * 5;

        score += ScoreMood(track, tuner);

        if (!string.Equals(tuner.Language, "Any", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(track.Language, tuner.Language, StringComparison.OrdinalIgnoreCase))
        {
            score += 6;
        }

        return score;
    }

    private static double ScoreMood(Track track, WaveTunerSettings tuner)
    {
        var genres = track.Genres.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var score = 0d;

        if (string.Equals(tuner.Activity, "Focus", StringComparison.OrdinalIgnoreCase))
        {
            score += genres.Contains("Ambient") || genres.Contains("Electronic") ? 4 : 0;
        }
        else if (string.Equals(tuner.Activity, "Workout", StringComparison.OrdinalIgnoreCase))
        {
            score += genres.Contains("Hip-Hop") || genres.Contains("Rock") || genres.Contains("Electronic") ? 5 : 0;
        }

        if (string.Equals(tuner.Mood, "Calm", StringComparison.OrdinalIgnoreCase))
        {
            score += genres.Contains("Ambient") || genres.Contains("Soul") || genres.Contains("Chillout") ? 6 * (1 - tuner.Energy) : 0;
        }
        else if (string.Equals(tuner.Mood, "Electric", StringComparison.OrdinalIgnoreCase))
        {
            score += genres.Contains("Electronic") || genres.Contains("Trap") || genres.Contains("Rock") ? 6 * tuner.Energy : 0;
        }

        return score;
    }

    private static string BuildReason(Track track, WaveSeed seed, HashSet<string> preferredArtistIds, HashSet<string> preferredGenres)
    {
        if (seed.Type == WaveSeedType.Artist && string.Equals(track.ArtistId, seed.Id, StringComparison.OrdinalIgnoreCase))
        {
            return $"Directly tied to {seed.Title}.";
        }

        if (seed.Type == WaveSeedType.Album && string.Equals(track.AlbumId, seed.Id, StringComparison.OrdinalIgnoreCase))
        {
            return $"Pulled from the orbit of {seed.Title}.";
        }

        var genre = track.Genres.FirstOrDefault(preferredGenres.Contains);
        if (!string.IsNullOrWhiteSpace(genre))
        {
            return $"Matches the {genre.ToLowerInvariant()} lane you return to.";
        }

        if (preferredArtistIds.Contains(track.ArtistId))
        {
            return $"Close to artists you've already pulled into your library.";
        }

        if (track.IsLiked)
        {
            return "You already liked this direction, so it stays in the mix.";
        }

        return "Fresh cut aligned with the current wave tuner.";
    }

    private static Album CloneAlbum(Album album)
    {
        return new Album
        {
            Id = album.Id,
            Title = album.Title,
            ArtistName = album.ArtistName,
            ArtistId = album.ArtistId,
            CoverArtUrl = album.CoverArtUrl,
            ReleaseDate = album.ReleaseDate,
            Label = album.Label,
            AlbumType = album.AlbumType,
            Tracks = album.Tracks.Select(track => track.Clone()).ToList(),
            Genres = new List<string>(album.Genres),
            IsLiked = album.IsLiked,
            IsDownloaded = album.IsDownloaded,
            ProviderAlbumId = album.ProviderAlbumId
        };
    }

    private static Artist CloneArtist(Artist artist)
    {
        return new Artist
        {
            Id = artist.Id,
            Name = artist.Name,
            Biography = artist.Biography,
            ImageUrl = artist.ImageUrl,
            BannerUrl = artist.BannerUrl,
            Country = artist.Country,
            Followers = artist.Followers,
            MonthlyListeners = artist.MonthlyListeners,
            TrackCount = artist.TrackCount,
            AlbumCount = artist.AlbumCount,
            Genres = new List<string>(artist.Genres),
            SocialLinks = new List<string>(artist.SocialLinks),
            IsFollowed = artist.IsFollowed,
            FormedYear = artist.FormedYear,
            TopTracks = artist.TopTracks.Select(track => track.Clone()).ToList(),
            Albums = artist.Albums.Select(CloneAlbum).ToList()
        };
    }

    private static Playlist ClonePlaylist(Playlist playlist)
    {
        return new Playlist
        {
            Id = playlist.Id,
            Title = playlist.Title,
            Description = playlist.Description,
            CoverArtUrl = playlist.CoverArtUrl,
            OwnerName = playlist.OwnerName,
            OwnerId = playlist.OwnerId,
            Tracks = playlist.Tracks.Select(track => track.Clone()).ToList(),
            IsPublic = playlist.IsPublic,
            IsSystemPlaylist = playlist.IsSystemPlaylist,
            IsPinned = playlist.IsPinned,
            IsDownloaded = playlist.IsDownloaded,
            CreatedDate = playlist.CreatedDate,
            LastModifiedDate = playlist.LastModifiedDate,
            ProviderPlaylistId = playlist.ProviderPlaylistId
        };
    }
}
