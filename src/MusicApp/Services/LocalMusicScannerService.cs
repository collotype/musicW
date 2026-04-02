using MusicApp.Enums;
using MusicApp.Models;
using NAudio.Wave;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MusicApp.Services;

public class LocalMusicScannerService : ILocalMusicScannerService
{
    private readonly ILibraryService _libraryService;
    private readonly ISettingsService _settingsService;
    private readonly List<string> _supportedExtensions = new() { ".mp3", ".wav", ".flac", ".m4a", ".wma", ".ogg", ".aac" };

    public event EventHandler? ScanCompleted;

    public LocalMusicScannerService(ILibraryService libraryService, ISettingsService settingsService)
    {
        _libraryService = libraryService;
        _settingsService = settingsService;
    }

    public async Task InitializeAsync()
    {
        if (_settingsService.Settings.AutoScanOnStartup)
        {
            await ScanLibraryAsync();
        }
    }

    public async Task ScanLibraryAsync()
    {
        var folders = _settingsService.Settings.LibraryFolders;

        if (folders.Count == 0)
        {
            // Default to Music folder
            var musicFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            if (Directory.Exists(musicFolder))
            {
                folders = new List<string> { musicFolder };
            }
        }

        foreach (var folder in folders)
        {
            if (Directory.Exists(folder))
            {
                await ScanFolderAsync(folder);
            }
        }

        ScanCompleted?.Invoke(this, EventArgs.Empty);
    }

    public async Task ScanFolderAsync(string path)
    {
        try
        {
            var files = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
                .Where(f => _supportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));

            foreach (var file in files)
            {
                await ScanFileAsync(file);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error scanning folder {path}: {ex.Message}");
        }
    }

