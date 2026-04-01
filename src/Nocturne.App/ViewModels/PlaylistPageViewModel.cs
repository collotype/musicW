using CommunityToolkit.Mvvm.Input;
using Nocturne.App.Models;
using Nocturne.App.Models.Enums;
using Nocturne.App.Services;

namespace Nocturne.App.ViewModels;

public sealed class PlaylistPageViewModel : PageViewModelBase
{
    private readonly IMockDataService _mockDataService;
    private readonly IOnlineMusicService _onlineMusicService;
    private readonly IPlaybackService _playbackService;

    private Playlist? _playlist;
    private bool _isLoading;

    public PlaylistPageViewModel(
        IMockDataService mockDataService,
        IOnlineMusicService onlineMusicService,
        IPlaybackService playbackService)
    {
        _mockDataService = mockDataService;
        _onlineMusicService = onlineMusicService;
        _playbackService = playbackService;

        PlayAllCommand = new AsyncRelayCommand(PlayAllAsync, () => Tracks.Count > 0);
        PlayTrackCommand = new AsyncRelayCommand<Track?>(PlayTrackAsync);
    }

    public Playlist? Playlist
    {
        get => _playlist;
        private set
        {
            if (SetProperty(ref _playlist, value))
            {
                RaisePlaylistProperties();
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public ObservableCollection<Track> Tracks { get; } = [];

    public string PlaylistMetaLine => Playlist is null
        ? string.Empty
        : $"{Playlist.OwnerName} - {Playlist.TrackCount} tracks - {Playlist.DurationLabel}";

    public string Description => Playlist?.Description ?? string.Empty;

    public int MetadataOnlyTrackCount => Tracks.Count(track => track.IsMetadataOnly);

    public IAsyncRelayCommand PlayAllCommand { get; }

    public IAsyncRelayCommand<Track?> PlayTrackCommand { get; }

    public override async Task OnNavigatedToAsync(object? parameter)
    {
        IsLoading = true;

        try
        {
            Playlist = parameter as Playlist ?? _mockDataService.CreateFeaturedPlaylist();
            ReplaceTracks(Playlist.Tracks);

            if (Playlist.Source is TrackSource.SoundCloud or TrackSource.Spotify && !string.IsNullOrWhiteSpace(Playlist.ProviderPlaylistId))
            {
                var remotePlaylist = await _onlineMusicService.GetPlaylistAsync(Playlist.Source, Playlist.ProviderPlaylistId, CancellationToken.None);
                if (remotePlaylist is not null)
                {
                    Playlist = remotePlaylist;
                    ReplaceTracks(remotePlaylist.Tracks);
                }
            }
        }
        finally
        {
            IsLoading = false;
            RaisePlaylistProperties();
            PlayAllCommand.NotifyCanExecuteChanged();
        }
    }

    private async Task PlayAllAsync()
    {
        if (Tracks.Count == 0)
        {
            return;
        }

        await _playbackService.PlayQueueAsync(Tracks.ToList(), Tracks[0], "playlist");
    }

    private async Task PlayTrackAsync(Track? track)
    {
        if (track is null)
        {
            return;
        }

        await _playbackService.PlayQueueAsync(Tracks.ToList(), track, "playlist");
    }

    private void RaisePlaylistProperties()
    {
        OnPropertyChanged(nameof(PlaylistMetaLine));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(MetadataOnlyTrackCount));
    }

    private void ReplaceTracks(IEnumerable<Track> tracks)
    {
        Tracks.Clear();
        foreach (var track in tracks)
        {
            Tracks.Add(track);
        }

        RaisePlaylistProperties();
    }
}
