using CommunityToolkit.Mvvm.Input;
using Nocturne.App.Models;
using Nocturne.App.Models.Enums;
using Nocturne.App.Services;

namespace Nocturne.App.ViewModels;

public sealed class AlbumPageViewModel : PageViewModelBase
{
    private readonly IMockDataService _mockDataService;
    private readonly IOnlineMusicService _onlineMusicService;
    private readonly IPlaybackService _playbackService;
    private readonly ILibraryService _libraryService;
    private readonly INavigationService _navigationService;

    private Album? _album;
    private bool _isLoading;

    public AlbumPageViewModel(
        IMockDataService mockDataService,
        IOnlineMusicService onlineMusicService,
        IPlaybackService playbackService,
        ILibraryService libraryService,
        INavigationService navigationService)
    {
        _mockDataService = mockDataService;
        _onlineMusicService = onlineMusicService;
        _playbackService = playbackService;
        _libraryService = libraryService;
        _navigationService = navigationService;

        PlayAllCommand = new AsyncRelayCommand(PlayAllAsync, () => Tracks.Count > 0);
        ShuffleCommand = new AsyncRelayCommand(ShuffleAsync, () => Tracks.Count > 0);
        SaveCommand = new AsyncRelayCommand(SaveAsync, () => Album is not null);
        PlayTrackCommand = new AsyncRelayCommand<Track?>(PlayTrackAsync);
        OpenArtistCommand = new AsyncRelayCommand(OpenArtistAsync, () => Album is not null);
    }

    public Album? Album
    {
        get => _album;
        private set
        {
            if (SetProperty(ref _album, value))
            {
                RaiseAlbumProperties();
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public ObservableCollection<Track> Tracks { get; } = [];

    public string AlbumMetaLine => Album is null
        ? string.Empty
        : $"{Album.ReleaseYearLabel} - {Album.TrackCount} tracks - {Album.DurationLabel}";

    public string Description => Album?.Description ?? string.Empty;

    public IAsyncRelayCommand PlayAllCommand { get; }

    public IAsyncRelayCommand ShuffleCommand { get; }

    public IAsyncRelayCommand SaveCommand { get; }

    public IAsyncRelayCommand<Track?> PlayTrackCommand { get; }

    public IAsyncRelayCommand OpenArtistCommand { get; }

    public override async Task OnNavigatedToAsync(object? parameter)
    {
        IsLoading = true;

        try
        {
            Album = parameter as Album ?? _mockDataService.CreateFeaturedAlbum();
            ReplaceTracks(Album.Tracks);

            if (Album.Source is TrackSource.SoundCloud or TrackSource.Spotify && !string.IsNullOrWhiteSpace(Album.ProviderAlbumId))
            {
                var onlineAlbum = await _onlineMusicService.GetAlbumAsync(Album.Source, Album.ProviderAlbumId, CancellationToken.None);
                if (onlineAlbum is not null)
                {
                    Album = onlineAlbum;
                    ReplaceTracks(onlineAlbum.Tracks);
                }
            }
        }
        finally
        {
            IsLoading = false;
            RaiseAlbumProperties();
            PlayAllCommand.NotifyCanExecuteChanged();
            ShuffleCommand.NotifyCanExecuteChanged();
            SaveCommand.NotifyCanExecuteChanged();
            OpenArtistCommand.NotifyCanExecuteChanged();
        }
    }

    private async Task PlayAllAsync()
    {
        if (Tracks.Count == 0)
        {
            return;
        }

        await _playbackService.PlayQueueAsync(Tracks.ToList(), Tracks[0], "album");
    }

    private async Task ShuffleAsync()
    {
        if (Tracks.Count == 0)
        {
            return;
        }

        var shuffled = Tracks.OrderBy(_ => Random.Shared.Next()).ToList();
        await _playbackService.PlayQueueAsync(shuffled, shuffled[0], "album");
    }

    private async Task SaveAsync()
    {
        if (Album is null)
        {
            return;
        }

        foreach (var track in Tracks)
        {
            await _libraryService.AddTrackToLibraryAsync(track);
        }
    }

    private async Task PlayTrackAsync(Track? track)
    {
        if (track is null)
        {
            return;
        }

        await _playbackService.PlayQueueAsync(Tracks.ToList(), track, "album");
    }

    private Task OpenArtistAsync()
    {
        if (Album is null)
        {
            return Task.CompletedTask;
        }

        return _navigationService.NavigateAsync<ArtistPageViewModel>(new Artist
        {
            Id = Album.ProviderAlbumId ?? $"artist-{Album.ArtistName}",
            Name = Album.ArtistName,
            AvatarUrl = Album.CoverArtUrl,
            HeaderImageUrl = Album.HeaderImageUrl,
            Source = Album.Source
        });
    }

    private void RaiseAlbumProperties()
    {
        OnPropertyChanged(nameof(AlbumMetaLine));
        OnPropertyChanged(nameof(Description));
    }

    private void ReplaceTracks(IEnumerable<Track> tracks)
    {
        Tracks.Clear();
        foreach (var track in tracks)
        {
            Tracks.Add(track);
        }

        RaiseAlbumProperties();
    }
}
