using Nocturne.App.Models;

namespace Nocturne.App.Services;

public interface IPlaybackService
{
    PlaybackState State { get; }

    event EventHandler? StateChanged;

    Task PlayQueueAsync(IReadOnlyList<Track> tracks, Track? startTrack = null, string origin = "");

    Task PlayTrackAsync(Track track, IReadOnlyList<Track>? queue = null, string origin = "");

    Task TogglePlayPauseAsync();

    Task NextAsync();

    Task PreviousAsync();

    Task SeekAsync(double progress);

    Task SetVolumeAsync(double volume);

    Task ToggleShuffleAsync();

    Task CycleRepeatModeAsync();
}
