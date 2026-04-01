using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MusicApp.Diagnostics;
using MusicApp.Models;
using MusicApp.Services;
using MusicApp.Views;

namespace MusicApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private const string ProviderSeparator = "::";

    private readonly INavigationService _navigationService;
    private readonly IPlaybackService _playbackService;
    private readonly ILibraryService _libraryService;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private object? _currentPage;

    [ObservableProperty]
    private string _currentPageType = "Library";

    [ObservableProperty]
    private object? _currentParameter;

    [ObservableProperty]
    private bool _isLibrarySidebarVisible = true;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private LibraryViewModel _library;

    [ObservableProperty]
    private NowPlayingViewModel _nowPlaying;

    [ObservableProperty]
    private bool _isStartupBusy = true;

    [ObservableProperty]
    private string _startupStatusMessage = "Starting MusicApp...";

    [ObservableProperty]
    private string _startupErrorMessage = string.Empty;

    public MainViewModel(
        INavigationService navigationService,
        IPlaybackService playbackService,
        ILibraryService libraryService,
        LibraryViewModel library,
        NowPlayingViewModel nowPlaying,
        IServiceProvider serviceProvider)
    {
        _navigationService = navigationService;
        _playbackService = playbackService;
        _libraryService = libraryService;
        _library = library;
        _nowPlaying = nowPlaying;
        _serviceProvider = serviceProvider;

        _navigationService.Navigated += OnNavigated;
        _playbackService.StateChanged += OnPlaybackStateChanged;

        _library.LoadLibrary();
    }

    public void SetStartupStatus(string message)
    {
        StartupStatusMessage = message;
        StartupErrorMessage = string.Empty;
        IsStartupBusy = true;
    }

    public void CompleteStartup()
    {
        StartupErrorMessage = string.Empty;
        IsStartupBusy = false;
    }

    public void ReportStartupFailure(string message)
    {
        StartupErrorMessage = message;
        StartupStatusMessage = "Startup failed";
        IsStartupBusy = true;
    }

    private void OnNavigated(object? sender, NavigationEventArgs e)
    {
        CurrentPageType = e.PageType;
        CurrentParameter = e.Parameter ?? e.ItemId;
        LoadPage(e.PageType, e.Parameter ?? e.ItemId);
    }

    private void OnPlaybackStateChanged(object? sender, PlaybackState e)
    {
        // Update now playing from playback state
    }

    private void LoadPage(string pageType, object? parameter)
    {
        StartupDiagnostics.LogInfo($"Resolving page '{pageType}'.");

        CurrentPage = pageType switch
        {
            "Library" => CreatePage<LibraryPage, LibraryViewModel>(),
            "Search" => CreatePage<SearchPage, SearchViewModel>(),
            "Artist" => LoadArtistPage(parameter?.ToString()),
            "Album" => LoadAlbumPage(parameter?.ToString()),
            "Playlist" => LoadPlaylistPage(parameter?.ToString()),
            "Settings" => CreatePage<SettingsPage, SettingsViewModel>(),
            _ => CreatePage<LibraryPage, LibraryViewModel>()
        };

        StartupDiagnostics.LogInfo($"Current page set to '{pageType}'.");
    }

    private TPage CreatePage<TPage, TViewModel>()
        where TPage : FrameworkElement
        where TViewModel : class
    {
        var page = _serviceProvider.GetRequiredService<TPage>();
        page.DataContext ??= _serviceProvider.GetRequiredService<TViewModel>();
        return page;
    }

    private UserControl LoadArtistPage(string? artistId)
    {
        var page = CreatePage<ArtistPage, ArtistViewModel>();
        var navigationTarget = ParseNavigationTarget(artistId);

        if (page.DataContext is ArtistViewModel viewModel && !string.IsNullOrWhiteSpace(navigationTarget.ItemId))
        {
            _ = viewModel.LoadArtistAsync(navigationTarget.ItemId, navigationTarget.ProviderName);
        }

        return page;
    }

    private UserControl LoadAlbumPage(string? albumId)
    {
        var page = CreatePage<AlbumPage, AlbumViewModel>();
        var navigationTarget = ParseNavigationTarget(albumId);

        if (page.DataContext is AlbumViewModel viewModel && !string.IsNullOrWhiteSpace(navigationTarget.ItemId))
        {
            _ = viewModel.LoadAlbumAsync(navigationTarget.ItemId, navigationTarget.ProviderName);
        }

        return page;
    }

    private UserControl LoadPlaylistPage(string? playlistId)
    {
        var page = CreatePage<PlaylistPage, PlaylistViewModel>();
        var navigationTarget = ParseNavigationTarget(playlistId);

        if (page.DataContext is PlaylistViewModel viewModel && !string.IsNullOrWhiteSpace(navigationTarget.ItemId))
        {
            _ = viewModel.LoadPlaylistAsync(navigationTarget.ItemId, navigationTarget.ProviderName);
        }

        return page;
    }

    private static (string ItemId, string ProviderName) ParseNavigationTarget(string? navigationId)
    {
        if (string.IsNullOrWhiteSpace(navigationId))
        {
            return (string.Empty, "Local");
        }

        var parts = navigationId.Split(ProviderSeparator, 2, StringSplitOptions.None);
        if (parts.Length == 2)
        {
            return (parts[1], parts[0]);
        }

        return (navigationId, "Local");
    }

    [RelayCommand]
    private void NavigateToLibrary()
    {
        _navigationService.NavigateToLibrary();
    }

    [RelayCommand]
    private void NavigateToSearch()
    {
        _navigationService.NavigateToSearch();
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        _navigationService.NavigateToSettings();
    }

    [RelayCommand]
    private void ToggleLibrarySidebar()
    {
        IsLibrarySidebarVisible = !IsLibrarySidebarVisible;
    }

    [RelayCommand]
    private void PlayPause()
    {
        _ = _playbackService.TogglePlayPauseAsync();
    }

    [RelayCommand]
    private void Next()
    {
        _ = _playbackService.NextAsync();
    }

    [RelayCommand]
    private void Previous()
    {
        _ = _playbackService.PreviousAsync();
    }
}
