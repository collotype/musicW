using CommunityToolkit.Mvvm.ComponentModel;
using Nocturne.App.Models.Enums;

namespace Nocturne.App.Models;

public sealed partial class SidebarCollectionItem : ObservableObject
{
    [ObservableProperty]
    private bool isSelected;

    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    public required string Title { get; init; }

    public string Subtitle { get; init; } = string.Empty;

    public string? CoverArtUrl { get; init; }

    public required CollectionType Type { get; init; }

    public string Glyph { get; init; } = "\uE8D2";
}
