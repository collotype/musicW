using CommunityToolkit.Mvvm.Input;
using Nocturne.App.Models;
using Nocturne.App.Services;

namespace Nocturne.App.ViewModels;

public sealed class SettingsPageViewModel : PageViewModelBase
{
    private readonly ISettingsService _settingsService;

    public SettingsPageViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;

        SaveCommand = new AsyncRelayCommand(_settingsService.SaveAsync);
        ClearCacheCommand = new AsyncRelayCommand(_settingsService.ClearCacheAsync);
    }

    public SettingsModel Settings => _settingsService.Current;

    public ObservableCollection<string> LocalMusicFolders => Settings.LocalMusicFolders;

    public IAsyncRelayCommand SaveCommand { get; }

    public IAsyncRelayCommand ClearCacheCommand { get; }
}
