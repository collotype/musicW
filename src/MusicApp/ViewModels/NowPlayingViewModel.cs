using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicApp.Enums;
using MusicApp.Models;
using MusicApp.Services;

namespace MusicApp.ViewModels;

public partial class NowPlayingViewModel : ObservableObject
{
    private readonly IPlaybackService _playbackService;
    private readonly ILibraryService _libraryService;
    private readonly IQueueService _queueService;
    private readonly ILyricsService _lyricsService;
    private readonly ITimedCommentService _timedCommentService;
    private readonly IDownloadService _downloadService;
    private readonly INavigationService _navigationService;

    private bool _isApplyingPlaybackState;
    private string _currentTrackId = string.Empty;

    [ObservableProperty]
    private Track? _currentTrack;

    [ObservableProperty]
    private string _title = "Not Playing";

    [ObservableProperty]
    private string _artist = "-";

    [ObservableProperty]
    private string _coverArtUrl = string.Empty;

    [ObservableProperty]
    private TimeSpan _currentPosition;

    [ObservableProperty]
    private TimeSpan _totalDuration;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private bool _isLiked;

    [ObservableProperty]
    private double _volume = 0.8;

    [ObservableProperty]
    private bool _isMuted;

    [ObservableProperty]
    private RepeatMode _repeatMode;

    [ObservableProperty]
    private bool _isShuffle;

    [ObservableProperty]
    private string _currentPositionFormatted = "0:00";

    [ObservableProperty]
    private string _totalDurationFormatted = "0:00";

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private LyricsDocument _lyrics = new();

    [ObservableProperty]
    private List<LyricLine> _lyricLines = new();

    [ObservableProperty]
    private List<TimedComment> _timedComments = new();

    [ObservableProperty]
    private List<QueueItem> _queueItems = new();

    [ObservableProperty]
    private List<QueueItem> _upNextItems = new();

    [ObservableProperty]
    private string _newCommentText = string.Empty;

    [ObservableProperty]
    private string _contextSummary = "Queue and lyrics will appear here once playback starts.";

    [ObservableProperty]
    private bool _isDownloaded;

    [ObservableProperty]
    private double _downloadProgress;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasTrack => CurrentTrack != null;
    public bool HasLyrics => LyricLines.Count > 0;
    public bool HasTimedLyrics => Lyrics.IsTimed && LyricLines.Count > 0;
    public bool HasComments => TimedComments.Count > 0;
    public bool HasQueueItems => QueueItems.Count > 0;
    public bool CanAddComment => HasTrack && !string.IsNullOrWhiteSpace(NewCommentText);
    public string QueueCountLabel => QueueItems.Count == 0 ? "Queue is empty." : $"{QueueItems.Count} tracks in queue";
    public string LyricsStatusMessage => Lyrics.StatusMessage;

    public NowPlayingViewModel(
        IPlaybackService playbackService,
        ILibraryService libraryService,
        IQueueService queueService,
        ILyricsService lyricsService,
        ITimedCommentService timedCommentService,
        IDownloadService downloadService,
        INavigationService navigationService)
    {
        _playbackService = playbackService;
        _libraryService = libraryService;
        _queueService = queueService;
        _lyricsService = lyricsService;
        _timedCommentService = timedCommentService;
        _downloadService = downloadService;
        _navigationService = navigationService;

        _playbackService.StateChanged += OnPlaybackStateChanged;
        _queueService.QueueChanged += OnQueueChanged;
        _timedCommentService.CommentsChanged += OnCommentsChanged;
        _downloadService.ProgressChanged += OnDownloadProgressChanged;

        ApplyState(_playbackService.CurrentState);
        RefreshQueue();
    }

    partial void OnNewCommentTextChanged(string value)
    {
        OnPropertyChanged(nameof(CanAddComment));
    }

    private void OnPlaybackStateChanged(object? sender, PlaybackState state)
    {
        if (Application.Current.Dispatcher.CheckAccess())
        {
            ApplyState(state);
            return;
        }

        Application.Current.Dispatcher.Invoke(() => ApplyState(state));
    }

    private void OnQueueChanged(object? sender, EventArgs e)
    {
        if (Application.Current.Dispatcher.CheckAccess())
        {
            RefreshQueue();
            return;
        }

        Application.Current.Dispatcher.Invoke(RefreshQueue);
    }

