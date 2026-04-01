using Nocturne.App.Models;
using Nocturne.App.Models.Enums;

namespace Nocturne.App.Services;

public sealed class DownloadService(
    IOnlineMusicService onlineMusicService,
    ILibraryService libraryService,
    INotificationService notificationService) : IDownloadService
{
    public async Task<bool> DownloadTrackAsync(Track track, CancellationToken cancellationToken)
    {
        var path = await onlineMusicService.DownloadTrackAsync(track, cancellationToken);
        if (string.IsNullOrWhiteSpace(path))
        {
            await notificationService.ShowAsync("Download unavailable", "This track cannot be saved for offline use right now.", NotificationLevel.Warning);
            return false;
        }

        track.LocalFilePath = path;
        track.StorageLocation = StorageLocation.Library;
        track.IsDownloaded = true;

        await libraryService.AddTrackToLibraryAsync(track);
        await notificationService.ShowAsync("Saved offline", $"{track.Title} is available from your library now.", NotificationLevel.Success);
        return true;
    }
}
