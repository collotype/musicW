namespace Nocturne.App.ViewModels;

public abstract class PageViewModelBase : ViewModelBase, INavigationAware
{
    public virtual Task OnNavigatedToAsync(object? parameter)
    {
        return Task.CompletedTask;
    }
}
