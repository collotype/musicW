namespace Nocturne.App.Controls;

public partial class HeroHeader : UserControl
{
    public static readonly DependencyProperty ImageSourceProperty =
        DependencyProperty.Register(nameof(ImageSource), typeof(string), typeof(HeroHeader), new PropertyMetadata(null));

    public static readonly DependencyProperty CoverArtUrlProperty =
        DependencyProperty.Register(nameof(CoverArtUrl), typeof(string), typeof(HeroHeader), new PropertyMetadata(null));

    public static readonly DependencyProperty KickerProperty =
        DependencyProperty.Register(nameof(Kicker), typeof(string), typeof(HeroHeader), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(HeroHeader), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(HeroHeader), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty MetaProperty =
        DependencyProperty.Register(nameof(Meta), typeof(string), typeof(HeroHeader), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty TagsProperty =
        DependencyProperty.Register(nameof(Tags), typeof(IEnumerable<string>), typeof(HeroHeader), new PropertyMetadata(null));

    public static readonly DependencyProperty ActionsContentProperty =
        DependencyProperty.Register(nameof(ActionsContent), typeof(object), typeof(HeroHeader), new PropertyMetadata(null));

    public static readonly DependencyProperty IsCircularCoverProperty =
        DependencyProperty.Register(nameof(IsCircularCover), typeof(bool), typeof(HeroHeader), new PropertyMetadata(false));

    public HeroHeader()
    {
        InitializeComponent();
    }

    public string? ImageSource
    {
        get => (string?)GetValue(ImageSourceProperty);
        set => SetValue(ImageSourceProperty, value);
    }

    public string? CoverArtUrl
    {
        get => (string?)GetValue(CoverArtUrlProperty);
        set => SetValue(CoverArtUrlProperty, value);
    }

    public string Kicker
    {
        get => (string)GetValue(KickerProperty);
        set => SetValue(KickerProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public string Meta
    {
        get => (string)GetValue(MetaProperty);
        set => SetValue(MetaProperty, value);
    }

    public IEnumerable<string>? Tags
    {
        get => (IEnumerable<string>?)GetValue(TagsProperty);
        set => SetValue(TagsProperty, value);
    }

    public object? ActionsContent
    {
        get => GetValue(ActionsContentProperty);
        set => SetValue(ActionsContentProperty, value);
    }

    public bool IsCircularCover
    {
        get => (bool)GetValue(IsCircularCoverProperty);
        set => SetValue(IsCircularCoverProperty, value);
    }
}
