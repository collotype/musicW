using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicApp.Enums;
using MusicApp.Models;
using MusicApp.Services;

namespace MusicApp.ViewModels;

public partial class PlaylistViewModel : ObservableObject
{
    private readonly ILibraryService _libraryService;
    private readonly IPlaybackService _playbackService;
    private readonly IMusicProviderService _providerService;
    private readonly INavigationService _navigationService;
    private readonly IRecommendationService _recommendationService;

    private string _currentPlaylistId = string.Empty;
    private string _currentProviderName = "Local";

    [ObservableProperty]
    private Playlist? _playlist;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private List<Track> _displayedTracks = new();

    [ObservableProperty]
    private Track? _playingTrack;

    [ObservableProperty]
    private bool _isEditing;

    public bool HasTracks => Playlist?.Tracks.Count > 0;
    public bool HasFilteredTracks => DisplayedTracks.Count > 0;
    public bool ShowEmptyCollectionState => Playlist != null && !HasTracks;
    public bool ShowNoSearchMatchesState => Playlist != null && HasTracks && !HasFilteredTracks;
    public string CollectionLabel => Playlist?.IsSystemPlaylist == true ? "COLLECTION" : "PLAYLIST";
    public string CollectionSubtitle => GetCollectionSubtitle();
    public string SearchScopeTitle => GetSearchScopeTitle();
    public string EmptyStateTitle => GetEmptyStateTitle();
    public string EmptyStateMessage => GetEmptyStateMessage();
    public string NoSearchMatchesMessage => GetNoSearchMatchesMessage();
    public bool IsPinned => Playlist?.IsPinned == true;

    public PlaylistViewModel(
        ILibraryService libraryService,
        IPlaybackService playbackService,
        IMusicProviderService providerService,
        INavigationService navigationService,
        IRecommendationService recommendationService)
    {
        _libraryService = libraryService;
        _playbackService = playbackService;
        _providerService = providerService;
        _navigationService = navigationService;
        _recommendationService = recommendationService;

        _libraryService.LibraryChanged += OnLibraryChanged;
    }

    partial void OnPlaylistChanged(Playlist? value)
    {
        ApplyTrackFilter();
        NotifyCollectionStateChanged();
    }

    partial void OnSearchQueryChanged(string value)
    {
        ApplyTrackFilter();
        NotifyCollectionStateChanged();
    }

    partial void OnDisplayedTracksChanged(List<Track> value)
    {
        NotifyCollectionStateChanged();
    }

    public Task LoadPlaylistAsync(string playlistId, string providerName = "Local")
    {
        return LoadPlaylistCoreAsync(playlistId, providerName, preserveSearchQuery: false);
    }

    [RelayCommand]
    private Task PlayAll()
    {
        return DisplayedTracks.Count > 0 ? _playbackService.PlayAsync(DisplayedTracks[0], DisplayedTracks) : Task.CompletedTask;
    }

    [RelayCommand]
    private Task Shuffle()
    {
        if (DisplayedTracks.Count == 0)
        {
            return Task.CompletedTask;
        }

        var shuffledTracks = DisplayedTracks.OrderBy(_ => Guid.NewGuid()).ToList();
        return _playbackService.PlayAsync(shuffledTracks[0], shuffledTracks);
    }

    [RelayCommand]
    private Task SmartShuffle()
    {
        if (Playlist == null || Playlist.Tracks.Count == 0)
        {
            return Task.CompletedTask;
        }

        var queue = Playlist.Tracks
            .Select(track => QueueItem.FromTrack(track, "Playlist", Playlist.Id))
            .Concat(_recommendationService.GetSmartQueueTracks(Playlist.Tracks.FirstOrDefault(), 6, Playlist.Tracks.Select(track => track.Id)))
            .ToList();

        return _playbackService.PlayQueueAsync(queue, 0);
    }

    [RelayCommand]
    private Task PlayTrack(Track? track)
    {
        if (track == null)
        {
            return Task.CompletedTask;
        }

        PlayingTrack = track;
        return _playbackService.PlayAsync(track, DisplayedTracks);
    }

    [RelayCommand]
    private async Task RemoveTrack(Track? track)
    {
        if (Playlist == null || track == null || Playlist.IsSystemPlaylist)
        {
            return;
        }

        await _libraryService.RemoveFromPlaylistAsync(Playlist.Id, track.Id);
        Playlist.Tracks.RemoveAll(item => item.Id == track.Id);
        ApplyTrackFilter();
    }

    [RelayCommand]
    private async Task MoveTrackUp(Track? track)
    {
        if (Playlist == null || track == null || Playlist.IsSystemPlaylist)
        {
            return;
        }

        var index = Playlist.Tracks.FindIndex(item => item.Id == track.Id);
        if (index > 0)
        {
            await _libraryService.ReorderPlaylistTrackAsync(Playlist.Id, index, index - 1);
        }
    }

    [RelayCommand]
    private async Task MoveTrackDown(Track? track)
    {
        if (Playlist == null || track == null || Playlist.IsSystemPlaylist)
        {
            return;
        }

        var index = Playlist.Tracks.FindIndex(item => item.Id == track.Id);
        if (index >= 0 && index < Playlist.Tracks.Count - 1)
        {
            await _libraryService.ReorderPlaylistTrackAsync(Playlist.Id, index, index + 1);
        }
    }

