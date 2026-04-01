using CommunityToolkit.Mvvm.Input;
using Nocturne.App.Models;
using Nocturne.App.Services;

namespace Nocturne.App.ViewModels;

public sealed class FavoritesPageViewModel : PageViewModelBase
{
    private readonly ILibraryService _libraryService;
    private readonly IPlaybackService _playbackService;

    public FavoritesPageViewModel(ILibraryService libraryService, IPlaybackService playbackService)
    {
        _libraryService = libraryService;
        _playbackService = playbackService;

        PlayTrackCommand = new AsyncRelayCommand<Track?>(PlayTrackAsync);
        Refresh();
        _libraryService.LibraryChanged += (_, _) => Refresh();
    }

    public ObservableCollection<Track> Tracks { get; } = [];

    public IAsyncRelayCommand<Track?> PlayTrackCommand { get; }

    public override Task OnNavigatedToAsync(object? parameter)
    {
        Refresh();
        return Task.CompletedTask;
    }

    private async Task PlayTrackAsync(Track? track)
    {
        if (track is null)
        {
            return;
        }

        await _playbackService.PlayQueueAsync(Tracks.ToList(), track, "favorites");
    }

    private void Refresh()
    {
        Tracks.Clear();
        foreach (var track in _libraryService.GetFavorites())
        {
            Tracks.Add(track);
        }
    }
}
