using CommunityToolkit.Mvvm.ComponentModel;

namespace Nocturne.App.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    protected void RaiseAll(params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            OnPropertyChanged(propertyName);
        }
    }
}
