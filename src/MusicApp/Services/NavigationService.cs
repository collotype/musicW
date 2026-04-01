namespace MusicApp.Services;

public class NavigationService : INavigationService
{
    public event EventHandler<NavigationEventArgs>? Navigated;

    private readonly Stack<NavigationState> _history = new();
    private NavigationState? _currentState;

    private class NavigationState
    {
        public string PageType { get; set; } = string.Empty;
        public string? ItemId { get; set; }
        public object? Parameter { get; set; }
    }

    public void NavigateToArtist(string artistId)
    {
        PushState("Artist", artistId);
        Navigated?.Invoke(this, new NavigationEventArgs { PageType = "Artist", ItemId = artistId });
    }

    public void NavigateToAlbum(string albumId)
    {
        PushState("Album", albumId);
        Navigated?.Invoke(this, new NavigationEventArgs { PageType = "Album", ItemId = albumId });
    }

    public void NavigateToPlaylist(string playlistId)
    {
        PushState("Playlist", playlistId);
        Navigated?.Invoke(this, new NavigationEventArgs { PageType = "Playlist", ItemId = playlistId });
    }

    public void NavigateToLibrary()
    {
        PushState("Library", null);
        Navigated?.Invoke(this, new NavigationEventArgs { PageType = "Library" });
    }

    public void NavigateToSearch()
    {
        PushState("Search", null);
        Navigated?.Invoke(this, new NavigationEventArgs { PageType = "Search" });
    }

    public void NavigateToSettings()
    {
        PushState("Settings", null);
        Navigated?.Invoke(this, new NavigationEventArgs { PageType = "Settings" });
    }

    public void GoBack()
    {
        if (_history.Count > 0)
        {
            _currentState = _history.Pop();
            Navigated?.Invoke(this, new NavigationEventArgs
            {
                PageType = _currentState.PageType,
                ItemId = _currentState.ItemId,
                Parameter = _currentState.Parameter
            });
        }
    }

    private void PushState(string pageType, string? itemId)
    {
        if (_currentState != null)
        {
            _history.Push(_currentState);
        }
        _currentState = new NavigationState { PageType = pageType, ItemId = itemId };
    }
}
