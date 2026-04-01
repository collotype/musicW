using Nocturne.App.Helpers;
using Nocturne.App.Models;
using Nocturne.App.Models.Enums;
using System.Security.Cryptography;
using TagLib;

namespace Nocturne.App.Services;

public sealed class LocalMusicScannerService : ILocalMusicScannerService
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3",
        ".wav",
        ".flac",
        ".m4a",
        ".aac",
        ".wma",
        ".ogg"
    };

    public async Task<IReadOnlyList<Track>> ScanAsync(IEnumerable<string> folders, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            var tracks = new List<Track>();

            foreach (var folder in folders.Where(Directory.Exists))
            {
                foreach (var path in SafeEnumerateFiles(folder))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!SupportedExtensions.Contains(Path.GetExtension(path)))
                    {
                        continue;
                    }

                    try
                    {
                        using var file = TagLib.File.Create(path);
                        var artworkPath = ExtractArtwork(path, file);

                        tracks.Add(new Track
                        {
                            Title = string.IsNullOrWhiteSpace(file.Tag.Title)
                                ? Path.GetFileNameWithoutExtension(path)
                                : file.Tag.Title,
                            ArtistName = file.Tag.FirstPerformer ?? "Unknown Artist",
                            AlbumTitle = file.Tag.Album,
                            Duration = file.Properties.Duration,
                            CoverArtUrl = artworkPath,
                            LocalFilePath = path,
                            Source = TrackSource.Local,
                            StorageLocation = StorageLocation.Library,
                            IsDownloaded = true,
                            Genres = file.Tag.Genres?.ToList() ?? [],
                            ReleaseDate = file.Tag.Year > 0
                                ? new DateTimeOffset(new DateTime((int)file.Tag.Year, 1, 1))
                                : null
                        });
                    }
                    catch
                    {
                    }
                }
            }

            return (IReadOnlyList<Track>)tracks;
        }, cancellationToken);
    }

    private static IEnumerable<string> SafeEnumerateFiles(string folder)
    {
        var stack = new Stack<string>();
        stack.Push(folder);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            IEnumerable<string> subFolders = [];
            IEnumerable<string> files = [];

            try
            {
                subFolders = Directory.EnumerateDirectories(current);
                files = Directory.EnumerateFiles(current);
            }
            catch
            {
            }

            foreach (var file in files)
            {
                yield return file;
            }

            foreach (var subFolder in subFolders)
            {
                stack.Push(subFolder);
            }
        }
    }

    private static string? ExtractArtwork(string audioPath, TagLib.File file)
    {
        var picture = file.Tag.Pictures.FirstOrDefault();
        if (picture is null)
        {
            return null;
        }

        var extension = picture.MimeType?.Contains("png", StringComparison.OrdinalIgnoreCase) == true ? ".png" : ".jpg";
        var hash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(audioPath))).ToLowerInvariant();
        var destinationPath = Path.Combine(AppPaths.ImageCacheFolder, $"{hash}{extension}");

        if (!System.IO.File.Exists(destinationPath))
        {
            System.IO.File.WriteAllBytes(destinationPath, picture.Data.Data);
        }

        return destinationPath;
    }
}
