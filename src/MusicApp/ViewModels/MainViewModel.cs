using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MusicApp.Diagnostics;
using MusicApp.Enums;
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
    private readonly ISettingsService _settingsService;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private object? _currentPage;

    [ObservableProperty]
    private NavigationPage _currentPageType = NavigationPage.Home;

    [ObservableProperty]
    private LibrarySection _currentLibrarySection = LibrarySection.Overview;

    [ObservableProperty]
    private bool _isSidebarCollapsed;

    [ObservableProperty]
    private bool _isContextPanelVisible = true;

    [ObservableProperty]
    private ContextPanelMode _selectedContextPanelMode = ContextPanelMode.Queue;

    [ObservableProperty]
    private bool _isExpandedPlayerOpen;

    [ObservableProperty]
    private LibraryViewModel _library;

    [ObservableProperty]
    private HomeViewModel _home;

    [ObservableProperty]
    private MyWaveViewModel _myWave;

    [ObservableProperty]
    private QueueViewModel _queue;

    [ObservableProperty]
    private NowPlayingViewModel _nowPlaying;

    [ObservableProperty]
    private bool _isStartupBusy = true;

    [ObservableProperty]
    private string _startupStatusMessage = "Starting MusicApp...";

    [ObservableProperty]
    private string _startupErrorMessage = string.Empty;

    public bool IsHomeActive => CurrentPageType == NavigationPage.Home;
    public bool IsMyWaveActive => CurrentPageType == NavigationPage.MyWave;
    public bool IsSearchActive => CurrentPageType == NavigationPage.Search;
    public bool IsLibraryActive => CurrentPageType == NavigationPage.Library;
    public bool IsQueueActive => CurrentPageType == NavigationPage.Queue;
    public bool IsSettingsActive => CurrentPageType == NavigationPage.Settings;
    public bool IsQueueContextMode => SelectedContextPanelMode == ContextPanelMode.Queue;
    public bool IsLyricsContextMode => SelectedContextPanelMode == ContextPanelMode.Lyrics;
    public bool IsDetailsContextMode => SelectedContextPanelMode == ContextPanelMode.Details;
    public bool IsCommentsContextMode => SelectedContextPanelMode == ContextPanelMode.Comments;
    public bool IsWaveContextMode => SelectedContextPanelMode == ContextPanelMode.Wave;
    public string WorkspaceSummary => $"{Library.TotalTracks} tracks • {Library.TotalAlbums} albums • {Library.TotalArtists} artists";
    public string CurrentPageTitle => CurrentPageType switch
    {
        NavigationPage.Home => "Home",
        NavigationPage.MyWave => "My Wave",
        NavigationPage.Search => "Search",
        NavigationPage.Library => Library.SectionTitle,
        NavigationPage.Queue => "Queue",
        NavigationPage.Artist => "Artist",
        NavigationPage.Album => "Album",
        NavigationPage.Playlist => "Playlist",
        NavigationPage.Settings => "Settings",
        _ => "Music Workspace"
    };
    public string CurrentPageSubtitle => CurrentPageType switch
    {
        NavigationPage.Home => "Continue listening, launch discovery, and return to favorite artists.",
        NavigationPage.MyWave => "Blend familiarity and discovery with a desktop-first tuner.",
        NavigationPage.Search => "Search your indexed library and connected providers in one surface.",
        NavigationPage.Library => Library.SectionSubtitle,
        NavigationPage.Queue => "Inspect playback order, recommendations, and smart queue inserts.",
        NavigationPage.Artist => "Deep artist surface with tracks, releases, and related context.",
        NavigationPage.Album => "A complete album view with tracklist, metadata, and wave entry points.",
        NavigationPage.Playlist => "Curate, reorder, pin, and expand playlists without leaving the app.",
        NavigationPage.Settings => "Playback, discovery, storage, services, lyrics, and privacy controls.",
        _ => "A premium desktop listening workspace."
    };
    public string ContextPanelTitle => SelectedContextPanelMode switch
    {
        ContextPanelMode.Lyrics => "Lyrics",
        ContextPanelMode.Details => "Track Details",
        ContextPanelMode.Comments => "Timed Comments",
        ContextPanelMode.Wave => "Wave Notes",
        _ => "Up Next"
    };
    public string ContextPanelSubtitle => SelectedContextPanelMode switch
    {
        ContextPanelMode.Lyrics => NowPlaying.LyricsStatusMessage,
        ContextPanelMode.Details => NowPlaying.ContextSummary,
        ContextPanelMode.Comments => NowPlaying.CommentsSummary,
        ContextPanelMode.Wave => MyWave.TunerSummary,
        _ => NowPlaying.QueueCountLabel
    };

    public MainViewModel(
        INavigationService navigationService,
        IPlaybackService playbackService,
        ILibraryService libraryService,
        ISettingsService settingsService,
        LibraryViewModel library,
        HomeViewModel home,
        MyWaveViewModel myWave,
        QueueViewModel queue,
        NowPlayingViewModel nowPlaying,
        IServiceProvider serviceProvider)
    {
        _navigationService = navigationService;
        _playbackService = playbackService;
        _libraryService = libraryService;
        _settingsService = settingsService;
        _library = library;
        _home = home;
        _myWave = myWave;
        _queue = queue;
        _nowPlaying = nowPlaying;
        _serviceProvider = serviceProvider;

        IsSidebarCollapsed = _settingsService.Settings.CompactSidebar;
        IsContextPanelVisible = _settingsService.Settings.ShowContextPanelByDefault;

        _navigationService.Navigated += OnNavigated;
        _playbackService.StateChanged += OnPlaybackStateChanged;
        _library.PropertyChanged += OnLibraryPropertyChanged;
        _library.LoadLibrary();
        _home.Refresh();
    }

    private void OnLibraryPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (CurrentPageType != NavigationPage.Library)
        {
            return;
        }

        if (e.PropertyName == nameof(LibraryViewModel.SectionTitle) ||
            e.PropertyName == nameof(LibraryViewModel.SectionSubtitle) ||
            e.PropertyName == nameof(LibraryViewModel.SelectedSection) ||
            e.PropertyName == nameof(LibraryViewModel.TotalTracks) ||
            e.PropertyName == nameof(LibraryViewModel.TotalAlbums) ||
            e.PropertyName == nameof(LibraryViewModel.TotalArtists) ||
            e.PropertyName == nameof(LibraryViewModel.PinnedPlaylists) ||
            e.PropertyName == nameof(LibraryViewModel.UserPlaylists))
        {
            OnPropertyChanged(nameof(CurrentPageTitle));
            OnPropertyChanged(nameof(CurrentPageSubtitle));
            OnPropertyChanged(nameof(WorkspaceSummary));
        }
    }

    partial void OnSelectedContextPanelModeChanged(ContextPanelMode value)
    {
        OnPropertyChanged(nameof(IsQueueContextMode));
        OnPropertyChanged(nameof(IsLyricsContextMode));
        OnPropertyChanged(nameof(IsDetailsContextMode));
        OnPropertyChanged(nameof(IsCommentsContextMode));
        OnPropertyChanged(nameof(IsWaveContextMode));
        OnPropertyChanged(nameof(ContextPanelTitle));
        OnPropertyChanged(nameof(ContextPanelSubtitle));
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

    public void ApplyShellSettings()
    {
        IsSidebarCollapsed = _settingsService.Settings.CompactSidebar;
        IsContextPanelVisible = _settingsService.Settings.ShowContextPanelByDefault;
    }

    private void OnNavigated(object? sender, NavigationEventArgs e)
    {
        CurrentPageType = e.Request.Page;
        CurrentLibrarySection = e.Request.LibrarySection;
        _ = LoadPageAsync(e.Request);
        NotifyNavigationStateChanged();
    }

    private void OnPlaybackStateChanged(object? sender, PlaybackState e)
    {
        if (CurrentPageType == NavigationPage.Queue)
        {
            Queue.Refresh();
        }
    }

    private async Task LoadPageAsync(NavigationRequest request)
    {
        StartupDiagnostics.LogInfo($"Resolving page '{request.Page}'.");

        CurrentPage = request.Page switch
        {
            NavigationPage.Home => LoadHomePage(),
            NavigationPage.MyWave => await LoadWavePageAsync(request),
            NavigationPage.Search => LoadSearchPage(request.Query),
            NavigationPage.Library => LoadLibraryPage(request.LibrarySection),
            NavigationPage.Queue => LoadQueuePage(),
            NavigationPage.Artist => await LoadArtistPageAsync(request.ItemId, request.ProviderName),
            NavigationPage.Album => await LoadAlbumPageAsync(request.ItemId, request.ProviderName),
            NavigationPage.Playlist => await LoadPlaylistPageAsync(request.ItemId, request.ProviderName),
            NavigationPage.Settings => CreatePage<SettingsPage, SettingsViewModel>(),
            _ => LoadHomePage()
        };

        if (request.Page == NavigationPage.MyWave)
        {
            SelectedContextPanelMode = ContextPanelMode.Wave;
        }

        StartupDiagnostics.LogInfo($"Current page set to '{request.Page}'.");
    }

    private TPage CreatePage<TPage, TViewModel>()
        where TPage : FrameworkElement
        where TViewModel : class
    {
        var page = _serviceProvider.GetRequiredService<TPage>();
        page.DataContext ??= _serviceProvider.GetRequiredService<TViewModel>();
        return page;
    }

    private UserControl LoadHomePage()
    {
        Home.Refresh();
        return CreatePage<HomePage, HomeViewModel>();
    }

    private async Task<UserControl> LoadWavePageAsync(NavigationRequest request)
    {
        var page = CreatePage<MyWavePage, MyWaveViewModel>();
        if (page.DataContext is MyWaveViewModel viewModel)
        {
            await viewModel.SetSeedAsync(request.WaveSeed ?? WaveSeed.Home());
        }

        return page;
    }

    private UserControl LoadSearchPage(string? query)
    {
        var page = CreatePage<SearchPage, SearchViewModel>();
        if (page.DataContext is SearchViewModel viewModel && !string.IsNullOrWhiteSpace(query))
        {
            viewModel.SearchQuery = query;
        }

        return page;
    }

    private UserControl LoadLibraryPage(LibrarySection section)
    {
        var page = CreatePage<LibraryPage, LibraryViewModel>();
        if (page.DataContext is LibraryViewModel viewModel)
        {
            viewModel.LoadLibrary();
            viewModel.SetSection(section);
        }

        return page;
    }

    private UserControl LoadQueuePage()
    {
        Queue.Refresh();
        return CreatePage<QueuePage, QueueViewModel>();
    }

    private async Task<UserControl> LoadArtistPageAsync(string? artistId, string providerName)
    {
        var page = CreatePage<ArtistPage, ArtistViewModel>();
        var navigationTarget = ParseNavigationTarget(artistId, providerName);
        if (page.DataContext is ArtistViewModel viewModel && !string.IsNullOrWhiteSpace(navigationTarget.ItemId))
        {
            await viewModel.LoadArtistAsync(navigationTarget.ItemId, navigationTarget.ProviderName);
        }

        return page;
    }

    private async Task<UserControl> LoadAlbumPageAsync(string? albumId, string providerName)
    {
        var page = CreatePage<AlbumPage, AlbumViewModel>();
        var navigationTarget = ParseNavigationTarget(albumId, providerName);
        if (page.DataContext is AlbumViewModel viewModel && !string.IsNullOrWhiteSpace(navigationTarget.ItemId))
        {
            await viewModel.LoadAlbumAsync(navigationTarget.ItemId, navigationTarget.ProviderName);
        }

        return page;
    }

    private async Task<UserControl> LoadPlaylistPageAsync(string? playlistId, string providerName)
    {
        var page = CreatePage<PlaylistPage, PlaylistViewModel>();
        var navigationTarget = ParseNavigationTarget(playlistId, providerName);
        if (page.DataContext is PlaylistViewModel viewModel && !string.IsNullOrWhiteSpace(navigationTarget.ItemId))
        {
            await viewModel.LoadPlaylistAsync(navigationTarget.ItemId, navigationTarget.ProviderName);
        }

        return page;
    }

    private static (string ItemId, string ProviderName) ParseNavigationTarget(string? navigationId, string fallbackProviderName)
    {
        if (string.IsNullOrWhiteSpace(navigationId))
        {
            return (string.Empty, fallbackProviderName);
        }

        var parts = navigationId.Split(ProviderSeparator, 2, StringSplitOptions.None);
        if (parts.Length == 2)
        {
            return (parts[1], parts[0]);
        }

        return (navigationId, fallbackProviderName);
    }

    [RelayCommand]
    private void NavigateToHome()
    {
        _navigationService.NavigateToHome();
    }

    [RelayCommand]
    private void NavigateToMyWave()
    {
        _navigationService.NavigateToMyWave();
    }

    [RelayCommand]
    private void NavigateToSearch()
    {
        _navigationService.NavigateToSearch();
    }

    [RelayCommand]
    private void NavigateToLibrary()
    {
        _navigationService.NavigateToLibrary();
    }

    [RelayCommand]
    private void NavigateToLibrarySection(LibrarySection section)
    {
        _navigationService.NavigateToLibrary(section);
    }

    [RelayCommand]
    private void NavigateToQueue()
    {
        _navigationService.NavigateToQueue();
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        _navigationService.NavigateToSettings();
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarCollapsed = !IsSidebarCollapsed;
        _ = _settingsService.UpdateSettingsAsync(settings => settings.CompactSidebar = IsSidebarCollapsed);
    }

    [RelayCommand]
    private void ToggleContextPanel()
    {
        IsContextPanelVisible = !IsContextPanelVisible;
        _ = _settingsService.UpdateSettingsAsync(settings => settings.ShowContextPanelByDefault = IsContextPanelVisible);
    }

    [RelayCommand]
    private void NavigateToPlaylist(string? playlistId)
    {
        if (!string.IsNullOrWhiteSpace(playlistId))
        {
            _navigationService.NavigateToPlaylist(playlistId);
        }
    }

    [RelayCommand]
    private void ShowQueueContext()
    {
        IsContextPanelVisible = true;
        SelectedContextPanelMode = ContextPanelMode.Queue;
    }

    [RelayCommand]
    private void ShowLyricsContext()
    {
        IsContextPanelVisible = true;
        SelectedContextPanelMode = ContextPanelMode.Lyrics;
    }

    [RelayCommand]
    private void ShowDetailsContext()
    {
        IsContextPanelVisible = true;
        SelectedContextPanelMode = ContextPanelMode.Details;
    }

    [RelayCommand]
    private void ShowCommentsContext()
    {
        IsContextPanelVisible = true;
        SelectedContextPanelMode = ContextPanelMode.Comments;
    }

    [RelayCommand]
    private void ShowWaveContext()
    {
        IsContextPanelVisible = true;
        SelectedContextPanelMode = ContextPanelMode.Wave;
    }

    [RelayCommand]
    private void ToggleExpandedPlayer()
    {
        IsExpandedPlayerOpen = !IsExpandedPlayerOpen;
    }

    [RelayCommand]
    private void CloseExpandedPlayer()
    {
        IsExpandedPlayerOpen = false;
    }

    private void NotifyNavigationStateChanged()
    {
        OnPropertyChanged(nameof(IsHomeActive));
        OnPropertyChanged(nameof(IsMyWaveActive));
        OnPropertyChanged(nameof(IsSearchActive));
        OnPropertyChanged(nameof(IsLibraryActive));
        OnPropertyChanged(nameof(IsQueueActive));
        OnPropertyChanged(nameof(IsSettingsActive));
        OnPropertyChanged(nameof(CurrentPageTitle));
        OnPropertyChanged(nameof(CurrentPageSubtitle));
        OnPropertyChanged(nameof(WorkspaceSummary));
        OnPropertyChanged(nameof(ContextPanelSubtitle));
    }
}
