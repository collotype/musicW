namespace Nocturne.App.Services;

public interface INavigationService
{
    object? CurrentViewModel { get; }

    event EventHandler? CurrentViewModelChanged;

    Task NavigateAsync<TViewModel>(object? parameter = null) where TViewModel : class;
}
