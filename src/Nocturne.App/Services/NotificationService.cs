using Nocturne.App.Models;
using Nocturne.App.Models.Enums;

namespace Nocturne.App.Services;

public sealed class NotificationService : INotificationService
{
    private CancellationTokenSource? _dismissCancellationTokenSource;

    public NotificationMessage? Current { get; private set; }

    public event EventHandler? NotificationChanged;

    public async Task ShowAsync(string title, string body, NotificationLevel level = NotificationLevel.Info, TimeSpan? duration = null)
    {
        _dismissCancellationTokenSource?.Cancel();
        _dismissCancellationTokenSource = new CancellationTokenSource();

        Current = new NotificationMessage
        {
            Title = title,
            Body = body,
            Level = level
        };

        NotificationChanged?.Invoke(this, EventArgs.Empty);

        try
        {
            await Task.Delay(duration ?? TimeSpan.FromSeconds(3.6), _dismissCancellationTokenSource.Token);
            Current = null;
            NotificationChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (TaskCanceledException)
        {
        }
    }
}
