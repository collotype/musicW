using Microsoft.Extensions.DependencyInjection;
using Nocturne.App.ViewModels;

namespace Nocturne.App.Services;

public sealed class NavigationService(IServiceProvider serviceProvider) : INavigationService
{
    public object? CurrentViewModel { get; private set; }

    public event EventHandler? CurrentViewModelChanged;

    public async Task NavigateAsync<TViewModel>(object? parameter = null) where TViewModel : class
    {
        var viewModel = serviceProvider.GetRequiredService<TViewModel>();
        CurrentViewModel = viewModel;
        CurrentViewModelChanged?.Invoke(this, EventArgs.Empty);

        if (viewModel is INavigationAware navigationAware)
        {
            await navigationAware.OnNavigatedToAsync(parameter);
        }
    }
}