    [RelayCommand]
    private Task ToggleEdit()
    {
        if (Playlist != null && !Playlist.IsSystemPlaylist)
        {
            IsEditing = !IsEditing;
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task TogglePin()
    {
        if (Playlist == null || Playlist.IsSystemPlaylist)
        {
            return;
        }

        await _libraryService.TogglePlaylistPinAsync(Playlist.Id);
    }

    [RelayCommand]
    private async Task DeletePlaylist()
    {
        if (Playlist == null || Playlist.IsSystemPlaylist)
        {
            return;
        }

        await _libraryService.DeletePlaylistAsync(Playlist.Id);
        Playlist = null;
        DisplayedTracks = new List<Track>();
        SearchQuery = string.Empty;
        IsEditing = false;
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchQuery = string.Empty;
    }

    [RelayCommand]
    private void StartWave()
    {
        if (Playlist == null)
        {
            return;
        }

        _navigationService.NavigateToMyWave(new WaveSeed
        {
            Type = WaveSeedType.Playlist,
            Id = Playlist.Id,
            Title = Playlist.Title,
            Subtitle = "Wave started from this playlist."
        });
    }

    private async void OnLibraryChanged(object? sender, EventArgs e)
    {
        if (!string.Equals(_currentProviderName, "Local", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(_currentPlaylistId))
        {
            return;
        }

        await LoadPlaylistCoreAsync(_currentPlaylistId, _currentProviderName, preserveSearchQuery: true);
    }

    private async Task LoadPlaylistCoreAsync(string playlistId, string providerName, bool preserveSearchQuery)
    {
        _currentPlaylistId = playlistId;
        _currentProviderName = providerName;
        IsEditing = false;

        Playlist = string.Equals(providerName, "Local", StringComparison.OrdinalIgnoreCase)
            ? BuildLocalPlaylist(playlistId)
            : await _providerService.GetPlaylistAsync(playlistId, providerName);

        if (!preserveSearchQuery && !string.IsNullOrEmpty(SearchQuery))
        {
            SearchQuery = string.Empty;
            return;
        }

        ApplyTrackFilter();
    }

    private Playlist? BuildLocalPlaylist(string playlistId)
    {
        if (playlistId == "favorites")
        {
            return new Playlist
            {
                Id = "favorites",
                Title = "Liked Tracks",
                Description = "Tracks you've marked as liked.",
                OwnerName = "Your Library",
                Tracks = _libraryService.LikedTracks.ToList(),
                IsSystemPlaylist = true
            };
        }

        if (playlistId == "offline")
        {
            return new Playlist
            {
                Id = "offline",
                Title = "Downloads",
                Description = "Tracks currently available offline.",
                OwnerName = "Your Library",
                Tracks = _libraryService.OfflineTracks.ToList(),
                IsSystemPlaylist = true
            };
        }

        return _libraryService.Playlists.FirstOrDefault(playlist => playlist.Id == playlistId);
    }

    private void ApplyTrackFilter()
    {
        var sourceTracks = Playlist?.Tracks ?? new List<Track>();
        var query = SearchQuery.Trim();

        DisplayedTracks = string.IsNullOrWhiteSpace(query)
            ? sourceTracks.ToList()
            : sourceTracks.Where(track => MatchesTrack(track, query)).ToList();
    }

    private static bool MatchesTrack(Track track, string query)
    {
        return Contains(track.Title, query) ||
               Contains(track.ArtistName, query) ||
               Contains(track.AlbumTitle, query) ||
               track.Genres.Any(genre => Contains(genre, query)) ||
               track.Tags.Any(tag => Contains(tag, query));
    }

    private static bool Contains(string? value, string query)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private string GetCollectionSubtitle()
    {
        if (Playlist == null)
        {
            return string.Empty;
        }

        return !string.IsNullOrWhiteSpace(Playlist.Description)
            ? Playlist.Description
            : Playlist.IsSystemPlaylist ? "System collection from your library." : Playlist.OwnerName;
    }

    private string GetSearchScopeTitle()
    {
        return Playlist?.Id switch
        {
            "favorites" => "Filter liked tracks",
            "offline" => "Filter downloads",
            _ => "Filter tracks in this playlist"
        };
    }

    private string GetEmptyStateTitle()
    {
        return Playlist?.Id switch
        {
            "favorites" => "No liked tracks yet",
            "offline" => "Nothing downloaded yet",
            _ when Playlist?.IsSystemPlaylist == true => "This collection is empty",
            _ => "This playlist is empty"
        };
    }

    private string GetEmptyStateMessage()
    {
        return Playlist?.Id switch
        {
            "favorites" => "Like tracks from search, artists, or albums and they will appear here.",
            "offline" => "Downloaded tracks, albums, and playlists will surface here.",
            _ when Playlist?.IsSystemPlaylist == true => "This system collection does not contain any tracks yet.",
            _ => "Add tracks from search, albums, or artist pages to build this playlist."
        };
    }

    private string GetNoSearchMatchesMessage()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            return string.Empty;
        }

        var collectionName = string.IsNullOrWhiteSpace(Playlist?.Title) ? "this collection" : Playlist.Title;
        return $"No tracks in {collectionName} match \"{SearchQuery.Trim()}\".";
    }

    private void NotifyCollectionStateChanged()
    {
        OnPropertyChanged(nameof(HasTracks));
        OnPropertyChanged(nameof(HasFilteredTracks));
        OnPropertyChanged(nameof(ShowEmptyCollectionState));
        OnPropertyChanged(nameof(ShowNoSearchMatchesState));
        OnPropertyChanged(nameof(CollectionLabel));
        OnPropertyChanged(nameof(CollectionSubtitle));
        OnPropertyChanged(nameof(SearchScopeTitle));
        OnPropertyChanged(nameof(EmptyStateTitle));
        OnPropertyChanged(nameof(EmptyStateMessage));
        OnPropertyChanged(nameof(NoSearchMatchesMessage));
        OnPropertyChanged(nameof(IsPinned));
    }
}
