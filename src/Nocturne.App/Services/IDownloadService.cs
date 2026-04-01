using Nocturne.App.Models;

namespace Nocturne.App.Services;

public interface IDownloadService
{
    Task<bool> DownloadTrackAsync(Track track, CancellationToken cancellationToken);
}
