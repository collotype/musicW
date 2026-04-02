using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicApp.Enums;
using MusicApp.Models;
using MusicApp.Services;

namespace MusicApp.ViewModels;

public partial class AlbumViewModel : ObservableObject
{
    private readonly IMusicProviderService _providerService;
    private readonly IPlaybackService _playbackService;
    private readonly INavigationService _navigationService;
    private readonly ILibraryService _libraryService;

    private string _providerName = "Local";

    [ObservableProperty]
    private Album? _album;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private Track? _playingTrack;

    [ObservableProperty]
    private List<Album> _relatedReleases = new();

    [ObservableProperty]
    private string _selectedPlaylistId = string.Empty;

    public bool HasTracks => Album?.Tracks.Count > 0;
    public bool HasRelatedReleases => RelatedReleases.Count > 0;
    public bool IsSaved => Album?.IsLiked == true;
    public List<Playlist> AvailablePlaylists => _libraryService.Playlists;
    public string SourceLabel => _providerName;
    public string AlbumSummary => Album == null
        ? string.Empty
        : $"{Album.ReleaseYear} • {Album.TotalTracks} tracks • {Album.TotalDurationFormatted} • {Album.AlbumType}";
    public string RelatedSummary => HasRelatedReleases ? $"{RelatedReleases.Count} related releases from the same artist." : "No related releases surfaced.";
    public string SaveLabel => IsSaved ? "Saved to Library" : "Save Album";

    public AlbumViewModel(
        IMusicProviderService providerService,
        IPlaybackService playbackService,
        INavigationService navigationService,
        ILibraryService libraryService)
    {
        _providerService = providerService;
        _playbackService = playbackService;
        _navigationService = navigationService;
        _libraryService = libraryService;

        EnsureSelectedPlaylist();
    }

    public async Task LoadAlbumAsync(string albumId, string providerName = "Local")
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        _providerName = providerName;

        try
        {
            Album = await _providerService.GetAlbumAsync(albumId, providerName);
            if (Album == null)
            {
                ErrorMessage = "Album not found.";
                return;
            }

            if (await _libraryService.GetAlbumAsync(Album.Id) is { } localAlbum)
            {
                Album.IsLiked = localAlbum.IsLiked;
                Album.IsDownloaded = localAlbum.IsDownloaded;
            }

            RelatedReleases = _libraryService.AllAlbums
                .Where(item => item.Id != Album.Id && string.Equals(item.ArtistId, Album.ArtistId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(item => item.ReleaseDate ?? DateTime.MinValue)
                .Take(6)
                .ToList();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            EnsureSelectedPlaylist();
            IsLoading = false;
            NotifyStateChanged();
        }
    }

    [RelayCommand]
    private Task PlayAll()
    {
        if (Album == null || Album.Tracks.Count == 0)
        {
            return Task.CompletedTask;
        }

        return _playbackService.PlayAsync(Album.Tracks[0], Album.Tracks);
    }

    [RelayCommand]
    private Task Shuffle()
    {
        if (Album == null || Album.Tracks.Count == 0)
        {
            return Task.CompletedTask;
        }

        var shuffledTracks = Album.Tracks.OrderBy(_ => Guid.NewGuid()).ToList();
        return _playbackService.PlayAsync(shuffledTracks[0], shuffledTracks);
    }

    [RelayCommand]
    private Task PlayTrack(Track? track)
    {
        if (track == null || Album == null)
        {
            return Task.CompletedTask;
        }

        PlayingTrack = track;
        return _playbackService.PlayAsync(track, Album.Tracks);
    }

    [RelayCommand]
    private async Task AddToLibrary()
    {
        if (Album == null || Album.Tracks.Count == 0)
        {
            return;
        }

        foreach (var track in Album.Tracks)
        {
            await _libraryService.AddTrackAsync(track);
        }
    }

    [RelayCommand]
    private async Task ToggleSaveAlbum()
    {
        if (Album == null)
        {
            return;
        }

        await _libraryService.ToggleSaveAlbumAsync(Album.Id);
        Album.IsLiked = !Album.IsLiked;
        NotifyStateChanged();
    }

    [RelayCommand]
    private void NavigateToArtist()
    {
        if (Album != null)
        {
            _navigationService.NavigateToArtist(Album.ArtistId, _providerName);
        }
    }

    [RelayCommand]
    private void NavigateToRelatedAlbum(string? albumId)
    {
        if (!string.IsNullOrWhiteSpace(albumId))
        {
            _navigationService.NavigateToAlbum(albumId, _providerName);
        }
    }

    [RelayCommand]
    private void StartWave()
    {
        if (Album == null)
        {
            return;
        }

        _navigationService.NavigateToMyWave(new WaveSeed
        {
            Type = WaveSeedType.Album,
            Id = Album.Id,
            Title = Album.Title,
            Subtitle = $"Wave started from {Album.ArtistName}."
        });
    }

    [RelayCommand]
    private async Task AddAlbumToSelectedPlaylist()
    {
        if (Album == null || string.IsNullOrWhiteSpace(SelectedPlaylistId))
        {
            return;
        }

        foreach (var track in Album.Tracks)
        {
            await _libraryService.AddToPlaylistAsync(SelectedPlaylistId, track);
        }
    }

    private void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(HasTracks));
        OnPropertyChanged(nameof(HasRelatedReleases));
        OnPropertyChanged(nameof(IsSaved));
        OnPropertyChanged(nameof(AvailablePlaylists));
        OnPropertyChanged(nameof(SourceLabel));
        OnPropertyChanged(nameof(AlbumSummary));
        OnPropertyChanged(nameof(RelatedSummary));
        OnPropertyChanged(nameof(SaveLabel));
    }

    private void EnsureSelectedPlaylist()
    {
        if (!string.IsNullOrWhiteSpace(SelectedPlaylistId))
        {
            return;
        }

        SelectedPlaylistId = AvailablePlaylists.FirstOrDefault()?.Id ?? string.Empty;
    }
}
