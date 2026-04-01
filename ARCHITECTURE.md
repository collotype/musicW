# Nocturne Solution Architecture

This document is the canonical map for the WPF desktop codebase in this repository. It exists to keep future files consistent with the names, namespaces, DI wiring, theme tokens, and provider contracts already in place.

## Canonical conventions

- Product working name: `Nocturne`
- Solution file: `Nocturne.sln`
- Main project: `src/Nocturne.App`
- Assembly and root namespace: `Nocturne.App`
- UI stack: `.NET 8`, `WPF`, `MVVM`, `Microsoft.Extensions.Hosting`
- Styling system: merged WPF resource dictionaries under `Theme`

### Namespace map

- `Nocturne.App`: app entry and shell
- `Nocturne.App.Controls`: reusable custom WPF controls
- `Nocturne.App.Converters`: XAML value converters
- `Nocturne.App.Helpers`: constants and application path helpers
- `Nocturne.App.Models`: domain and shell models
- `Nocturne.App.Models.Enums`: shared enums
- `Nocturne.App.Persistence`: JSON-backed persistence primitives
- `Nocturne.App.Providers`: pluggable music providers
- `Nocturne.App.Services`: application services and service interfaces
- `Nocturne.App.ViewModels`: shell and page view models
- `Nocturne.App.Views.Pages`: page views

### Naming rules that must remain stable

- Providers implement `IMusicProvider` and expose a matching `TrackSource`.
- Service interfaces stay in `Services` with `I*` naming and one concrete implementation each unless a real multi-implementation case exists.
- Page view models use the `*PageViewModel` suffix.
- Page views use the `*Page.xaml` naming under `Views/Pages`.
- Theme resource keys use stable prefixes:
  - `Color.*`
  - `Brush.*`
  - `Radius.*`
  - `Spacing.*`
  - `Font.*`
  - `Text.*`
- `App.xaml` owns the global resource merge and the view-model-to-view data templates.
- `App.xaml.cs` owns DI registration and startup sequencing.

### Intentional naming edge case

There is both a model and a control named `SidebarCollectionItem`.

- Model: `Nocturne.App.Models.SidebarCollectionItem`
- Control: `Nocturne.App.Controls.SidebarCollectionItem`

This is currently valid and build-safe because the namespaces differ, but future edits should keep the namespace explicit when ambiguity is possible.

## Runtime composition

### Shell layout

- `MainWindow.xaml`
  - fixed narrow navigation rail
  - fixed expanded library sidebar
  - central content host bound to the current page view model
  - persistent bottom player
  - notification surface

### Page set

- `LibraryPage`
- `FavoritesPage`
- `OfflineTracksPage`
- `SearchPage`
- `ArtistPage`
- `AlbumPage`
- `PlaylistPage`
- `SettingsPage`

### Navigation and view resolution

- `ShellViewModel` owns the main navigation state.
- `INavigationService` creates and switches page view models.
- `App.xaml` maps each page view model to its page view with a `DataTemplate`.

When adding a new page, keep this sequence:

1. Create `*PageViewModel` in `ViewModels`.
2. Create `*Page.xaml` and `*Page.xaml.cs` in `Views/Pages`.
3. Register the view model in `App.xaml.cs`.
4. Add the `DataTemplate` in `App.xaml`.
5. Add navigation entry points in `ShellViewModel` and related UI.

## DI and startup wiring

DI is centralized in `src/Nocturne.App/App.xaml.cs`.

### Named `HttpClient` registrations

- `soundcloud`
- `spotify`
- `images`

### Singleton services

- `ISettingsService`
- `IMockDataService`
- `INotificationService`
- `IImageCacheService`
- `ILocalMusicScannerService`
- `ILibraryService`
- `IQueueService`
- `INavigationService`
- `IPlaybackService`
- `IDownloadService`
- `IOnlineMusicService`
- `ISearchService`

### Provider registrations

- `LocalLibraryProvider`
- `SoundCloudProvider`
- `SpotifyProvider`
- each also registered as `IMusicProvider`

### View model registrations

- singleton:
  - `PlayerBarViewModel`
  - `ShellViewModel`
- transient:
  - `LibraryPageViewModel`
  - `FavoritesPageViewModel`
  - `OfflineTracksPageViewModel`
  - `SearchPageViewModel`
  - `ArtistPageViewModel`
  - `AlbumPageViewModel`
  - `PlaylistPageViewModel`
  - `SettingsPageViewModel`

