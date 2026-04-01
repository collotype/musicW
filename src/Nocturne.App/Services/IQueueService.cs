using Nocturne.App.Models;

namespace Nocturne.App.Services;

public interface IQueueService
{
    ObservableCollection<QueueItem> Items { get; }

    Track? CurrentTrack { get; }

    void ReplaceQueue(IEnumerable<Track> tracks, Track? currentTrack = null, string origin = "");

    bool TrySetCurrent(Track track);

    Track? MoveNext(bool shuffleEnabled);

    Track? MovePrevious();

    void Enqueue(Track track, string origin = "");
}
