using CommunityToolkit.Mvvm.Input;
using Nocturne.App.Models;
using Nocturne.App.Models.Enums;
using Nocturne.App.Services;

namespace Nocturne.App.ViewModels;

public sealed class ArtistPageViewModel : PageViewModelBase
{
    private readonly IMockDataService _mockDataService;
    private readonly IOnlineMusicService _onlineMusicService;
    private readonly IPlaybackService _playbackService;
    private readonly INavigationService _navigationService;

    private Artist? _artist;
    private bool _isLoading;

    public ArtistPageViewModel(
        IMockDataService mockDataService,
        IOnlineMusicService onlineMusicService,
        IPlaybackService playbackService,
        INavigationService navigationService)
    {
        _mockDataService = mockDataService;
        _onlineMusicService = onlineMusicService;
        _playbackService = playbackService;
        _navigationService = navigationService;

        PlayAllCommand = new AsyncRelayCommand(PlayAllAsync, () => Tracks.Count > 0);
        ShuffleCommand = new AsyncRelayCommand(ShuffleAsync, () => Tracks.Count > 0);
        PlayTrackCommand = new AsyncRelayCommand<Track?>(PlayTrackAsync);
        OpenAlbumCommand = new AsyncRelayCommand<Album?>(OpenAlbumAsync);
        OpenRelatedArtistCommand = new AsyncRelayCommand<Artist?>(OpenRelatedArtistAsync);
    }

    public Artist? Artist
    {
        get => _artist;
        private set
        {
            if (SetProperty(ref _artist, value))
            {
                RaiseArtistProperties();
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public ObservableCollection<Track> Tracks { get; } = [];

    public ObservableCollection<Album> Albums { get; } = [];

    public ObservableCollection<Artist> RelatedArtists { get; } = [];

    public string AboutText => Artist?.About ?? string.Empty;

    public string MonthlyListenersLabel => $"{Artist?.MonthlyListeners ?? 0:N0}";

    public string FollowersLabel => $"{Artist?.Followers ?? 0:N0}";

    public string TrackCountLabel => Tracks.Count.ToString("N0");

    public string CountryLabel => Artist?.CountryLabel ?? "Global";

    public IAsyncRelayCommand PlayAllCommand { get; }

    public IAsyncRelayCommand ShuffleCommand { get; }

    public IAsyncRelayCommand<Track?> PlayTrackCommand { get; }

    public IAsyncRelayCommand<Album?> OpenAlbumCommand { get; }

    public IAsyncRelayCommand<Artist?> OpenRelatedArtistCommand { get; }

    public override async Task OnNavigatedToAsync(object? parameter)
    {
        IsLoading = true;

        try
        {
            Artist = parameter as Artist ?? _mockDataService.CreateFeaturedArtist();
            ReplaceCollection(Tracks, Artist.TopTracks);
            ReplaceCollection(Albums, Artist.Albums);
            ReplaceCollection(RelatedArtists, Artist.RelatedArtists);

            var providerArtistId = Artist.ProviderArtistId;
            if (Artist.Source is TrackSource.SoundCloud or TrackSource.Spotify && !string.IsNullOrWhiteSpace(providerArtistId))
            {
                var hydratedArtist = await _onlineMusicService.GetArtistAsync(Artist.Source, providerArtistId, CancellationToken.None);
                if (hydratedArtist is not null)
                {
                    Artist = hydratedArtist;
                }

                var tracks = await _onlineMusicService.GetArtistTracksAsync(Artist.Source, providerArtistId, CancellationToken.None);
                var releases = await _onlineMusicService.GetArtistReleasesAsync(Artist.Source, providerArtistId, CancellationToken.None);
                ReplaceCollection(Tracks, tracks);
                ReplaceCollection(Albums, releases);
            }
        }
        finally
        {
            IsLoading = false;
            RaiseArtistProperties();
            PlayAllCommand.NotifyCanExecuteChanged();
            ShuffleCommand.NotifyCanExecuteChanged();
        }
    }

    private async Task PlayAllAsync()
    {
        if (Tracks.Count == 0)
        {
            return;
        }

        await _playbackService.PlayQueueAsync(Tracks.ToList(), Tracks[0], "artist");
    }

    private async Task ShuffleAsync()
    {
        if (Tracks.Count == 0)
        {
            return;
        }

        var shuffled = Tracks.OrderBy(_ => Random.Shared.Next()).ToList();
        await _playbackService.PlayQueueAsync(shuffled, shuffled[0], "artist");
    }

    private async Task PlayTrackAsync(Track? track)
    {
        if (track is null)
        {
            return;
        }

        await _playbackService.PlayQueueAsync(Tracks.ToList(), track, "artist");
    }

    private Task OpenAlbumAsync(Album? album) =>
        album is null ? Task.CompletedTask : _navigationService.NavigateAsync<AlbumPageViewModel>(album);

    private Task OpenRelatedArtistAsync(Artist? artist) =>
        artist is null ? Task.CompletedTask : _navigationService.NavigateAsync<ArtistPageViewModel>(artist);

    private void RaiseArtistProperties()
    {
        OnPropertyChanged(nameof(AboutText));
        OnPropertyChanged(nameof(MonthlyListenersLabel));
        OnPropertyChanged(nameof(FollowersLabel));
        OnPropertyChanged(nameof(TrackCountLabel));
        OnPropertyChanged(nameof(CountryLabel));
    }

    private static void ReplaceCollection<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }
}