    private void OnCommentsChanged(object? sender, string trackId)
    {
        if (!string.Equals(trackId, _currentTrackId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _ = LoadCommentsAsync(trackId);
    }

    private void OnDownloadProgressChanged(object? sender, DownloadProgressEventArgs e)
    {
        if (!string.Equals(CurrentTrack?.Id, e.TrackId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        DownloadProgress = e.Progress;
        if (e.IsComplete)
        {
            IsDownloaded = true;
        }
    }

    private void ApplyState(PlaybackState state)
    {
        _isApplyingPlaybackState = true;

        CurrentPosition = state.CurrentPosition;
        TotalDuration = state.TotalDuration;
        Progress = state.ProgressPercent;
        IsPlaying = state.Status == PlaybackStatus.Playing;
        Volume = state.Volume;
        IsMuted = state.IsMuted;
        RepeatMode = state.RepeatMode;
        IsShuffle = state.IsShuffle;
        CurrentPositionFormatted = state.CurrentPositionFormatted;
        TotalDurationFormatted = state.TotalDurationFormatted;
        ErrorMessage = state.ErrorMessage ?? string.Empty;

        if (state.CurrentTrack?.Track is { } track)
        {
            CurrentTrack = track;
            Title = track.Title;
            Artist = track.ArtistName;
            CoverArtUrl = track.CoverArtUrl ?? string.Empty;
            IsLiked = track.IsLiked;
            IsDownloaded = track.IsDownloaded;
            ContextSummary = state.CurrentTrack.RecommendationReason ??
                             state.CurrentTrack.SourceContext ??
                             "Playing from your library.";

            if (!string.Equals(_currentTrackId, track.Id, StringComparison.OrdinalIgnoreCase))
            {
                _currentTrackId = track.Id;
                _ = LoadTrackExperienceAsync(track);
            }
            else
            {
                UpdateActiveLyrics(state.CurrentPosition);
            }
        }
        else
        {
            CurrentTrack = null;
            Title = "Not Playing";
            Artist = "-";
            CoverArtUrl = string.Empty;
            IsLiked = false;
            IsDownloaded = false;
            Lyrics = new LyricsDocument
            {
                IsAvailable = false,
                StatusMessage = "Start playback to open lyrics."
            };
            LyricLines = new List<LyricLine>();
            TimedComments = new List<TimedComment>();
            ContextSummary = "Queue and lyrics will appear here once playback starts.";
            _currentTrackId = string.Empty;
        }

        _isApplyingPlaybackState = false;
        NotifyStateFlags();
    }

    private async Task LoadTrackExperienceAsync(Track track)
    {
        Lyrics = await _lyricsService.GetLyricsAsync(track);
        LyricLines = Lyrics.Lines;
        UpdateActiveLyrics(CurrentPosition);
        await LoadCommentsAsync(track.Id);
        NotifyStateFlags();
    }

    private async Task LoadCommentsAsync(string trackId)
    {
        TimedComments = await _timedCommentService.GetCommentsAsync(trackId);
        NotifyStateFlags();
    }

    private void UpdateActiveLyrics(TimeSpan position)
    {
        if (!Lyrics.IsTimed)
        {
            return;
        }

        LyricLine? activeLine = null;
        foreach (var line in LyricLines)
        {
            if (line.Timestamp.HasValue && line.Timestamp.Value <= position)
            {
                activeLine = line;
            }
        }

        foreach (var line in LyricLines)
        {
            line.IsActive = ReferenceEquals(line, activeLine);
        }
    }

    private void RefreshQueue()
    {
        QueueItems = _queueService.Queue.Select(item => item).ToList();
        UpNextItems = _queueService.Queue.Skip(Math.Max(_queueService.CurrentIndex + 1, 0)).Take(8).ToList();
        NotifyStateFlags();
    }

    partial void OnVolumeChanged(double value)
    {
        if (_isApplyingPlaybackState)
        {
            return;
        }

        _ = _playbackService.SetVolumeAsync(value);
    }

    partial void OnProgressChanged(double value)
    {
        if (_isApplyingPlaybackState || TotalDuration.TotalSeconds <= 0)
        {
            return;
        }

        _ = Seek(value);
    }

    [RelayCommand]
    private Task TogglePlayPause()
    {
        return _playbackService.TogglePlayPauseAsync();
    }

    [RelayCommand]
    private Task Next()
    {
        return _playbackService.NextAsync();
    }

    [RelayCommand]
    private Task Previous()
    {
        return _playbackService.PreviousAsync();
    }

    [RelayCommand]
    private Task ToggleLike()
    {
        return _playbackService.LikeCurrentTrackAsync();
    }

    [RelayCommand]
    private Task SetVolume(double volume)
    {
        return _playbackService.SetVolumeAsync(volume);
    }

    [RelayCommand]
    private Task ToggleMute()
    {
        return _playbackService.ToggleMuteAsync();
    }

    [RelayCommand]
    private Task CycleRepeatMode()
    {
        var nextMode = RepeatMode switch
        {
            RepeatMode.None => RepeatMode.All,
            RepeatMode.All => RepeatMode.One,
            _ => RepeatMode.None
        };

        return _playbackService.SetRepeatModeAsync(nextMode);
    }

    [RelayCommand]
    private Task ToggleShuffle()
    {
        return _playbackService.ToggleShuffleAsync();
    }

    [RelayCommand]
    private async Task Seek(double position)
    {
        if (TotalDuration.TotalSeconds <= 0)
        {
            return;
        }

        var timePosition = TimeSpan.FromSeconds(position / 100 * TotalDuration.TotalSeconds);
        await _playbackService.SeekAsync(timePosition);
    }

    [RelayCommand]
    private async Task SeekToLyric(LyricLine? line)
    {
        if (line?.Timestamp == null)
        {
            return;
        }

        await _playbackService.SeekAsync(line.Timestamp.Value);
    }

    [RelayCommand]
    private async Task AddComment()
    {
        if (!CanAddComment || CurrentTrack == null)
        {
            return;
        }

        await _timedCommentService.AddCommentAsync(CurrentTrack.Id, CurrentPosition, NewCommentText.Trim());
        NewCommentText = string.Empty;
        OnPropertyChanged(nameof(CanAddComment));
    }

    [RelayCommand]
    private Task DeleteComment(TimedComment? comment)
    {
        return comment == null ? Task.CompletedTask : _timedCommentService.DeleteCommentAsync(comment.Id);
    }

    [RelayCommand]
    private Task ToggleFavoriteMoment(TimedComment? comment)
    {
        return comment == null ? Task.CompletedTask : _timedCommentService.ToggleFavoriteMomentAsync(comment.Id);
    }

    [RelayCommand]
    private async Task JumpToComment(TimedComment? comment)
    {
        if (comment == null)
        {
            return;
        }

        await _playbackService.SeekAsync(comment.Timestamp);
    }

    [RelayCommand]
    private async Task PlayQueueItem(QueueItem? queueItem)
    {
        if (queueItem == null)
        {
            return;
        }

        var index = QueueItems.FindIndex(item => item.Id == queueItem.Id);
        if (index < 0)
        {
            return;
        }

        await _playbackService.PlayQueueAsync(QueueItems, index);
    }

    [RelayCommand]
    private void RemoveQueueItem(QueueItem? queueItem)
    {
        if (queueItem == null)
        {
            return;
        }

        var index = QueueItems.FindIndex(item => item.Id == queueItem.Id);
        if (index >= 0)
        {
            _queueService.RemoveFromQueue(index);
        }
    }

    [RelayCommand]
    private void MoveQueueItemUp(QueueItem? queueItem)
    {
        if (queueItem == null)
        {
            return;
        }

        var index = QueueItems.FindIndex(item => item.Id == queueItem.Id);
        if (index > 0)
        {
            _queueService.MoveItem(index, index - 1);
        }
    }

    [RelayCommand]
    private void MoveQueueItemDown(QueueItem? queueItem)
    {
        if (queueItem == null)
        {
            return;
        }

        var index = QueueItems.FindIndex(item => item.Id == queueItem.Id);
        if (index >= 0 && index < QueueItems.Count - 1)
        {
            _queueService.MoveItem(index, index + 1);
        }
    }

    [RelayCommand]
    private async Task DownloadCurrentTrack()
    {
        if (CurrentTrack == null || IsDownloaded)
        {
            return;
        }

        DownloadProgress = 0;
        await _downloadService.DownloadTrackAsync(CurrentTrack);
        var libraryTrack = _libraryService.AllTracks.FirstOrDefault(track => track.Id == CurrentTrack.Id);
        if (libraryTrack != null)
        {
            IsDownloaded = libraryTrack.IsDownloaded;
        }
    }

    [RelayCommand]
    private void NavigateToArtist()
    {
        if (CurrentTrack != null)
        {
            _navigationService.NavigateToArtist(CurrentTrack.ArtistId);
        }
    }

    [RelayCommand]
    private void NavigateToAlbum()
    {
        if (CurrentTrack != null && !string.IsNullOrWhiteSpace(CurrentTrack.AlbumId))
        {
            _navigationService.NavigateToAlbum(CurrentTrack.AlbumId);
        }
    }

    private void NotifyStateFlags()
    {
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(HasTrack));
        OnPropertyChanged(nameof(HasLyrics));
        OnPropertyChanged(nameof(HasTimedLyrics));
        OnPropertyChanged(nameof(HasComments));
        OnPropertyChanged(nameof(HasQueueItems));
        OnPropertyChanged(nameof(CanAddComment));
        OnPropertyChanged(nameof(QueueCountLabel));
        OnPropertyChanged(nameof(LyricsStatusMessage));
    }
}
