namespace Nocturne.App.Controls;

public partial class BlurredBackgroundPresenter : UserControl
{
    public static readonly DependencyProperty ImageSourceProperty =
        DependencyProperty.Register(nameof(ImageSource), typeof(string), typeof(BlurredBackgroundPresenter), new PropertyMetadata(null));

    public static readonly DependencyProperty PresenterHeightProperty =
        DependencyProperty.Register(nameof(PresenterHeight), typeof(double), typeof(BlurredBackgroundPresenter), new PropertyMetadata(260d));

    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(BlurredBackgroundPresenter), new PropertyMetadata(new CornerRadius(28)));

    public BlurredBackgroundPresenter()
    {
        InitializeComponent();
    }

    public string? ImageSource
    {
        get => (string?)GetValue(ImageSourceProperty);
        set => SetValue(ImageSourceProperty, value);
    }

    public double PresenterHeight
    {
        get => (double)GetValue(PresenterHeightProperty);
        set => SetValue(PresenterHeightProperty, value);
    }

    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }
}
