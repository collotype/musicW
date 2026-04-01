using CommunityToolkit.Mvvm.ComponentModel;
using Nocturne.App.Models.Enums;

namespace Nocturne.App.Models;

public sealed partial class NavigationItem : ObservableObject
{
    [ObservableProperty]
    private bool isSelected;

    public required NavigationTarget Target { get; init; }

    public required string Glyph { get; init; }

    public required string Label { get; init; }
}
