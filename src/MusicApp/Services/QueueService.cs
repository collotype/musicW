using MusicApp.Models;

namespace MusicApp.Services;

public class QueueService : IQueueService
{
    private readonly List<QueueItem> _queue = new();
    private readonly Random _random = new();
    private int _currentIndex = -1;

    public event EventHandler? QueueChanged;

    public List<QueueItem> Queue => _queue;
    public int CurrentIndex => _currentIndex;
    public QueueItem? CurrentItem => _currentIndex >= 0 && _currentIndex < _queue.Count ? _queue[_currentIndex] : null;
    public bool SmartQueueEnabled { get; set; } = true;

    public void SetQueue(List<QueueItem> queue, int startIndex = 0)
    {
        _queue.Clear();
        _queue.AddRange(queue);
        _currentIndex = Math.Clamp(startIndex, queue.Count == 0 ? -1 : 0, queue.Count == 0 ? -1 : queue.Count - 1);
        UpdateQueueState();
    }

    public void AddToQueue(Track track)
    {
        _queue.Add(QueueItem.FromTrack(track));
        UpdateQueueState();
    }

    public void AddToQueueNext(Track track)
    {
        var insertIndex = _currentIndex < 0 ? 0 : _currentIndex + 1;
        _queue.Insert(insertIndex, QueueItem.FromTrack(track));
        UpdateQueueState();
    }

    public void RemoveFromQueue(int index)
    {
        if (index < 0 || index >= _queue.Count)
        {
            return;
        }

        _queue.RemoveAt(index);
        if (_queue.Count == 0)
        {
            _currentIndex = -1;
        }
        else if (index < _currentIndex)
        {
            _currentIndex--;
        }
        else if (_currentIndex >= _queue.Count)
        {
            _currentIndex = _queue.Count - 1;
        }

        UpdateQueueState();
    }

    public void ClearQueue()
    {
        _queue.Clear();
        _currentIndex = -1;
        UpdateQueueState();
    }

    public void MoveToNext()
    {
        if (_currentIndex < _queue.Count - 1)
        {
            _currentIndex++;
            UpdateQueueState();
        }
        else
        {
            ClearPlayingFlags();
            QueueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void MoveToPrevious()
    {
        if (_currentIndex <= 0)
        {
            return;
        }

        _currentIndex--;
        UpdateQueueState();
    }

    public void MoveItem(int oldIndex, int newIndex)
    {
        if (oldIndex < 0 || oldIndex >= _queue.Count || newIndex < 0 || newIndex >= _queue.Count || oldIndex == newIndex)
        {
            return;
        }

        var item = _queue[oldIndex];
        _queue.RemoveAt(oldIndex);
        _queue.Insert(newIndex, item);

        if (_currentIndex == oldIndex)
        {
            _currentIndex = newIndex;
        }
        else if (oldIndex < _currentIndex && newIndex >= _currentIndex)
        {
            _currentIndex--;
        }
        else if (oldIndex > _currentIndex && newIndex <= _currentIndex)
        {
            _currentIndex++;
        }

        UpdateQueueState();
    }

    public void SetCurrentIndex(int index)
    {
        if (index < 0 || index >= _queue.Count)
        {
            return;
        }

        _currentIndex = index;
        UpdateQueueState();
    }

    public void Shuffle()
    {
        if (_queue.Count <= 1)
        {
            return;
        }

        var current = CurrentItem;
        var remaining = _queue.Where((_, index) => index != _currentIndex).ToList();

        for (var i = remaining.Count - 1; i > 0; i--)
        {
            var swapIndex = _random.Next(i + 1);
            (remaining[i], remaining[swapIndex]) = (remaining[swapIndex], remaining[i]);
        }

        _queue.Clear();
        if (current != null)
        {
            _queue.Add(current);
            _currentIndex = 0;
        }

        _queue.AddRange(remaining);
        UpdateQueueState();
    }

    public void AppendRecommendations(IEnumerable<QueueItem> recommendations)
    {
        foreach (var item in recommendations.Where(item => _queue.All(existing => existing.Track.Id != item.Track.Id)))
        {
            _queue.Add(item);
        }

        UpdateQueueState();
    }

    public QueueItem? GetNextTrack()
    {
        return _currentIndex < _queue.Count - 1 ? _queue[_currentIndex + 1] : null;
    }

    public QueueItem? GetPreviousTrack()
    {
        return _currentIndex > 0 ? _queue[_currentIndex - 1] : null;
    }

    private void UpdateQueueState()
    {
        ClearPlayingFlags();

        if (_currentIndex >= 0 && _currentIndex < _queue.Count)
        {
            _queue[_currentIndex].IsPlaying = true;
        }

        QueueChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ClearPlayingFlags()
    {
        foreach (var item in _queue)
        {
            item.IsPlaying = false;
        }
    }
}