### Startup sequence

1. `AppPaths.EnsureCreated()`
2. build `IHost`
3. `ISettingsService.InitializeAsync()`
4. `ILibraryService.InitializeAsync()`
5. `ShellViewModel.InitializeAsync()`
6. resolve and show `MainWindow`

Do not move page initialization into constructors if it would block the UI thread. The current pattern deliberately keeps async setup in the startup flow and view-model initialization methods.

## Theme token inventory

Defined in `src/Nocturne.App/Theme/Colors.xaml` and `src/Nocturne.App/Theme/Typography.xaml`.

### Color tokens

- `Color.AppBackground`
- `Color.ShellBackground`
- `Color.PanelBackground`
- `Color.PanelElevated`
- `Color.PanelRaised`
- `Color.PanelHover`
- `Color.Border`
- `Color.TextPrimary`
- `Color.TextSecondary`
- `Color.TextMuted`
- `Color.Accent`
- `Color.AccentStrong`
- `Color.AccentDark`
- `Color.Danger`

### Brush tokens

- `Brush.AppBackground`
- `Brush.ShellBackground`
- `Brush.PanelBackground`
- `Brush.PanelElevated`
- `Brush.PanelRaised`
- `Brush.PanelHover`
- `Brush.Border`
- `Brush.TextPrimary`
- `Brush.TextSecondary`
- `Brush.TextMuted`
- `Brush.Accent`
- `Brush.AccentStrong`
- `Brush.AccentDark`
- `Brush.Danger`
- `Brush.HeaderOverlay`
- `Brush.HeroFade`

### Shared shape and spacing tokens

- `Radius.Small`
- `Radius.Medium`
- `Radius.Large`
- `Spacing.XS`
- `Spacing.S`
- `Spacing.M`
- `Spacing.L`

### Typography tokens

- `Font.Ui`
- `Font.Icon`
- `Text.Display`
- `Text.SectionTitle`
- `Text.Body`
- `Text.Caption`

Future visual work should extend these dictionaries before inventing page-local colors or styles.

## Source tree

Only the source tree is listed here. Build output and the iOS reference clone are excluded from the app structure below.

