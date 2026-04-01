namespace Nocturne.App.Controls;

public partial class TrackRowControl : UserControl
{
    public static readonly DependencyProperty TrackProperty =
        DependencyProperty.Register(nameof(Track), typeof(Models.Track), typeof(TrackRowControl), new PropertyMetadata(null));

    public static readonly DependencyProperty PlayCommandProperty =
        DependencyProperty.Register(nameof(PlayCommand), typeof(ICommand), typeof(TrackRowControl));

    public static readonly DependencyProperty ActiveTrackIdProperty =
        DependencyProperty.Register(nameof(ActiveTrackId), typeof(string), typeof(TrackRowControl), new PropertyMetadata(string.Empty));

    public TrackRowControl()
    {
        InitializeComponent();
    }

    public Models.Track? Track
    {
        get => (Models.Track?)GetValue(TrackProperty);
        set => SetValue(TrackProperty, value);
    }

    public ICommand? PlayCommand
    {
        get => (ICommand?)GetValue(PlayCommandProperty);
        set => SetValue(PlayCommandProperty, value);
    }

    public string ActiveTrackId
    {
        get => (string)GetValue(ActiveTrackIdProperty);
        set => SetValue(ActiveTrackIdProperty, value);
    }
}
