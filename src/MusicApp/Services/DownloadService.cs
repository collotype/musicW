using MusicApp.Models;
using MusicApp.Enums;
using System.Net.Http;
using System.IO;

namespace MusicApp.Services;

public class DownloadService : IDownloadService
{
    private readonly ILibraryService _libraryService;
    private readonly HttpClient _httpClient = new();
    private readonly Dictionary<string, CancellationTokenSource> _activeDownloads = new();

    public event EventHandler<DownloadProgressEventArgs>? ProgressChanged;

    public DownloadService(ILibraryService libraryService)
    {
        _libraryService = libraryService;
    }

    public async Task<bool> DownloadTrackAsync(Track track, string? destinationPath = null)
    {
        if (track.Source != TrackSource.Local && string.IsNullOrEmpty(track.StreamUrl))
        {
            OnProgressChanged(new DownloadProgressEventArgs
            {
                TrackId = track.Id,
                ErrorMessage = "Track is not downloadable"
            });
            return false;
        }

        var cts = new CancellationTokenSource();
        _activeDownloads[track.Id] = cts;

        try
        {
            string url;
            if (track.Source == TrackSource.Local)
            {
                // Copy local file to downloads folder
                url = track.LocalFilePath!;
            }
            else
            {
                url = track.StreamUrl!;
            }

            if (string.IsNullOrEmpty(destinationPath))
            {
                var downloadsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                var downloadDir = Path.Combine(downloadsFolder, "MusicApp Downloads");
                Directory.CreateDirectory(downloadDir);
                destinationPath = Path.Combine(downloadDir, $"{track.ArtistName} - {track.Title}.mp3");
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? 0;
            var downloadedBytes = 0L;

            await using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
            await using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer = new byte[81920];
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer, cts.Token)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cts.Token);
                downloadedBytes += bytesRead;

                if (totalBytes > 0)
                {
                    var progress = (double)downloadedBytes / totalBytes * 100;
                    OnProgressChanged(new DownloadProgressEventArgs
                    {
                        TrackId = track.Id,
                        Progress = progress
                    });
                }
            }

            // Update track as downloaded
            track.IsDownloaded = true;
            track.LocalFilePath = destinationPath;
            await _libraryService.AddTrackAsync(track);

            OnProgressChanged(new DownloadProgressEventArgs
            {
                TrackId = track.Id,
                Progress = 100,
                IsComplete = true
            });

            return true;
        }
        catch (OperationCanceledException)
        {
            OnProgressChanged(new DownloadProgressEventArgs
            {
                TrackId = track.Id,
                ErrorMessage = "Download cancelled"
            });
            return false;
        }
        catch (Exception ex)
        {
            OnProgressChanged(new DownloadProgressEventArgs
            {
                TrackId = track.Id,
                ErrorMessage = ex.Message
            });
            return false;
        }
        finally
        {
            _activeDownloads.Remove(track.Id);
            cts.Dispose();
        }
    }

    public Task CancelDownloadAsync(string trackId)
    {
        if (_activeDownloads.TryGetValue(trackId, out var cts))
        {
            cts.Cancel();
        }
        return Task.CompletedTask;
    }

    public Task<List<Track>> GetDownloadedTracksAsync()
    {
        return Task.FromResult(_libraryService.OfflineTracks);
    }

    public Task<bool> IsDownloadedAsync(string trackId)
    {
        var track = _libraryService.AllTracks.FirstOrDefault(t => t.Id == trackId);
        return Task.FromResult(track?.IsDownloaded ?? false);
    }

    public async Task DeleteDownloadAsync(string trackId)
    {
        var track = _libraryService.AllTracks.FirstOrDefault(t => t.Id == trackId);
        if (track != null && !string.IsNullOrEmpty(track.LocalFilePath) && File.Exists(track.LocalFilePath))
        {
            File.Delete(track.LocalFilePath);
            track.IsDownloaded = false;
            track.LocalFilePath = null;
            await _libraryService.AddTrackAsync(track);
        }
    }

    private void OnProgressChanged(DownloadProgressEventArgs args)
    {
        ProgressChanged?.Invoke(this, args);
    }
}
