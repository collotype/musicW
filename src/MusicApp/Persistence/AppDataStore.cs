using System.IO;
using MusicApp.Models;
using Newtonsoft.Json;

namespace MusicApp.Persistence;

public class AppDataStore
{
    private readonly string _dataDirectory;
    private readonly string _tracksPath;
    private readonly string _playlistsPath;
    private readonly string _settingsPath;

    private List<Track> _tracks = new();
    private List<Playlist> _playlists = new();

    public List<Track> Tracks
    {
        get => _tracks;
        set => _tracks = value;
    }

    public List<Playlist> Playlists
    {
        get => _playlists;
        set => _playlists = value;
    }

    public AppDataStore()
    {
        _dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MusicApp", "Data");
        Directory.CreateDirectory(_dataDirectory);

        _tracksPath = Path.Combine(_dataDirectory, "tracks.json");
        _playlistsPath = Path.Combine(_dataDirectory, "playlists.json");
        _settingsPath = Path.Combine(_dataDirectory, "app-settings.json");
    }

    public async Task LoadAllAsync()
    {
        await LoadTracksAsync();
        await LoadPlaylistsAsync();
    }

    public async Task SaveAllAsync()
    {
        await SaveTracksAsync();
        await SavePlaylistsAsync();
    }

    private async Task LoadTracksAsync()
    {
        try
        {
            if (File.Exists(_tracksPath))
            {
                var json = await File.ReadAllTextAsync(_tracksPath);
                var tracks = JsonConvert.DeserializeObject<List<Track>>(json);
                if (tracks != null)
                {
                    _tracks = tracks;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load tracks: {ex.Message}");
        }
    }

    private async Task SaveTracksAsync()
    {
        try
        {
            var json = JsonConvert.SerializeObject(_tracks, Formatting.Indented);
            await File.WriteAllTextAsync(_tracksPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save tracks: {ex.Message}");
        }
    }

    private async Task LoadPlaylistsAsync()
    {
        try
        {
            if (File.Exists(_playlistsPath))
            {
                var json = await File.ReadAllTextAsync(_playlistsPath);
                var playlists = JsonConvert.DeserializeObject<List<Playlist>>(json);
                if (playlists != null)
                {
                    _playlists = playlists;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load playlists: {ex.Message}");
        }
    }

    private async Task SavePlaylistsAsync()
    {
        try
        {
            var json = JsonConvert.SerializeObject(_playlists, Formatting.Indented);
            await File.WriteAllTextAsync(_playlistsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save playlists: {ex.Message}");
        }
    }
}
