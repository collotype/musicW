using CommunityToolkit.Mvvm.ComponentModel;
using Nocturne.App.Models.Enums;

namespace Nocturne.App.Models;

public sealed partial class Playlist : ObservableObject
{
    [ObservableProperty]
    private bool isPinned;

    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    public TrackSource Source { get; set; } = TrackSource.Local;

    public StorageLocation StorageLocation { get; set; } = StorageLocation.Library;

    public string Title { get; set; } = string.Empty;

    public string OwnerName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? CoverArtUrl { get; set; }

    public string? RemotePageUrl { get; set; }

    public string? ProviderPlaylistId { get; set; }

    public bool IsEditable { get; set; } = true;

    public bool IsOffline { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ObservableCollection<Track> Tracks { get; set; } = [];

    public string Subtitle => $"{OwnerName} • {Tracks.Count} tracks";
}
