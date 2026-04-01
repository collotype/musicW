using Nocturne.App.Models;

namespace Nocturne.App.Services;

public interface IMockDataService
{
    UserLibrary CreateInitialLibrary();

    Artist CreateFeaturedArtist();

    Album CreateFeaturedAlbum();

    Playlist CreateFeaturedPlaylist();

    SearchResults CreateSearchPreview(string query);
}
