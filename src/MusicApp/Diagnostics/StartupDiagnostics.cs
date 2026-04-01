using System.IO;
using System.Text;
using System.Threading;
using System.Windows;

namespace MusicApp.Diagnostics;

internal static class StartupDiagnostics
{
    private const string AppName = "MusicApp";
    private static readonly object SyncRoot = new();
    private static int _errorDialogShown;

    public static string LogDirectoryPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppName,
            "Logs");

    public static string StartupLogPath => Path.Combine(LogDirectoryPath, "startup.log");

    public static string StartupErrorPath => Path.Combine(LogDirectoryPath, "startup-error.txt");

    public static void BeginSession()
    {
        Directory.CreateDirectory(LogDirectoryPath);

        lock (SyncRoot)
        {
            Interlocked.Exchange(ref _errorDialogShown, 0);

            if (File.Exists(StartupErrorPath))
            {
                File.Delete(StartupErrorPath);
            }

            File.AppendAllText(
                StartupLogPath,
                $"{Environment.NewLine}=== Startup session {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} ==={Environment.NewLine}",
                Encoding.UTF8);
        }
    }

    public static void LogInfo(string message) => WriteLine("INFO", message);

    public static void LogWarning(string message) => WriteLine("WARN", message);

    public static void LogException(string context, Exception exception)
    {
        WriteLine("ERROR", $"{context}{Environment.NewLine}{exception}");

        var builder = new StringBuilder();
        builder.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        builder.AppendLine($"Context: {context}");
        builder.AppendLine();
        builder.AppendLine(exception.ToString());

        lock (SyncRoot)
        {
            Directory.CreateDirectory(LogDirectoryPath);
            File.WriteAllText(StartupErrorPath, builder.ToString(), Encoding.UTF8);
        }
    }

    public static void ShowErrorDialog(string title, string context, Exception exception)
    {
        if (Interlocked.Exchange(ref _errorDialogShown, 1) == 1)
        {
            return;
        }

        var message =
            $"{context}{Environment.NewLine}{Environment.NewLine}" +
            $"{exception.GetType().Name}: {exception.Message}{Environment.NewLine}{Environment.NewLine}" +
            $"Log file:{Environment.NewLine}{StartupErrorPath}";

        try
        {
            if (Application.Current?.Dispatcher is { } dispatcher && !dispatcher.CheckAccess())
            {
                dispatcher.Invoke(() => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error));
                return;
            }

            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch
        {
            // Nothing else to do if even the fallback dialog cannot be shown.
        }
    }

    private static void WriteLine(string level, string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {level} {message}{Environment.NewLine}";

        lock (SyncRoot)
        {
            Directory.CreateDirectory(LogDirectoryPath);
            File.AppendAllText(StartupLogPath, line, Encoding.UTF8);
        }
    }
}
