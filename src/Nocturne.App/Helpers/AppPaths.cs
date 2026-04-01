namespace Nocturne.App.Helpers;

public static class AppPaths
{
    public static string Root { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        Branding.CompanyName,
        Branding.ProductName);

    public static string DataFolder { get; } = Path.Combine(Root, "Data");

    public static string CacheFolder { get; } = Path.Combine(Root, "Cache");

    public static string ImageCacheFolder { get; } = Path.Combine(CacheFolder, "Images");

    public static string TempAudioFolder { get; } = Path.Combine(CacheFolder, "TempAudio");

    public static string LibraryFile { get; } = Path.Combine(DataFolder, "library.json");

    public static string SettingsFile { get; } = Path.Combine(DataFolder, "settings.json");

    public static void EnsureCreated()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(DataFolder);
        Directory.CreateDirectory(CacheFolder);
        Directory.CreateDirectory(ImageCacheFolder);
        Directory.CreateDirectory(TempAudioFolder);
    }
}
