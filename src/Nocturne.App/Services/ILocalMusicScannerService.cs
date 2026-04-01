using Nocturne.App.Models;

namespace Nocturne.App.Services;

public interface ILocalMusicScannerService
{
    Task<IReadOnlyList<Track>> ScanAsync(IEnumerable<string> folders, CancellationToken cancellationToken);
}