```text
Nocturne.sln
README.md
ARCHITECTURE.md
src/
  Nocturne.App/
    Nocturne.App.csproj
    App.xaml
    App.xaml.cs
    AssemblyInfo.cs
    GlobalUsings.cs
    MainWindow.xaml
    MainWindow.xaml.cs
    Assets/
      Fonts/
        Manrope.ttf
    Controls/
      AlbumCard.xaml
      AlbumCard.xaml.cs
      ArtistCircleCard.xaml
      ArtistCircleCard.xaml.cs
      BlurredBackgroundPresenter.xaml
      BlurredBackgroundPresenter.xaml.cs
      EmptyStateView.xaml
      EmptyStateView.xaml.cs
      HeroHeader.xaml
      HeroHeader.xaml.cs
      LibrarySidebar.xaml
      LibrarySidebar.xaml.cs
      NavRailButton.xaml
      NavRailButton.xaml.cs
      NavRailProfileButton.xaml
      NavRailProfileButton.xaml.cs
      NowPlayingMiniCard.xaml
      NowPlayingMiniCard.xaml.cs
      PlaybackControls.xaml
      PlaybackControls.xaml.cs
      PlaylistCard.xaml
      PlaylistCard.xaml.cs
      SearchBar.xaml
      SearchBar.xaml.cs
      SectionHeader.xaml
      SectionHeader.xaml.cs
      SidebarCollectionItem.xaml
      SidebarCollectionItem.xaml.cs
      StyledControls.cs
      TagChip.xaml
      TagChip.xaml.cs
      TrackCard.xaml
      TrackCard.xaml.cs
      TrackListView.xaml
      TrackListView.xaml.cs
      TrackRowControl.xaml
      TrackRowControl.xaml.cs
    Converters/
      InverseBooleanToVisibilityConverter.cs
      NullToVisibilityConverter.cs
      RepeatModeToGlyphConverter.cs
      TimeSpanToClockStringConverter.cs
    Helpers/
      AppPaths.cs
      Branding.cs
      HttpConstants.cs
    Models/
      Album.cs
      Artist.cs
      NavigationItem.cs
      NotificationMessage.cs
      PlaybackState.cs
      Playlist.cs
      QueueItem.cs
      ResolvedPlaybackStream.cs
      SearchResults.cs
      SettingsModel.cs
      SidebarCollectionItem.cs
      Track.cs
      UserLibrary.cs
      Enums/
        CommonEnums.cs
    Persistence/
      JsonFileStore.cs
    Providers/
      IMusicProvider.cs
      LocalLibraryProvider.cs
      SoundCloudProvider.cs
      SpotifyProvider.cs
    Services/
      DownloadService.cs
      IDownloadService.cs
      IImageCacheService.cs
      ILibraryService.cs
      ILocalMusicScannerService.cs
      ImageCacheService.cs
      IMockDataService.cs
      INavigationService.cs
      INotificationService.cs
      IOnlineMusicService.cs
      IPlaybackService.cs
      IQueueService.cs
      ISearchService.cs
      ISettingsService.cs
      LibraryService.cs
      LocalMusicScannerService.cs
      MockDataService.cs
      NavigationService.cs
      NotificationService.cs
      OnlineMusicService.cs
      PlaybackService.cs
      QueueService.cs
      SearchService.cs
      SettingsService.cs
    Theme/
      Colors.xaml
      Controls.xaml
      Typography.xaml
    ViewModels/
      AlbumPageViewModel.cs
      ArtistPageViewModel.cs
      FavoritesPageViewModel.cs
      INavigationAware.cs
      LibraryPageViewModel.cs
      OfflineTracksPageViewModel.cs
      PageViewModelBase.cs
      PlayerBarViewModel.cs
      PlaylistPageViewModel.cs
      SearchPageViewModel.cs
      SettingsPageViewModel.cs
      ShellViewModel.cs
      ViewModelBase.cs
    Views/
      Pages/
        AlbumPage.xaml
        AlbumPage.xaml.cs
        ArtistPage.xaml
        ArtistPage.xaml.cs
        FavoritesPage.xaml
        FavoritesPage.xaml.cs
        LibraryPage.xaml
        LibraryPage.xaml.cs
        OfflineTracksPage.xaml
        OfflineTracksPage.xaml.cs
        PlaylistPage.xaml
        PlaylistPage.xaml.cs
        SearchPage.xaml
        SearchPage.xaml.cs
        SettingsPage.xaml
        SettingsPage.xaml.cs
```

## Incremental file generation order

This is the stable order to follow when extending or recreating the project from scratch. It keeps names, DI, and theme references aligned from the start.

### 1. Solution and project skeleton

- `Nocturne.sln`
- `src/Nocturne.App/Nocturne.App.csproj`
- `src/Nocturne.App/GlobalUsings.cs`
- `src/Nocturne.App/AssemblyInfo.cs`

### 2. App composition and global wiring

- `src/Nocturne.App/App.xaml`
- `src/Nocturne.App/App.xaml.cs`
- `src/Nocturne.App/Helpers/Branding.cs`
- `src/Nocturne.App/Helpers/AppPaths.cs`
- `src/Nocturne.App/Helpers/HttpConstants.cs`

### 3. Theme foundation before page work

- `src/Nocturne.App/Assets/Fonts/Manrope.ttf`
- `src/Nocturne.App/Theme/Colors.xaml`
- `src/Nocturne.App/Theme/Typography.xaml`
- `src/Nocturne.App/Theme/Controls.xaml`

### 4. Core domain types

- `src/Nocturne.App/Models/Enums/CommonEnums.cs`
- `src/Nocturne.App/Models/Track.cs`
- `src/Nocturne.App/Models/Artist.cs`
- `src/Nocturne.App/Models/Album.cs`
- `src/Nocturne.App/Models/Playlist.cs`
- `src/Nocturne.App/Models/QueueItem.cs`
- `src/Nocturne.App/Models/SearchResults.cs`
- `src/Nocturne.App/Models/PlaybackState.cs`
- `src/Nocturne.App/Models/UserLibrary.cs`
- `src/Nocturne.App/Models/SettingsModel.cs`
- `src/Nocturne.App/Models/ResolvedPlaybackStream.cs`
- `src/Nocturne.App/Models/NotificationMessage.cs`
- `src/Nocturne.App/Models/NavigationItem.cs`
- `src/Nocturne.App/Models/SidebarCollectionItem.cs`

