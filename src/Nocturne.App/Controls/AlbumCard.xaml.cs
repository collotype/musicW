namespace Nocturne.App.Controls;

public partial class AlbumCard : UserControl
{
    public static readonly DependencyProperty AlbumProperty =
        DependencyProperty.Register(nameof(Album), typeof(Models.Album), typeof(AlbumCard), new PropertyMetadata(null));

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(AlbumCard));

    public AlbumCard()
    {
        InitializeComponent();
    }

    public Models.Album? Album
    {
        get => (Models.Album?)GetValue(AlbumProperty);
        set => SetValue(AlbumProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }
}
