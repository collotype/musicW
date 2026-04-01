using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nocturne.App.Helpers;
using Nocturne.App.Providers;
using Nocturne.App.Services;
using Nocturne.App.ViewModels;
using System.Windows;

namespace Nocturne.App;

public partial class App : Application
{
    private IHost? _host;

    public static IServiceProvider Services =>
        ((App)Current)._host?.Services ?? throw new InvalidOperationException("The host has not been initialized.");

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        AppPaths.EnsureCreated();

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(ConfigureServices)
            .Build();

        await _host.StartAsync();

        var settingsService = Services.GetRequiredService<ISettingsService>();
        await settingsService.InitializeAsync();

        var libraryService = Services.GetRequiredService<ILibraryService>();
        await libraryService.InitializeAsync();

        var shellViewModel = Services.GetRequiredService<ShellViewModel>();
        await shellViewModel.InitializeAsync();

        var mainWindow = Services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(3));
            _host.Dispose();
        }

        base.OnExit(e);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddHttpClient("soundcloud", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(12);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(HttpConstants.BrowserUserAgent);
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json,text/plain,*/*");
        });
        services.AddHttpClient("spotify", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(12);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(HttpConstants.BrowserUserAgent);
        });
        services.AddHttpClient("images", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(20);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(HttpConstants.BrowserUserAgent);
        });

        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IMockDataService, MockDataService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IImageCacheService, ImageCacheService>();
        services.AddSingleton<ILocalMusicScannerService, LocalMusicScannerService>();
        services.AddSingleton<ILibraryService, LibraryService>();
        services.AddSingleton<IQueueService, QueueService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IPlaybackService, PlaybackService>();
        services.AddSingleton<IDownloadService, DownloadService>();

        services.AddSingleton<LocalLibraryProvider>();
        services.AddSingleton<SoundCloudProvider>();
        services.AddSingleton<SpotifyProvider>();
        services.AddSingleton<IMusicProvider>(sp => sp.GetRequiredService<LocalLibraryProvider>());
        services.AddSingleton<IMusicProvider>(sp => sp.GetRequiredService<SoundCloudProvider>());
        services.AddSingleton<IMusicProvider>(sp => sp.GetRequiredService<SpotifyProvider>());
        services.AddSingleton<IOnlineMusicService, OnlineMusicService>();
        services.AddSingleton<ISearchService, SearchService>();

        services.AddSingleton<PlayerBarViewModel>();
        services.AddSingleton<ShellViewModel>();
        services.AddTransient<LibraryPageViewModel>();
        services.AddTransient<FavoritesPageViewModel>();
        services.AddTransient<OfflineTracksPageViewModel>();
        services.AddTransient<SearchPageViewModel>();
        services.AddTransient<ArtistPageViewModel>();
        services.AddTransient<AlbumPageViewModel>();
        services.AddTransient<PlaylistPageViewModel>();
        services.AddTransient<SettingsPageViewModel>();

        services.AddSingleton<MainWindow>();
    }
}
