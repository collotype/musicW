using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicApp.Models;
using MusicApp.Services;

namespace MusicApp.ViewModels;

public partial class QueueViewModel : ObservableObject
{
    private readonly IQueueService _queueService;
    private readonly IPlaybackService _playbackService;
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private List<QueueItem> _queueItems = new();

    [ObservableProperty]
    private QueueItem? _currentItem;

    [ObservableProperty]
    private bool _smartQueueEnabled;

    [ObservableProperty]
    private List<QueueItem> _upNextItems = new();

    [ObservableProperty]
    private List<QueueItem> _recommendedItems = new();

    public bool HasQueueItems => QueueItems.Count > 0;
    public bool HasUpNextItems => UpNextItems.Count > 0;
    public bool HasRecommendedItems => RecommendedItems.Count > 0;
    public string QueueSummary => HasQueueItems ? $"{QueueItems.Count} tracks ready." : "Queue is empty.";
    public string UpNextSummary => HasUpNextItems ? $"{UpNextItems.Count} explicit tracks still queued." : "No explicit up next items.";
    public string RecommendationSummary => HasRecommendedItems ? $"{RecommendedItems.Count} smart inserts are staged after the explicit queue." : "No smart inserts in the queue yet.";
    public string SmartQueueStatusSummary => SmartQueueEnabled
        ? "Smart queue will extend playback with explainable recommendations."
        : "Playback will stop after the explicit queue ends.";

    public QueueViewModel(
        IQueueService queueService,
        IPlaybackService playbackService,
        ISettingsService settingsService)
    {
        _queueService = queueService;
        _playbackService = playbackService;
        _settingsService = settingsService;

        _queueService.QueueChanged += OnQueueChanged;
        SmartQueueEnabled = _queueService.SmartQueueEnabled;
        Refresh();
    }

    partial void OnSmartQueueEnabledChanged(bool value)
    {
        _queueService.SmartQueueEnabled = value;
        _ = _settingsService.UpdateSettingsAsync(settings => settings.SmartQueueEnabled = value);
        OnPropertyChanged(nameof(SmartQueueStatusSummary));
    }

    private void OnQueueChanged(object? sender, EventArgs e)
    {
        Refresh();
    }

    public void Refresh()
    {
        QueueItems = _queueService.Queue.ToList();
        CurrentItem = _queueService.CurrentItem;
        var upcoming = _queueService.Queue.Skip(Math.Max(_queueService.CurrentIndex + 1, 0)).ToList();
        UpNextItems = upcoming.Where(item => !item.IsRecommendation).ToList();
        RecommendedItems = upcoming.Where(item => item.IsRecommendation).ToList();
        OnPropertyChanged(nameof(HasQueueItems));
        OnPropertyChanged(nameof(HasUpNextItems));
        OnPropertyChanged(nameof(HasRecommendedItems));
        OnPropertyChanged(nameof(QueueSummary));
        OnPropertyChanged(nameof(UpNextSummary));
        OnPropertyChanged(nameof(RecommendationSummary));
        OnPropertyChanged(nameof(SmartQueueStatusSummary));
    }

    [RelayCommand]
    private async Task PlayItem(QueueItem? item)
    {
        if (item == null)
        {
            return;
        }

        var index = QueueItems.FindIndex(queueItem => queueItem.Id == item.Id);
        if (index >= 0)
        {
            await _playbackService.PlayQueueAsync(QueueItems, index);
        }
    }

    [RelayCommand]
    private void RemoveItem(QueueItem? item)
    {
        if (item == null)
        {
            return;
        }

        var index = QueueItems.FindIndex(queueItem => queueItem.Id == item.Id);
        if (index >= 0)
        {
            _queueService.RemoveFromQueue(index);
        }
    }

    [RelayCommand]
    private void MoveUp(QueueItem? item)
    {
        if (item == null)
        {
            return;
        }

        var index = QueueItems.FindIndex(queueItem => queueItem.Id == item.Id);
        if (index > 0)
        {
            _queueService.MoveItem(index, index - 1);
        }
    }

    [RelayCommand]
    private void MoveDown(QueueItem? item)
    {
        if (item == null)
        {
            return;
        }

        var index = QueueItems.FindIndex(queueItem => queueItem.Id == item.Id);
        if (index >= 0 && index < QueueItems.Count - 1)
        {
            _queueService.MoveItem(index, index + 1);
        }
    }

    [RelayCommand]
    private void ClearQueue()
    {
        _queueService.ClearQueue();
    }
}
