using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MusicApp.Audio;
using MusicApp.Diagnostics;
using MusicApp.Persistence;
using MusicApp.Providers;
using MusicApp.Services;
using MusicApp.ViewModels;
using MusicApp.Views;

namespace MusicApp;

public partial class App : Application
{
    private IHost? _host;
    private MainViewModel? _mainViewModel;
    private bool _isShuttingDown;
    private bool _startupCompleted;

    public static IHost Host => ((App)Current)._host ?? throw new InvalidOperationException("The application host is not available.");

    public static IServiceProvider Services => Host.Services;

    public App()
    {
        try
        {
            ShutdownMode = ShutdownMode.OnMainWindowClose;

            StartupDiagnostics.BeginSession();
            StartupDiagnostics.LogInfo("Application instance created.");

            RegisterGlobalExceptionHandlers();

            StartupDiagnostics.LogInfo("Creating host.");
            _host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                .UseContentRoot(AppContext.BaseDirectory)
                .ConfigureServices(ConfigureServices)
                .Build();
            StartupDiagnostics.LogInfo("Host created.");
        }
        catch (Exception ex)
        {
            StartupDiagnostics.LogException("Application construction failed.", ex);
            StartupDiagnostics.ShowErrorDialog("MusicApp startup error", "Application construction failed.", ex);
            throw;
        }
    }

