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
    private readonly string _favoriteArtistsPath;
    private readonly string _savedAlbumsPath;
    private readonly string _timedCommentsPath;

    private List<Track> _tracks = new();
    private List<Playlist> _playlists = new();
    private List<string> _favoriteArtistIds = new();
    private List<string> _savedAlbumIds = new();
    private List<TimedComment> _timedComments = new();

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

    public List<string> FavoriteArtistIds
    {
        get => _favoriteArtistIds;
        set => _favoriteArtistIds = value;
    }

    public List<string> SavedAlbumIds
    {
        get => _savedAlbumIds;
        set => _savedAlbumIds = value;
    }

    public List<TimedComment> TimedComments
    {
        get => _timedComments;
        set => _timedComments = value;
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
        _favoriteArtistsPath = Path.Combine(_dataDirectory, "favorite-artists.json");
        _savedAlbumsPath = Path.Combine(_dataDirectory, "saved-albums.json");
        _timedCommentsPath = Path.Combine(_dataDirectory, "timed-comments.json");
    }

    public async Task LoadAllAsync()
    {
        await LoadTracksAsync();
        await LoadPlaylistsAsync();
        await LoadFavoriteArtistsAsync();
        await LoadSavedAlbumsAsync();
        await LoadTimedCommentsAsync();
    }

    public async Task SaveAllAsync()
    {
        await SaveTracksAsync();
        await SavePlaylistsAsync();
        await SaveFavoriteArtistsAsync();
        await SaveSavedAlbumsAsync();
        await SaveTimedCommentsAsync();
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

    private async Task LoadFavoriteArtistsAsync()
    {
        try
        {
            if (File.Exists(_favoriteArtistsPath))
            {
                var json = await File.ReadAllTextAsync(_favoriteArtistsPath);
                var ids = JsonConvert.DeserializeObject<List<string>>(json);
                if (ids != null)
                {
                    _favoriteArtistIds = ids;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load favorite artists: {ex.Message}");
        }
    }

    private async Task SaveFavoriteArtistsAsync()
    {
        try
        {
            var json = JsonConvert.SerializeObject(_favoriteArtistIds, Formatting.Indented);
            await File.WriteAllTextAsync(_favoriteArtistsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save favorite artists: {ex.Message}");
        }
    }

    private async Task LoadSavedAlbumsAsync()
    {
        try
        {
            if (File.Exists(_savedAlbumsPath))
            {
                var json = await File.ReadAllTextAsync(_savedAlbumsPath);
                var ids = JsonConvert.DeserializeObject<List<string>>(json);
                if (ids != null)
                {
                    _savedAlbumIds = ids;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load saved albums: {ex.Message}");
        }
    }

    private async Task SaveSavedAlbumsAsync()
    {
        try
        {
            var json = JsonConvert.SerializeObject(_savedAlbumIds, Formatting.Indented);
            await File.WriteAllTextAsync(_savedAlbumsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save saved albums: {ex.Message}");
        }
    }

    private async Task LoadTimedCommentsAsync()
    {
        try
        {
            if (File.Exists(_timedCommentsPath))
            {
                var json = await File.ReadAllTextAsync(_timedCommentsPath);
                var comments = JsonConvert.DeserializeObject<List<TimedComment>>(json);
                if (comments != null)
                {
                    _timedComments = comments;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load timed comments: {ex.Message}");
        }
    }

    private async Task SaveTimedCommentsAsync()
    {
        try
        {
            var json = JsonConvert.SerializeObject(_timedComments, Formatting.Indented);
            await File.WriteAllTextAsync(_timedCommentsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save timed comments: {ex.Message}");
        }
    }
}
