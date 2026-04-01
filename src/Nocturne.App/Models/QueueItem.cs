namespace Nocturne.App.Models;

public sealed class QueueItem
{
    public required Track Track { get; init; }

    public string QueueOrigin { get; init; } = string.Empty;

    public bool IsCurrent { get; set; }
}
