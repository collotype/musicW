using MusicApp.Enums;
using MusicApp.Models;

namespace MusicApp.Services;

public class NavigationService : INavigationService
{
    public event EventHandler<NavigationEventArgs>? Navigated;

    private readonly Stack<NavigationRequest> _history = new();

    public NavigationRequest CurrentRequest { get; private set; } = new()
    {
        Page = NavigationPage.Home
    };

    public void NavigateToHome()
    {
        Navigate(new NavigationRequest
        {
            Page = NavigationPage.Home
        });
    }

    public void NavigateToMyWave(WaveSeed? seed = null)
    {
        Navigate(new NavigationRequest
        {
            Page = NavigationPage.MyWave,
            WaveSeed = seed ?? WaveSeed.Home()
        });
    }

    public void NavigateToArtist(string artistId, string providerName = "Local")
    {
        Navigate(new NavigationRequest
        {
            Page = NavigationPage.Artist,
            ItemId = artistId,
            ProviderName = providerName
        });
    }

    public void NavigateToAlbum(string albumId, string providerName = "Local")
    {
        Navigate(new NavigationRequest
        {
            Page = NavigationPage.Album,
            ItemId = albumId,
            ProviderName = providerName
        });
    }

    public void NavigateToPlaylist(string playlistId, string providerName = "Local")
    {
        Navigate(new NavigationRequest
        {
            Page = NavigationPage.Playlist,
            ItemId = playlistId,
            ProviderName = providerName
        });
    }

    public void NavigateToLibrary(LibrarySection section = LibrarySection.Overview)
    {
        Navigate(new NavigationRequest
        {
            Page = NavigationPage.Library,
            LibrarySection = section
        });
    }

    public void NavigateToSearch(string? query = null)
    {
        Navigate(new NavigationRequest
        {
            Page = NavigationPage.Search,
            Query = query
        });
    }

    public void NavigateToQueue()
    {
        Navigate(new NavigationRequest
        {
            Page = NavigationPage.Queue
        });
    }

    public void NavigateToSettings()
    {
        Navigate(new NavigationRequest
        {
            Page = NavigationPage.Settings
        });
    }

    public void GoBack()
    {
        if (_history.Count == 0)
        {
            return;
        }

        CurrentRequest = _history.Pop();
        Navigated?.Invoke(this, new NavigationEventArgs { Request = CurrentRequest });
    }

    private void Navigate(NavigationRequest request)
    {
        if (CurrentRequest != null)
        {
            _history.Push(CloneRequest(CurrentRequest));
        }

        CurrentRequest = CloneRequest(request);
        Navigated?.Invoke(this, new NavigationEventArgs { Request = CurrentRequest });
    }

    private static NavigationRequest CloneRequest(NavigationRequest request)
    {
        return new NavigationRequest
        {
            Page = request.Page,
            ItemId = request.ItemId,
            ProviderName = request.ProviderName,
            LibrarySection = request.LibrarySection,
            Query = request.Query,
            WaveSeed = request.WaveSeed == null
                ? null
                : new WaveSeed
                {
                    Type = request.WaveSeed.Type,
                    Id = request.WaveSeed.Id,
                    Title = request.WaveSeed.Title,
                    Subtitle = request.WaveSeed.Subtitle
                }
        };
    }
}
