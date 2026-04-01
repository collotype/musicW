namespace Nocturne.App.Controls;

public partial class TrackListView : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(TrackListView), new PropertyMetadata(null));

    public static readonly DependencyProperty PlayCommandProperty =
        DependencyProperty.Register(nameof(PlayCommand), typeof(ICommand), typeof(TrackListView));

    public static readonly DependencyProperty ActiveTrackIdProperty =
        DependencyProperty.Register(nameof(ActiveTrackId), typeof(string), typeof(TrackListView), new PropertyMetadata(string.Empty));

    public TrackListView()
    {
        InitializeComponent();
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
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
