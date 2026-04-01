namespace Nocturne.App.Controls;

public partial class NavRailProfileButton : UserControl
{
    public static readonly DependencyProperty InitialsProperty =
        DependencyProperty.Register(nameof(Initials), typeof(string), typeof(NavRailProfileButton), new PropertyMetadata("N"));

    public NavRailProfileButton()
    {
        InitializeComponent();
    }

    public string Initials
    {
        get => (string)GetValue(InitialsProperty);
        set => SetValue(InitialsProperty, value);
    }
}
