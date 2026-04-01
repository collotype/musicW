using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicApp.Models;
using MusicApp.Services;

namespace MusicApp.ViewModels;

public partial class PlaylistViewModel : ObservableObject
{
    private readonly ILibraryService _libraryService;
    private readonly IPlaybackService _playbackService;
    private readonly IMusicProviderService _providerService;

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

    public PlaylistViewModel(
        ILibraryService libraryService,
        IPlaybackService playbackService,
        IMusicProviderService providerService)
    {
        _libraryService = libraryService;
        _playbackService = playbackService;
        _providerService = providerService;

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
    private async Task PlayAll()
    {
        if (DisplayedTracks.Count > 0)
        {
            await _playbackService.PlayAsync(DisplayedTracks[0], DisplayedTracks);
        }
    }

    [RelayCommand]
    private async Task PlayTrack(Track track)
    {
        await _playbackService.PlayAsync(track, DisplayedTracks);
        PlayingTrack = track;
    }

    [RelayCommand]
    private async Task RemoveTrack(Track track)
    {
        if (Playlist != null && !Playlist.IsSystemPlaylist)
        {
            await _libraryService.RemoveFromPlaylistAsync(Playlist.Id, track.Id);
            Playlist.Tracks.RemoveAll(t => t.Id == track.Id);
            ApplyTrackFilter();
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
    private Task SaveChanges()
    {
        IsEditing = false;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task DeletePlaylist()
    {
        if (Playlist != null && !Playlist.IsSystemPlaylist)
        {
            await _libraryService.DeletePlaylistAsync(Playlist.Id);
            Playlist = null;
            DisplayedTracks = new List<Track>();
            SearchQuery = string.Empty;
            IsEditing = false;
        }
    }

    [RelayCommand]
    private void ClearSearch()
    {
        if (!string.IsNullOrEmpty(SearchQuery))
        {
            SearchQuery = string.Empty;
        }
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

        if (string.Equals(providerName, "Local", StringComparison.OrdinalIgnoreCase))
        {
            Playlist = BuildLocalPlaylist(playlistId);
        }
        else
        {
            Playlist = await _providerService.GetPlaylistAsync(playlistId, providerName);
        }

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
                Title = "Liked Songs",
                Description = "Tracks you've marked as liked",
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
                Title = "Offline Tracks",
                Description = "Tracks saved for offline playback",
                OwnerName = "Your Library",
                Tracks = _libraryService.OfflineTracks.ToList(),
                IsSystemPlaylist = true
            };
        }

        return _libraryService.Playlists.FirstOrDefault(p => p.Id == playlistId);
    }

    private void ApplyTrackFilter()
    {
        var sourceTracks = Playlist?.Tracks ?? new List<Track>();
        var query = SearchQuery.Trim();

        if (string.IsNullOrWhiteSpace(query))
        {
            DisplayedTracks = sourceTracks.ToList();
            return;
        }

        DisplayedTracks = sourceTracks
            .Where(track => MatchesTrack(track, query))
            .ToList();
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

        if (!string.IsNullOrWhiteSpace(Playlist.Description))
        {
            return Playlist.Description;
        }

        return Playlist.IsSystemPlaylist ? "From your local library" : Playlist.OwnerName;
    }

    private string GetSearchScopeTitle()
    {
        return Playlist?.Id switch
        {
            "favorites" => "Filter liked songs",
            "offline" => "Filter offline tracks",
            _ => "Filter tracks in this collection"
        };
    }

    private string GetEmptyStateTitle()
    {
        return Playlist?.Id switch
        {
            "favorites" => "No favorites yet",
            "offline" => "No offline tracks yet",
            _ when Playlist?.IsSystemPlaylist == true => "This collection is empty",
            _ => "This playlist is empty"
        };
    }

    private string GetEmptyStateMessage()
    {
        return Playlist?.Id switch
        {
            "favorites" => "Tracks you like will appear here so you can find them quickly later.",
            "offline" => "Downloaded tracks will appear here when offline playback is available for them.",
            _ when Playlist?.IsSystemPlaylist == true => "This collection does not have any tracks yet.",
            _ => "Add some tracks to this playlist to start building it."
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
    }
}
