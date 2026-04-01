using CommunityToolkit.Mvvm.Input;
using Nocturne.App.Helpers;
using Nocturne.App.Models;
using Nocturne.App.Models.Enums;
using Nocturne.App.Services;

namespace Nocturne.App.ViewModels;

public sealed partial class ShellViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly ILibraryService _libraryService;
    private readonly INotificationService _notificationService;
    private readonly ISettingsService _settingsService;

    private object? _currentPageViewModel;
    private LibraryCollection? _selectedSidebarItem;

    public ShellViewModel(
        INavigationService navigationService,
        ILibraryService libraryService,
        PlayerBarViewModel playerBar,
        INotificationService notificationService,
        ISettingsService settingsService)
    {
        _navigationService = navigationService;
        _libraryService = libraryService;
        _notificationService = notificationService;
        _settingsService = settingsService;
        PlayerBar = playerBar;

        PrimaryNavigation =
        [
            new NavigationItem { Glyph = "\uE8F1", Label = "Library", Target = NavigationTarget.Library, IsSelected = true },
            new NavigationItem { Glyph = "\uE721", Label = "Search", Target = NavigationTarget.Search },
            new NavigationItem { Glyph = "\uEB51", Label = "Favorites", Target = NavigationTarget.Favorites },
            new NavigationItem { Glyph = "\uE8FE", Label = "Offline", Target = NavigationTarget.OfflineTracks }
        ];

        FooterNavigation =
        [
            new NavigationItem { Glyph = "\uE713", Label = "Settings", Target = NavigationTarget.Settings }
        ];

        SelectNavigationCommand = new AsyncRelayCommand<NavigationItem?>(SelectNavigationAsync);
        SelectSidebarItemCommand = new AsyncRelayCommand<LibraryCollection?>(SelectSidebarAsync);
        CreatePlaylistCommand = new AsyncRelayCommand(CreatePlaylistAsync);

        _navigationService.CurrentViewModelChanged += (_, _) =>
        {
            CurrentPageViewModel = _navigationService.CurrentViewModel;
            SyncSidebarSelectionForCurrentPage();
        };

        _libraryService.LibraryChanged += (_, _) => RebuildSidebarCollections();
        _notificationService.NotificationChanged += (_, _) => OnPropertyChanged(nameof(CurrentNotification));
    }

    public string Title => Branding.ProductName;

    public string Subtitle => Branding.ProductSubtitle;

    public ObservableCollection<NavigationItem> PrimaryNavigation { get; }

    public ObservableCollection<NavigationItem> FooterNavigation { get; }

    public ObservableCollection<LibraryCollection> SidebarCollections { get; } = [];

    public IEnumerable<LibraryCollection> SystemCollections => SidebarCollections.Where(item => !item.IsPlaylist);

    public IEnumerable<LibraryCollection> UserPlaylists => SidebarCollections.Where(item => item.IsPlaylist);

    public int PlaylistCount => UserPlaylists.Count();

    public PlayerBarViewModel PlayerBar { get; }

    public object? CurrentPageViewModel
    {
        get => _currentPageViewModel;
        private set
        {
            if (SetProperty(ref _currentPageViewModel, value))
            {
                OnPropertyChanged(nameof(IsLibrarySidebarVisible));
            }
        }
    }

    public LibraryCollection? SelectedSidebarItem
    {
        get => _selectedSidebarItem;
        private set => SetProperty(ref _selectedSidebarItem, value);
    }

    public NotificationMessage? CurrentNotification => _notificationService.Current;

    public bool IsLibrarySidebarVisible => CurrentPageViewModel is not SettingsPageViewModel;

    public IAsyncRelayCommand<NavigationItem?> SelectNavigationCommand { get; }

    public IAsyncRelayCommand<LibraryCollection?> SelectSidebarItemCommand { get; }

    public IAsyncRelayCommand CreatePlaylistCommand { get; }

    public async Task InitializeAsync()
    {
        RebuildSidebarCollections();
        await NavigateAsync(ParseTarget(_settingsService.Current.LastVisitedPage));
    }

    private async Task SelectNavigationAsync(NavigationItem? navigationItem)
    {
        if (navigationItem is null)
        {
            return;
        }

        await NavigateAsync(navigationItem.Target);
    }

    private async Task SelectSidebarAsync(LibraryCollection? sidebarItem)
    {
        if (sidebarItem is null)
        {
            return;
        }

        SelectSidebarItem(sidebarItem);

        switch (sidebarItem.Type)
        {
            case CollectionType.Library:
                await NavigateAsync(NavigationTarget.Library);
                break;
            case CollectionType.Favorites:
                await NavigateAsync(NavigationTarget.Favorites);
                break;
            case CollectionType.OfflineTracks:
                await NavigateAsync(NavigationTarget.OfflineTracks);
                break;
            case CollectionType.Playlist:
                var playlist = _libraryService.FindPlaylist(sidebarItem.Id);
                if (playlist is not null)
                {
                    await _navigationService.NavigateAsync<PlaylistPageViewModel>(playlist);
                }

                break;
        }
    }

    private async Task CreatePlaylistAsync()
    {
        var playlist = new Playlist
        {
            Title = $"New Playlist {DateTime.Now:HHmm}",
            OwnerName = Branding.CompanyName,
            Description = "Fresh collection",
            CoverArtUrl = _libraryService.GetAlbums().FirstOrDefault()?.CoverArtUrl
        };

        await _libraryService.AddPlaylistAsync(playlist);
        RebuildSidebarCollections();
    }

    private async Task NavigateAsync(NavigationTarget target)
    {
        SelectNavigation(target);
        _settingsService.Current.LastVisitedPage = target.ToString();
        await _settingsService.SaveAsync();

        switch (target)
        {
            case NavigationTarget.Library:
                SelectSidebarByType(CollectionType.Library);
                await _navigationService.NavigateAsync<LibraryPageViewModel>();
                break;
            case NavigationTarget.Search:
                await _navigationService.NavigateAsync<SearchPageViewModel>();
                break;
            case NavigationTarget.Favorites:
                SelectSidebarByType(CollectionType.Favorites);
                await _navigationService.NavigateAsync<FavoritesPageViewModel>();
                break;
            case NavigationTarget.OfflineTracks:
                SelectSidebarByType(CollectionType.OfflineTracks);
                await _navigationService.NavigateAsync<OfflineTracksPageViewModel>();
                break;
            case NavigationTarget.Settings:
                await _navigationService.NavigateAsync<SettingsPageViewModel>();
                break;
        }
    }

    private void SelectNavigation(NavigationTarget target)
    {
        foreach (var item in PrimaryNavigation.Concat(FooterNavigation))
        {
            item.IsSelected = item.Target == target;
        }
    }

    private void RebuildSidebarCollections()
    {
        SidebarCollections.Clear();
        foreach (var item in _libraryService.BuildSidebarCollections())
        {
            SidebarCollections.Add(item);
        }

        OnPropertyChanged(nameof(SystemCollections));
        OnPropertyChanged(nameof(UserPlaylists));
        OnPropertyChanged(nameof(PlaylistCount));

        if (SidebarCollections.Count > 0 && SelectedSidebarItem is null)
        {
            SelectSidebarItem(SidebarCollections[0]);
        }
        else
        {
            SyncSidebarSelectionForCurrentPage();
        }
    }

    private void SyncSidebarSelectionForCurrentPage()
    {
        switch (CurrentPageViewModel)
        {
            case LibraryPageViewModel:
                SelectSidebarByType(CollectionType.Library);
                break;
            case FavoritesPageViewModel:
                SelectSidebarByType(CollectionType.Favorites);
                break;
            case OfflineTracksPageViewModel:
                SelectSidebarByType(CollectionType.OfflineTracks);
                break;
            case PlaylistPageViewModel playlistPageViewModel when playlistPageViewModel.Playlist is not null:
                var playlistItem = SidebarCollections.FirstOrDefault(item => item.Id == playlistPageViewModel.Playlist.Id);
                if (playlistItem is not null)
                {
                    SelectSidebarItem(playlistItem);
                }

                break;
        }
    }

    private void SelectSidebarByType(CollectionType collectionType)
    {
        var item = SidebarCollections.FirstOrDefault(candidate => candidate.Type == collectionType);
        if (item is not null)
        {
            SelectSidebarItem(item);
        }
    }

    private void SelectSidebarItem(LibraryCollection sidebarItem)
    {
        foreach (var item in SidebarCollections)
        {
            item.IsSelected = item == sidebarItem;
        }

        SelectedSidebarItem = sidebarItem;
    }

    private static NavigationTarget ParseTarget(string? value)
    {
        return Enum.TryParse<NavigationTarget>(value, out var target)
            ? target
            : NavigationTarget.Library;
    }
}
