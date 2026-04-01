namespace Nocturne.App.Controls;

public partial class TrackCard : UserControl
{
    public static readonly DependencyProperty TrackProperty =
        DependencyProperty.Register(nameof(Track), typeof(Models.Track), typeof(TrackCard), new PropertyMetadata(null));

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(TrackCard));

    public TrackCard()
    {
        InitializeComponent();
    }

    public Models.Track? Track
    {
        get => (Models.Track?)GetValue(TrackProperty);
        set => SetValue(TrackProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }
}