### 5. Persistence and service contracts

- `src/Nocturne.App/Persistence/JsonFileStore.cs`
- `src/Nocturne.App/Services/ISettingsService.cs`
- `src/Nocturne.App/Services/IMockDataService.cs`
- `src/Nocturne.App/Services/INotificationService.cs`
- `src/Nocturne.App/Services/IImageCacheService.cs`
- `src/Nocturne.App/Services/ILocalMusicScannerService.cs`
- `src/Nocturne.App/Services/ILibraryService.cs`
- `src/Nocturne.App/Services/IQueueService.cs`
- `src/Nocturne.App/Services/INavigationService.cs`
- `src/Nocturne.App/Services/IPlaybackService.cs`
- `src/Nocturne.App/Services/IDownloadService.cs`
- `src/Nocturne.App/Services/IOnlineMusicService.cs`
- `src/Nocturne.App/Services/ISearchService.cs`
- `src/Nocturne.App/Providers/IMusicProvider.cs`

### 6. Shared shell view models and concrete services

- `src/Nocturne.App/ViewModels/ViewModelBase.cs`
- `src/Nocturne.App/ViewModels/PageViewModelBase.cs`
- `src/Nocturne.App/ViewModels/INavigationAware.cs`
- `src/Nocturne.App/Services/SettingsService.cs`
- `src/Nocturne.App/Services/NotificationService.cs`
- `src/Nocturne.App/Services/ImageCacheService.cs`
- `src/Nocturne.App/Services/LocalMusicScannerService.cs`
- `src/Nocturne.App/Services/LibraryService.cs`
- `src/Nocturne.App/Services/QueueService.cs`
- `src/Nocturne.App/Services/NavigationService.cs`
- `src/Nocturne.App/Services/PlaybackService.cs`
- `src/Nocturne.App/Services/DownloadService.cs`
- `src/Nocturne.App/Services/MockDataService.cs`

### 7. Provider implementations and online orchestration

- `src/Nocturne.App/Providers/LocalLibraryProvider.cs`
- `src/Nocturne.App/Providers/SoundCloudProvider.cs`
- `src/Nocturne.App/Providers/SpotifyProvider.cs`
- `src/Nocturne.App/Services/OnlineMusicService.cs`
- `src/Nocturne.App/Services/SearchService.cs`

### 8. Shell controls and shell window

- `src/Nocturne.App/Converters/InverseBooleanToVisibilityConverter.cs`
- `src/Nocturne.App/Converters/NullToVisibilityConverter.cs`
- `src/Nocturne.App/Converters/TimeSpanToClockStringConverter.cs`
- `src/Nocturne.App/Converters/RepeatModeToGlyphConverter.cs`
- `src/Nocturne.App/Controls/StyledControls.cs`
- `src/Nocturne.App/Controls/NavRailButton.xaml`
- `src/Nocturne.App/Controls/NavRailButton.xaml.cs`
- `src/Nocturne.App/Controls/NavRailProfileButton.xaml`
- `src/Nocturne.App/Controls/NavRailProfileButton.xaml.cs`
- `src/Nocturne.App/Controls/LibrarySidebar.xaml`
- `src/Nocturne.App/Controls/LibrarySidebar.xaml.cs`
- `src/Nocturne.App/Controls/SidebarCollectionItem.xaml`
- `src/Nocturne.App/Controls/SidebarCollectionItem.xaml.cs`
- `src/Nocturne.App/Controls/NowPlayingMiniCard.xaml`
- `src/Nocturne.App/Controls/NowPlayingMiniCard.xaml.cs`
- `src/Nocturne.App/Controls/PlaybackControls.xaml`
- `src/Nocturne.App/Controls/PlaybackControls.xaml.cs`
- `src/Nocturne.App/MainWindow.xaml`
- `src/Nocturne.App/MainWindow.xaml.cs`

### 9. Content controls used by multiple pages

