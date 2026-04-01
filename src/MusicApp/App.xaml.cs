using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MusicApp.Audio;
using MusicApp.Data;
using MusicApp.Persistence;
using MusicApp.Providers;
using MusicApp.Services;
using MusicApp.ViewModels;
using MusicApp.Views;
using Serilog;

namespace MusicApp;

public partial class App : Application
{
    public static IHost Host { get; private set; } = default!;
    public static IServiceProvider Services => Host.Services;

    public App()
    {
        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureServices(ConfigureServices)
            .Build();

        Log.Information("Application starting...");
    }

    private void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        // Logging
        Log.Logger = new LoggerConfiguration()
            .WriteTo.File("logs/musicapp-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        // Persistence
        services.AddSingleton<AppDataStore>();

        // Services
        services.AddSingleton<SettingsService>();
        services.AddSingleton<ImageCacheService>();
        services.AddSingleton<LocalMusicScannerService>();
        services.AddSingleton<NavigationService>();
        services.AddSingleton<QueueService>();
        services.AddSingleton<PlaybackService>();
        services.AddSingleton<LibraryService>();
        services.AddSingleton<SearchService>();
        services.AddSingleton<DownloadService>();

        // Providers
        services.AddSingleton<IMusicProvider, LocalLibraryProvider>();
        services.AddSingleton<IMusicProvider, SoundCloudProvider>();
        services.AddSingleton<IMusicProvider, SpotifyProvider>();
        services.AddSingleton<MusicProviderService>();

        // Audio
        services.AddSingleton<AudioPlayer>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<ArtistViewModel>();
        services.AddSingleton<AlbumViewModel>();
        services.AddSingleton<PlaylistViewModel>();
        services.AddSingleton<LibraryViewModel>();
        services.AddSingleton<SearchViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<NowPlayingViewModel>();

        // Views
        services.AddTransient<MainWindow>();
        services.AddTransient<ArtistPage>();
        services.AddTransient<AlbumPage>();
        services.AddTransient<PlaylistPage>();
        services.AddTransient<LibraryPage>();
        services.AddTransient<SearchPage>();
        services.AddTransient<SettingsPage>();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await Host.StartAsync();

        // Load persistence
        var appDataStore = Services.GetRequiredService<AppDataStore>();
        await appDataStore.LoadAllAsync();

        var settingsService = Services.GetRequiredService<SettingsService>();
        await settingsService.LoadSettingsAsync();

        // Initialize library
        var libraryService = Services.GetRequiredService<LibraryService>();
        await libraryService.InitializeAsync();

        // Seed mock data if library is empty
        if (libraryService.AllTracks.Count == 0)
        {
            await MockDataSeeder.SeedAsync(libraryService);
        }

        var localScanner = Services.GetRequiredService<LocalMusicScannerService>();
        await localScanner.InitializeAsync();

        // Initialize main view model and navigate to library
        var mainViewModel = Services.GetRequiredService<MainViewModel>();
        var navigationService = Services.GetRequiredService<NavigationService>();
        navigationService.NavigateToLibrary();

        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        var appDataStore = Services.GetRequiredService<AppDataStore>();
        await appDataStore.SaveAllAsync();

        await Host.StopAsync();
        Log.Information("Application exited.");
        Log.CloseAndFlush();

        base.OnExit(e);
    }
}
