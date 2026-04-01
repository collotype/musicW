using MusicApp.Models;

namespace MusicApp.Services;

public class QueueService : IQueueService
{
    private readonly List<QueueItem> _queue = new();
    private readonly List<QueueItem> _originalQueue = new();
    private int _currentIndex = -1;
    private readonly Random _random = new();

    public event EventHandler? QueueChanged;

    public List<QueueItem> Queue => _queue;
    public int CurrentIndex => _currentIndex;
    public QueueItem? CurrentItem => _currentIndex >= 0 && _currentIndex < _queue.Count ? _queue[_currentIndex] : null;

    public void SetQueue(List<QueueItem> queue, int startIndex = 0)
    {
        _originalQueue.Clear();
        _originalQueue.AddRange(queue);
        _queue.Clear();
        _queue.AddRange(queue);
        _currentIndex = startIndex;
        QueueChanged?.Invoke(this, EventArgs.Empty);
    }

    public void AddToQueue(Track track)
    {
        _queue.Add(QueueItem.FromTrack(track));
        QueueChanged?.Invoke(this, EventArgs.Empty);
    }

    public void AddToQueueNext(Track track)
    {
        _queue.Insert(_currentIndex + 1, QueueItem.FromTrack(track));
        QueueChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RemoveFromQueue(int index)
    {
        if (index >= 0 && index < _queue.Count)
        {
            _queue.RemoveAt(index);
            if (index < _currentIndex)
            {
                _currentIndex--;
            }
            QueueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void ClearQueue()
    {
        _queue.Clear();
        _originalQueue.Clear();
        _currentIndex = -1;
        QueueChanged?.Invoke(this, EventArgs.Empty);
    }

    public void MoveToNext()
    {
        if (_currentIndex < _queue.Count - 1)
        {
            _currentIndex++;
            QueueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void MoveToPrevious()
    {
        if (_currentIndex > 0)
        {
            _currentIndex = 0;
            QueueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Shuffle()
    {
        if (_queue.Count <= 1) return;

        // Keep current track in place, shuffle the rest
        var current = CurrentItem;
        var remaining = _queue.Where((_, i) => i != _currentIndex).ToList();

        for (int i = remaining.Count - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (remaining[i], remaining[j]) = (remaining[j], remaining[i]);
        }

        _queue.Clear();
        if (current != null)
        {
            _queue.Insert(_currentIndex, current);
        }
        _queue.AddRange(remaining);
        QueueChanged?.Invoke(this, EventArgs.Empty);
    }

    public QueueItem? GetNextTrack()
    {
        if (_currentIndex < _queue.Count - 1)
        {
            return _queue[_currentIndex + 1];
        }
        return null;
    }

    public QueueItem? GetPreviousTrack()
    {
        if (_currentIndex > 0)
        {
            return _queue[0]; // Go to start of current context
        }
        return null;
    }
}
