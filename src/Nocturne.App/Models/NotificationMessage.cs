using Nocturne.App.Models.Enums;

namespace Nocturne.App.Models;

public sealed class NotificationMessage
{
    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public NotificationLevel Level { get; set; } = NotificationLevel.Info;
}
