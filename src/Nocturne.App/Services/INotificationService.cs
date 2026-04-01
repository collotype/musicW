using Nocturne.App.Models;
using Nocturne.App.Models.Enums;

namespace Nocturne.App.Services;

public interface INotificationService
{
    NotificationMessage? Current { get; }

    event EventHandler? NotificationChanged;

    Task ShowAsync(string title, string body, NotificationLevel level = NotificationLevel.Info, TimeSpan? duration = null);
}
