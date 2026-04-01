namespace Nocturne.App.Controls;

public partial class ArtistCircleCard : UserControl
{
    public static readonly DependencyProperty ArtistProperty =
        DependencyProperty.Register(nameof(Artist), typeof(Models.Artist), typeof(ArtistCircleCard), new PropertyMetadata(null));

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(ArtistCircleCard));

    public ArtistCircleCard()
    {
        InitializeComponent();
    }

    public Models.Artist? Artist
    {
        get => (Models.Artist?)GetValue(ArtistProperty);
        set => SetValue(ArtistProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }
}