- `src/Nocturne.App/Controls/SearchBar.xaml`
- `src/Nocturne.App/Controls/SearchBar.xaml.cs`
- `src/Nocturne.App/Controls/SectionHeader.xaml`
- `src/Nocturne.App/Controls/SectionHeader.xaml.cs`
- `src/Nocturne.App/Controls/TagChip.xaml`
- `src/Nocturne.App/Controls/TagChip.xaml.cs`
- `src/Nocturne.App/Controls/EmptyStateView.xaml`
- `src/Nocturne.App/Controls/EmptyStateView.xaml.cs`
- `src/Nocturne.App/Controls/BlurredBackgroundPresenter.xaml`
- `src/Nocturne.App/Controls/BlurredBackgroundPresenter.xaml.cs`
- `src/Nocturne.App/Controls/HeroHeader.xaml`
- `src/Nocturne.App/Controls/HeroHeader.xaml.cs`
- `src/Nocturne.App/Controls/TrackRowControl.xaml`
- `src/Nocturne.App/Controls/TrackRowControl.xaml.cs`
- `src/Nocturne.App/Controls/TrackListView.xaml`
- `src/Nocturne.App/Controls/TrackListView.xaml.cs`
- `src/Nocturne.App/Controls/TrackCard.xaml`
- `src/Nocturne.App/Controls/TrackCard.xaml.cs`
- `src/Nocturne.App/Controls/AlbumCard.xaml`
- `src/Nocturne.App/Controls/AlbumCard.xaml.cs`
- `src/Nocturne.App/Controls/ArtistCircleCard.xaml`
- `src/Nocturne.App/Controls/ArtistCircleCard.xaml.cs`
- `src/Nocturne.App/Controls/PlaylistCard.xaml`
- `src/Nocturne.App/Controls/PlaylistCard.xaml.cs`

### 10. Page view models and page views

- `src/Nocturne.App/ViewModels/PlayerBarViewModel.cs`
- `src/Nocturne.App/ViewModels/ShellViewModel.cs`
- `src/Nocturne.App/ViewModels/LibraryPageViewModel.cs`
- `src/Nocturne.App/ViewModels/FavoritesPageViewModel.cs`
- `src/Nocturne.App/ViewModels/OfflineTracksPageViewModel.cs`
- `src/Nocturne.App/ViewModels/SearchPageViewModel.cs`
- `src/Nocturne.App/ViewModels/ArtistPageViewModel.cs`
- `src/Nocturne.App/ViewModels/AlbumPageViewModel.cs`
- `src/Nocturne.App/ViewModels/PlaylistPageViewModel.cs`
- `src/Nocturne.App/ViewModels/SettingsPageViewModel.cs`
- `src/Nocturne.App/Views/Pages/LibraryPage.xaml`
- `src/Nocturne.App/Views/Pages/LibraryPage.xaml.cs`
- `src/Nocturne.App/Views/Pages/FavoritesPage.xaml`
- `src/Nocturne.App/Views/Pages/FavoritesPage.xaml.cs`
- `src/Nocturne.App/Views/Pages/OfflineTracksPage.xaml`
- `src/Nocturne.App/Views/Pages/OfflineTracksPage.xaml.cs`
- `src/Nocturne.App/Views/Pages/SearchPage.xaml`
- `src/Nocturne.App/Views/Pages/SearchPage.xaml.cs`
- `src/Nocturne.App/Views/Pages/ArtistPage.xaml`
- `src/Nocturne.App/Views/Pages/ArtistPage.xaml.cs`
- `src/Nocturne.App/Views/Pages/AlbumPage.xaml`
- `src/Nocturne.App/Views/Pages/AlbumPage.xaml.cs`
- `src/Nocturne.App/Views/Pages/PlaylistPage.xaml`
- `src/Nocturne.App/Views/Pages/PlaylistPage.xaml.cs`
- `src/Nocturne.App/Views/Pages/SettingsPage.xaml`
- `src/Nocturne.App/Views/Pages/SettingsPage.xaml.cs`

### 11. Repository-level docs

- `README.md`
- `ARCHITECTURE.md`

## Extension checklist

Before adding a new feature slice, verify all of these stay aligned:

1. namespace path matches folder path
2. DI registration exists for every new runtime-resolved service or page view model
3. `App.xaml` contains a matching page `DataTemplate` when a new page view model is added
4. new colors or styles are promoted into `Theme` instead of being embedded into a single page
5. new provider behavior maps back to `TrackSource`, `StorageLocation`, and `ResolvedPlaybackStream`
6. async flows accept `CancellationToken` where provider or network work is involved
