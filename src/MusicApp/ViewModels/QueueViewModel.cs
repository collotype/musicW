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

    public bool HasQueueItems => QueueItems.Count > 0;
    public string QueueSummary => HasQueueItems ? $"{QueueItems.Count} tracks ready." : "Queue is empty.";

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
    }

    private void OnQueueChanged(object? sender, EventArgs e)
    {
        Refresh();
    }

    public void Refresh()
    {
        QueueItems = _queueService.Queue.ToList();
        CurrentItem = _queueService.CurrentItem;
        OnPropertyChanged(nameof(HasQueueItems));
        OnPropertyChanged(nameof(QueueSummary));
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
