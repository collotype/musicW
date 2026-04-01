# Nocturne Desktop

Premium dark Windows desktop music client built with WPF, MVVM, NAudio, and provider-driven online music services.

## Architecture

The canonical solution map, source tree, naming rules, DI registration rules, theme token inventory, and incremental file-generation order are documented in `ARCHITECTURE.md`.

## Structure

- `Nocturne.sln`
- `ARCHITECTURE.md`
- `src/Nocturne.App`
  - `App.xaml`, `MainWindow.xaml`
  - `Assets/Fonts`
  - `Controls`
  - `Converters`
  - `Helpers`
  - `Models`
  - `Persistence`
  - `Providers`
  - `Services`
  - `Theme`
  - `ViewModels`
  - `Views/Pages`

## Packages

- `CommunityToolkit.Mvvm`
- `Microsoft.Extensions.Hosting`
- `Microsoft.Extensions.Http`
- `NAudio`
- `TagLibSharp`

## Run

```powershell
& 'C:\Program Files\dotnet\dotnet.exe' build .\src\Nocturne.App\Nocturne.App.csproj
& '.\src\Nocturne.App\bin\Debug\net8.0-windows\Nocturne.App.exe'
```

## Provider Notes

- `SoundCloudProvider`: public search, artist/release lookup, stream resolution, and file download path.
- `SpotifyProvider`: metadata-only search and entity lookup via client credentials.
- `LocalLibraryProvider`: local library indexing and playback resolution from disk.