    private void ConfigureServices(HostBuilderContext _, IServiceCollection services)
    {
        StartupDiagnostics.LogInfo("Configuring services.");

        services.AddSingleton<AppDataStore>();

        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IImageCacheService, ImageCacheService>();
        services.AddSingleton<ILocalMusicScannerService, LocalMusicScannerService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IQueueService, QueueService>();
        services.AddSingleton<ILibraryService, LibraryService>();
        services.AddSingleton<ILyricsService, LyricsService>();
        services.AddSingleton<ITimedCommentService, TimedCommentService>();
        services.AddSingleton<IRecommendationService, RecommendationService>();
        services.AddSingleton<IMusicProviderService, MusicProviderService>();
        services.AddSingleton<IPlaybackService, PlaybackService>();
        services.AddSingleton<ISearchService, SearchService>();
        services.AddSingleton<IDownloadService, DownloadService>();

        services.AddSingleton<IMusicProvider, LocalLibraryProvider>();
        services.AddSingleton<IMusicProvider, SoundCloudProvider>();

        services.AddSingleton<AudioPlayer>();

        services.AddSingleton<MainViewModel>();
        services.AddSingleton<HomeViewModel>();
        services.AddSingleton<MyWaveViewModel>();
        services.AddSingleton<QueueViewModel>();
        services.AddSingleton<ArtistViewModel>();
        services.AddSingleton<AlbumViewModel>();
        services.AddSingleton<PlaylistViewModel>();
        services.AddSingleton<LibraryViewModel>();
        services.AddSingleton<SearchViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<NowPlayingViewModel>();

        services.AddTransient<MainWindow>();
        services.AddTransient<HomePage>();
        services.AddTransient<MyWavePage>();
        services.AddTransient<QueuePage>();
        services.AddTransient<ArtistPage>();
        services.AddTransient<AlbumPage>();
        services.AddTransient<PlaylistPage>();
        services.AddTransient<LibraryPage>();
        services.AddTransient<SearchPage>();
        services.AddTransient<SettingsPage>();

        StartupDiagnostics.LogInfo("Service configuration complete.");
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            StartupDiagnostics.LogInfo("Startup entered.");

            await UpdateStartupStatusAsync("Resolving shell view model...");
            _mainViewModel = Services.GetRequiredService<MainViewModel>();
            StartupDiagnostics.LogInfo("Shell view model initialized.");

            await UpdateStartupStatusAsync("Creating main window...");
            var mainWindow = Services.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;
            StartupDiagnostics.LogInfo("Main window created.");

            await UpdateStartupStatusAsync("Opening main window...");
            mainWindow.Show();
            StartupDiagnostics.LogInfo("Main window shown.");

            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            await StartApplicationAsync();

            _startupCompleted = true;
            _mainViewModel.CompleteStartup();
            StartupDiagnostics.LogInfo("Startup completed successfully.");
        }
        catch (Exception ex)
        {
            HandleStartupFailure("Application startup failed.", ex);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            StartupDiagnostics.LogInfo("Application exit started.");

            if (_host != null)
            {
                try
                {
                    StartupDiagnostics.LogInfo("Saving persistence.");
                    var appDataStore = _host.Services.GetRequiredService<AppDataStore>();
                    await appDataStore.SaveAllAsync();
                    StartupDiagnostics.LogInfo("Persistence saved.");
                }
                catch (Exception ex)
                {
                    StartupDiagnostics.LogException("Saving application data during exit failed.", ex);
                }

                try
                {
                    StartupDiagnostics.LogInfo("Stopping host.");
                    await _host.StopAsync();
                    StartupDiagnostics.LogInfo("Host stopped.");
                }
                catch (Exception ex)
                {
                    StartupDiagnostics.LogException("Stopping host during exit failed.", ex);
                }
                finally
                {
                    _host.Dispose();
                    _host = null;
                }
            }
        }
        finally
        {
            base.OnExit(e);
        }
    }

    private async Task StartApplicationAsync()
    {
        await UpdateStartupStatusAsync("Starting host...");
        await Host.StartAsync();
        StartupDiagnostics.LogInfo("Host started.");

        await UpdateStartupStatusAsync("Loading saved data...");
        var appDataStore = Services.GetRequiredService<AppDataStore>();
        await appDataStore.LoadAllAsync();
        StartupDiagnostics.LogInfo("Persistence loaded.");

        await UpdateStartupStatusAsync("Loading settings...");
        var settingsService = Services.GetRequiredService<ISettingsService>();
        await settingsService.LoadSettingsAsync();
        _mainViewModel?.ApplyShellSettings();
        StartupDiagnostics.LogInfo("Settings initialized.");

        var playbackService = Services.GetRequiredService<IPlaybackService>();
        await playbackService.SetVolumeAsync(settingsService.Settings.Volume);
        if (settingsService.Settings.IsMuted)
        {
            await playbackService.ToggleMuteAsync();
        }

        await UpdateStartupStatusAsync("Initializing library...");
        var libraryService = Services.GetRequiredService<ILibraryService>();
        await libraryService.InitializeAsync();
        StartupDiagnostics.LogInfo("Library initialized.");

        await UpdateStartupStatusAsync("Scanning local music library...");
        var localScanner = Services.GetRequiredService<ILocalMusicScannerService>();
        await localScanner.InitializeAsync();
        StartupDiagnostics.LogInfo("Local music scanner initialized.");

        await UpdateStartupStatusAsync("Preparing workspace...");
        var navigationService = Services.GetRequiredService<INavigationService>();
        navigationService.NavigateToHome();
        StartupDiagnostics.LogInfo("First navigation completed.");
    }

    private async Task UpdateStartupStatusAsync(string message)
    {
        StartupDiagnostics.LogInfo(message);

        if (_mainViewModel != null)
        {
            _mainViewModel.SetStartupStatus(message);
        }

        await Dispatcher.Yield(DispatcherPriority.Background);
    }

    private void RegisterGlobalExceptionHandlers()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnTaskSchedulerUnobservedTaskException;

        StartupDiagnostics.LogInfo("Global exception handlers registered.");
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        HandleUnhandledException("An unhandled UI exception occurred.", e.Exception, shutdown: true);
        e.Handled = true;
    }

    private void OnCurrentDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception
            ?? new Exception($"Non-exception object thrown: {e.ExceptionObject}");

        HandleUnhandledException("A fatal application exception occurred.", exception, shutdown: e.IsTerminating);
    }

    private void OnTaskSchedulerUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        HandleUnhandledException("An unobserved task exception occurred.", e.Exception, shutdown: false);
        e.SetObserved();
    }

    private void HandleStartupFailure(string context, Exception exception)
    {
        StartupDiagnostics.LogException(context, exception);
        ReportStartupFailureToShell(exception);
        StartupDiagnostics.ShowErrorDialog("MusicApp startup error", context, exception);
        RequestShutdown();
    }

    private void HandleUnhandledException(string context, Exception exception, bool shutdown)
    {
        StartupDiagnostics.LogException(context, exception);
        ReportStartupFailureToShell(exception);
        StartupDiagnostics.ShowErrorDialog("MusicApp error", context, exception);

        if (shutdown)
        {
            RequestShutdown();
        }
    }

    private void ReportStartupFailureToShell(Exception exception)
    {
        if (_mainViewModel == null || _startupCompleted)
        {
            return;
        }

        void UpdateShell() => _mainViewModel.ReportStartupFailure($"{exception.GetType().Name}: {exception.Message}");

        if (Dispatcher.CheckAccess())
        {
            UpdateShell();
            return;
        }

        Dispatcher.Invoke(UpdateShell);
    }

    private void RequestShutdown()
    {
        if (_isShuttingDown)
        {
            return;
        }

        _isShuttingDown = true;

        if (Dispatcher.CheckAccess())
        {
            Shutdown(-1);
            return;
        }

        Dispatcher.BeginInvoke(() => Shutdown(-1));
    }
}
