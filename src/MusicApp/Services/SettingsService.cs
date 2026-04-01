using System.IO;
using MusicApp.Models;
using Newtonsoft.Json;

namespace MusicApp.Services;

public class SettingsService : ISettingsService
{
    private readonly string _settingsPath;
    private SettingsModel _settings = new();

    public event EventHandler<SettingsModel>? SettingsChanged;
    public SettingsModel Settings => _settings;

    public SettingsService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MusicApp");
        Directory.CreateDirectory(appDataPath);
        _settingsPath = Path.Combine(appDataPath, "settings.json");
    }

    public async Task LoadSettingsAsync()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = await File.ReadAllTextAsync(_settingsPath);
                var loaded = JsonConvert.DeserializeObject<SettingsModel>(json);
                if (loaded != null)
                {
                    _settings = loaded;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
        }
    }

    public async Task SaveSettingsAsync()
    {
        try
        {
            var json = JsonConvert.SerializeObject(_settings, Formatting.Indented);
            await File.WriteAllTextAsync(_settingsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    public async Task UpdateSettingsAsync(Action<SettingsModel> update)
    {
        update(_settings);
        await SaveSettingsAsync();
        SettingsChanged?.Invoke(this, _settings);
    }
}
