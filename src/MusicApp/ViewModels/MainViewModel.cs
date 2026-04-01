using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MusicApp.Enums;
using MusicApp.Models;
using MusicApp.Services;
using MusicApp.Views;
using System.Windows.Controls;

namespace MusicApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
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

    private void OnNavigated(object? sender, NavigationEventArgs e)
    {
        CurrentPageType = e.PageType;
        CurrentParameter = e.Parameter;
        LoadPage(e.PageType, e.Parameter);
    }

    private void OnPlaybackStateChanged(object? sender, PlaybackState e)
    {
        // Update now playing from playback state
    }

    private void LoadPage(string pageType, object? parameter)
    {
        CurrentPage = pageType switch
        {
            "Library" => _serviceProvider.GetRequiredService<LibraryPage>(),
            "Search" => _serviceProvider.GetRequiredService<SearchPage>(),
            "Artist" => LoadArtistPage(parameter?.ToString()),
            "Album" => LoadAlbumPage(parameter?.ToString()),
            "Playlist" => LoadPlaylistPage(parameter?.ToString()),
            "Settings" => _serviceProvider.GetRequiredService<SettingsPage>(),
            _ => _serviceProvider.GetRequiredService<LibraryPage>()
        };
    }

    private UserControl LoadArtistPage(string? artistId)
    {
        var page = _serviceProvider.GetRequiredService<ArtistPage>();
        if (!string.IsNullOrEmpty(artistId))
        {
            var viewModel = _serviceProvider.GetRequiredService<ArtistViewModel>();
            page.DataContext = viewModel;
            _ = viewModel.LoadArtistAsync(artistId);
        }
        return page;
    }

    private UserControl LoadAlbumPage(string? albumId)
    {
        var page = _serviceProvider.GetRequiredService<AlbumPage>();
        if (!string.IsNullOrEmpty(albumId))
        {
            var viewModel = _serviceProvider.GetRequiredService<AlbumViewModel>();
            page.DataContext = viewModel;
            _ = viewModel.LoadAlbumAsync(albumId);
        }
        return page;
    }

    private UserControl LoadPlaylistPage(string? playlistId)
    {
        var page = _serviceProvider.GetRequiredService<PlaylistPage>();
        if (!string.IsNullOrEmpty(playlistId))
        {
            var viewModel = _serviceProvider.GetRequiredService<PlaylistViewModel>();
            page.DataContext = viewModel;
            _ = viewModel.LoadPlaylistAsync(playlistId);
        }
        return page;
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
