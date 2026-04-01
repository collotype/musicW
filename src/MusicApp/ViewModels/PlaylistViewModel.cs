using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicApp.Models;
using MusicApp.Services;

namespace MusicApp.ViewModels;

public partial class PlaylistViewModel : ObservableObject
{
    private readonly ILibraryService _libraryService;
    private readonly IPlaybackService _playbackService;

    [ObservableProperty]
    private Playlist? _playlist;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private Track? _playingTrack;

    [ObservableProperty]
    private bool _isEditing;

    public PlaylistViewModel(ILibraryService libraryService, IPlaybackService playbackService)
    {
        _libraryService = libraryService;
        _playbackService = playbackService;
    }

    public Task LoadPlaylistAsync(string playlistId)
    {
        Playlist = _libraryService.Playlists.FirstOrDefault(p => p.Id == playlistId);

        if (Playlist == null)
        {
            // Check for system playlists
            if (playlistId == "favorites")
            {
                Playlist = new Playlist
                {
                    Id = "favorites",
                    Title = "Liked Songs",
                    Tracks = _libraryService.LikedTracks,
                    IsSystemPlaylist = true
                };
            }
            else if (playlistId == "offline")
            {
                Playlist = new Playlist
                {
                    Id = "offline",
                    Title = "Offline Tracks",
                    Tracks = _libraryService.OfflineTracks,
                    IsSystemPlaylist = true
                };
            }
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task PlayAll()
    {
        if (Playlist?.Tracks.Count > 0)
        {
            await _playbackService.PlayAsync(Playlist.Tracks[0], Playlist.Tracks);
        }
    }

    [RelayCommand]
    private async Task PlayTrack(Track track)
    {
        await _playbackService.PlayAsync(track, Playlist?.Tracks);
        PlayingTrack = track;
    }

    [RelayCommand]
    private async Task RemoveTrack(Track track)
    {
        if (Playlist != null && !Playlist.IsSystemPlaylist)
        {
            await _libraryService.RemoveFromPlaylistAsync(Playlist.Id, track.Id);
            Playlist.Tracks.Remove(track);
        }
    }

    [RelayCommand]
    private async Task ToggleEdit()
    {
        if (Playlist != null && !Playlist.IsSystemPlaylist)
        {
            IsEditing = !IsEditing;
        }
    }

    [RelayCommand]
    private async Task SaveChanges()
    {
        IsEditing = false;
        // Save changes to library
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task DeletePlaylist()
    {
        if (Playlist != null && !Playlist.IsSystemPlaylist)
        {
            await _libraryService.DeletePlaylistAsync(Playlist.Id);
        }
    }
}
