namespace Nocturne.App.Controls;

public partial class PlaylistCard : UserControl
{
    public static readonly DependencyProperty PlaylistProperty =
        DependencyProperty.Register(nameof(Playlist), typeof(Models.Playlist), typeof(PlaylistCard), new PropertyMetadata(null));

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(PlaylistCard));

    public PlaylistCard()
    {
        InitializeComponent();
    }

    public Models.Playlist? Playlist
    {
        get => (Models.Playlist?)GetValue(PlaylistProperty);
        set => SetValue(PlaylistProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }
}
