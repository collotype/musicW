using Nocturne.App.Models;

namespace Nocturne.App.Services;

public sealed class QueueService : IQueueService
{
    private readonly Random _random = new();
    private int _currentIndex = -1;

    public ObservableCollection<QueueItem> Items { get; } = [];

    public Track? CurrentTrack =>
        _currentIndex >= 0 && _currentIndex < Items.Count
            ? Items[_currentIndex].Track
            : null;

    public void ReplaceQueue(IEnumerable<Track> tracks, Track? currentTrack = null, string origin = "")
    {
        Items.Clear();

        foreach (var track in tracks)
        {
            Items.Add(new QueueItem
            {
                Track = track,
                QueueOrigin = origin
            });
        }

        _currentIndex = currentTrack is null
            ? (Items.Count > 0 ? 0 : -1)
            : FindTrackIndex(currentTrack);

        UpdateSelection();
    }

    public bool TrySetCurrent(Track track)
    {
        var index = FindTrackIndex(track);
        if (index < 0)
        {
            return false;
        }

        _currentIndex = index;
        UpdateSelection();
        return true;
    }

    public Track? MoveNext(bool shuffleEnabled)
    {
        if (Items.Count == 0)
        {
            return null;
        }

        if (shuffleEnabled && Items.Count > 1)
        {
            var next = _currentIndex;
            while (next == _currentIndex)
            {
                next = _random.Next(0, Items.Count);
            }

            _currentIndex = next;
        }
        else if (_currentIndex < Items.Count - 1)
        {
            _currentIndex++;
        }
        else
        {
            return null;
        }

        UpdateSelection();
        return CurrentTrack;
    }

    public Track? MovePrevious()
    {
        if (Items.Count == 0)
        {
            return null;
        }

        if (_currentIndex > 0)
        {
            _currentIndex--;
        }

        UpdateSelection();
        return CurrentTrack;
    }

    public void Enqueue(Track track, string origin = "")
    {
        Items.Add(new QueueItem
        {
            Track = track,
            QueueOrigin = origin
        });

        if (_currentIndex < 0)
        {
            _currentIndex = 0;
            UpdateSelection();
        }
    }

    private int FindTrackIndex(Track track)
    {
        for (var index = 0; index < Items.Count; index++)
        {
            var candidate = Items[index].Track;
            if (candidate.Id == track.Id ||
                (!string.IsNullOrWhiteSpace(candidate.ProviderTrackId) && candidate.ProviderTrackId == track.ProviderTrackId))
            {
                return index;
            }
        }

        return -1;
    }

    private void UpdateSelection()
    {
        for (var index = 0; index < Items.Count; index++)
        {
            Items[index].IsCurrent = index == _currentIndex;
        }
    }
}
