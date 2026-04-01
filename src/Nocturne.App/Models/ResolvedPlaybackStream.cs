namespace Nocturne.App.Models;

public sealed class ResolvedPlaybackStream
{
    public required string StreamUrl { get; init; }

    public required string ProviderName { get; init; }

    public required string StreamType { get; init; }
}
