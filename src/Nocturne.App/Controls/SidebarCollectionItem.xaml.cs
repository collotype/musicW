namespace Nocturne.App.Controls;

public partial class SidebarCollectionItem : UserControl
{
    public static readonly DependencyProperty ItemProperty =
        DependencyProperty.Register(nameof(Item), typeof(Models.SidebarCollectionItem), typeof(SidebarCollectionItem), new PropertyMetadata(null));

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(SidebarCollectionItem));

    public SidebarCollectionItem()
    {
        InitializeComponent();
    }

    public Models.SidebarCollectionItem? Item
    {
        get => (Models.SidebarCollectionItem?)GetValue(ItemProperty);
        set => SetValue(ItemProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }
}