    public async Task ImportFilesAsync(IEnumerable<string> filePaths)
    {
        foreach (var filePath in filePaths
                     .Where(File.Exists)
                     .Where(f => _supportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            await ScanFileAsync(filePath);
        }

        ScanCompleted?.Invoke(this, EventArgs.Empty);
    }

    private async Task ScanFileAsync(string filePath)
    {
        try
        {
            var track = await ParseAudioFileAsync(filePath);
            if (track != null)
            {
                await _libraryService.AddTrackAsync(track);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error parsing file {filePath}: {ex.Message}");
        }
    }

    private Task<Track?> ParseAudioFileAsync(string filePath)
    {
        var normalizedPath = Path.GetFullPath(filePath);
        var track = new Track
        {
            Id = CreateStableTrackId(normalizedPath),
            Source = TrackSource.Local,
            StorageLocation = StorageLocation.Library,
            LocalFilePath = normalizedPath,
            Title = Path.GetFileNameWithoutExtension(normalizedPath),
            IsLiked = false,
            IsDownloaded = true
        };

        try
        {
            using var audioFile = new AudioFileReader(filePath);
            var tagReader = new Id3v2TagReader(filePath);

            if (tagReader.HasTag)
            {
                track.Title = string.IsNullOrEmpty(tagReader.Title) ? track.Title : tagReader.Title;
                track.ArtistName = string.IsNullOrEmpty(tagReader.Artist) ? "Unknown Artist" : tagReader.Artist;
                track.AlbumTitle = tagReader.Album ?? string.Empty;
                track.TrackNumber = tagReader.Track;
                track.DiscNumber = tagReader.Disc;

                // Get duration from audio file
                track.Duration = audioFile.TotalTime;

                // Try to extract embedded artwork
                if (tagReader.HasArtwork)
                {
                    var artworkPath = SaveArtworkToFile(tagReader.Artwork, track.Id);
                    if (!string.IsNullOrEmpty(artworkPath))
                    {
                        track.CoverArtUrl = artworkPath;
                    }
                }
            }
            else
            {
                track.Duration = audioFile.TotalTime;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error reading tags from {filePath}: {ex.Message}");
            // Return basic track info even if tag reading fails
            track.Duration = TimeSpan.Zero;
        }

        return Task.FromResult<Track?>(track);
    }

    private static string CreateStableTrackId(string filePath)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(filePath.ToLowerInvariant()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private string? SaveArtworkToFile(byte[] artworkData, string trackId)
    {
        try
        {
            var cacheDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MusicApp", "Artwork");
            Directory.CreateDirectory(cacheDir);

            var artworkPath = Path.Combine(cacheDir, $"{trackId}.jpg");
            File.WriteAllBytes(artworkPath, artworkData);
            return artworkPath;
        }
        catch
        {
            return null;
        }
    }

    public Task RefreshMetadataAsync(string trackId)
    {
        var track = _libraryService.AllTracks.FirstOrDefault(t => t.Id == trackId);
        if (track != null && !string.IsNullOrEmpty(track.LocalFilePath))
        {
            return ScanFileAsync(track.LocalFilePath);
        }
        return Task.CompletedTask;
    }
}

// Simple ID3 tag reader
public class Id3v2TagReader
{
    public string Title { get; private set; } = string.Empty;
    public string Artist { get; private set; } = string.Empty;
    public string Album { get; private set; } = string.Empty;
    public int Track { get; private set; }
    public int Disc { get; private set; } = 1;
    public byte[]? Artwork { get; private set; }
    public bool HasTag { get; private set; }
    public bool HasArtwork => Artwork != null && Artwork.Length > 0;

    public Id3v2TagReader(string filePath)
    {
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(stream);

            // Check for ID3v2 tag
            var header = reader.ReadChars(3);
            if (new string(header) != "ID3")
            {
                return;
            }

            stream.Position = 0;
            var tagData = reader.ReadBytes(10); // Skip header
            var endPosition = stream.Length;

            // Simple frame parsing
            while (stream.Position < endPosition - 10)
            {
                var frameId = new string(reader.ReadChars(4));
                var frameSize = reader.ReadInt32BE();
                reader.ReadUInt16(); // Skip flags

                if (frameSize <= 0 || frameSize > 1000000) break;

                var frameData = reader.ReadBytes(frameSize);

                switch (frameId)
                {
                    case "TIT2":
                        Title = DecodeFrame(frameData);
                        break;
                    case "TPE1":
                        Artist = DecodeFrame(frameData);
                        break;
                    case "TALB":
                        Album = DecodeFrame(frameData);
                        break;
                    case "TRCK":
                        Track = int.TryParse(DecodeFrame(frameData), out var t) ? t : 0;
                        break;
                    case "TPOS":
                        var pos = DecodeFrame(frameData).Split('/');
                        Disc = int.TryParse(pos[0], out var d) ? d : 1;
                        break;
                    case "APIC":
                        // Extract artwork
                        var encoding = frameData[0];
                        var mimeTypeEnd = Array.IndexOf(frameData, (byte)0, 1);
                        var pictureType = frameData[mimeTypeEnd + 2];
                        var imgStart = mimeTypeEnd + 3;
                        if (imgStart < frameData.Length)
                        {
                            Artwork = frameData.Skip(imgStart).ToArray();
                        }
                        break;
                }
            }

            HasTag = !string.IsNullOrEmpty(Title) || !string.IsNullOrEmpty(Artist) || !string.IsNullOrEmpty(Album);
        }
        catch
        {
            HasTag = false;
        }
    }

    private static string DecodeFrame(byte[] data)
    {
        if (data.Length == 0) return string.Empty;

        // Skip encoding byte
        var encoding = data[0];
        var content = data.Skip(1).ToArray();

        // Remove null terminators
        var nullIndex = Array.IndexOf(content, (byte)0);
        if (nullIndex > 0)
        {
            content = content.Take(nullIndex).ToArray();
        }

        try
        {
            return encoding switch
            {
                0 => System.Text.Encoding.Default.GetString(content),
                1 => System.Text.Encoding.Unicode.GetString(content),
                2 => System.Text.Encoding.BigEndianUnicode.GetString(content),
                3 => System.Text.Encoding.UTF8.GetString(content),
                _ => System.Text.Encoding.Default.GetString(content)
            };
        }
        catch
        {
            return string.Empty;
        }
    }
}

public static class BinaryReaderExtensions
{
    public static int ReadInt32BE(this BinaryReader reader)
    {
        var bytes = reader.ReadBytes(4);
        Array.Reverse(bytes);
        return BitConverter.ToInt32(bytes, 0);
    }
}
