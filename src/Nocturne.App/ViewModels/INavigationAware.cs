namespace Nocturne.App.ViewModels;

public interface INavigationAware
{
    Task OnNavigatedToAsync(object? parameter);
}
