using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using MusicApp.Enums;

namespace MusicApp.Controls;

public class NavRailButton : Button
{
    static NavRailButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NavRailButton), new FrameworkPropertyMetadata(typeof(NavRailButton)));
    }

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(string), typeof(NavRailButton), new PropertyMetadata(""));

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(NavRailButton), new PropertyMetadata(""));

    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(NavRailButton), new PropertyMetadata(false));

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }
}

public class NavRailProfileButton : Button
{
    static NavRailProfileButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NavRailProfileButton), new FrameworkPropertyMetadata(typeof(NavRailProfileButton)));
    }

    public static readonly DependencyProperty AvatarUrlProperty =
        DependencyProperty.Register(nameof(AvatarUrl), typeof(string), typeof(NavRailProfileButton), new PropertyMetadata(""));

    public static readonly DependencyProperty UserNameProperty =
        DependencyProperty.Register(nameof(UserName), typeof(string), typeof(NavRailProfileButton), new PropertyMetadata("User"));

    public string AvatarUrl
    {
        get => (string)GetValue(AvatarUrlProperty);
        set => SetValue(AvatarUrlProperty, value);
    }

    public string UserName
    {
        get => (string)GetValue(UserNameProperty);
        set => SetValue(UserNameProperty, value);
    }
}

public class SidebarCollectionItem : Button
{
    static SidebarCollectionItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(SidebarCollectionItem), new FrameworkPropertyMetadata(typeof(SidebarCollectionItem)));
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(SidebarCollectionItem), new PropertyMetadata(""));

    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(SidebarCollectionItem), new PropertyMetadata(""));

    public static readonly DependencyProperty ThumbnailUrlProperty =
        DependencyProperty.Register(nameof(ThumbnailUrl), typeof(string), typeof(SidebarCollectionItem), new PropertyMetadata(""));

    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(SidebarCollectionItem), new PropertyMetadata(false));

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

    public string ThumbnailUrl
    {
        get => (string)GetValue(ThumbnailUrlProperty);
        set => SetValue(ThumbnailUrlProperty, value);
    }

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }
}

public class TrackCard : Button
{
    static TrackCard()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(TrackCard), new FrameworkPropertyMetadata(typeof(TrackCard)));
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(TrackCard), new PropertyMetadata(""));

    public static readonly DependencyProperty ArtistProperty =
        DependencyProperty.Register(nameof(Artist), typeof(string), typeof(TrackCard), new PropertyMetadata(""));

    public static readonly DependencyProperty CoverArtUrlProperty =
        DependencyProperty.Register(nameof(CoverArtUrl), typeof(string), typeof(TrackCard), new PropertyMetadata(""));

    public static readonly DependencyProperty DurationProperty =
        DependencyProperty.Register(nameof(Duration), typeof(string), typeof(TrackCard), new PropertyMetadata(""));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Artist
    {
        get => (string)GetValue(ArtistProperty);
        set => SetValue(ArtistProperty, value);
    }

    public string CoverArtUrl
    {
        get => (string)GetValue(CoverArtUrlProperty);
        set => SetValue(CoverArtUrlProperty, value);
    }

    public string Duration
    {
        get => (string)GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }
}

public class AlbumCard : Button
{
    static AlbumCard()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(AlbumCard), new FrameworkPropertyMetadata(typeof(AlbumCard)));
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(AlbumCard), new PropertyMetadata(""));

    public static readonly DependencyProperty ArtistProperty =
        DependencyProperty.Register(nameof(Artist), typeof(string), typeof(AlbumCard), new PropertyMetadata(""));

    public static readonly DependencyProperty CoverArtUrlProperty =
        DependencyProperty.Register(nameof(CoverArtUrl), typeof(string), typeof(AlbumCard), new PropertyMetadata(""));

    public static readonly DependencyProperty YearProperty =
        DependencyProperty.Register(nameof(Year), typeof(string), typeof(AlbumCard), new PropertyMetadata(""));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Artist
    {
        get => (string)GetValue(ArtistProperty);
        set => SetValue(ArtistProperty, value);
    }

    public string CoverArtUrl
    {
        get => (string)GetValue(CoverArtUrlProperty);
        set => SetValue(CoverArtUrlProperty, value);
    }

    public string Year
    {
        get => (string)GetValue(YearProperty);
        set => SetValue(YearProperty, value);
    }
}

public class ArtistCircleCard : Button
{
    static ArtistCircleCard()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ArtistCircleCard), new FrameworkPropertyMetadata(typeof(ArtistCircleCard)));
    }

    public static readonly DependencyProperty ArtistNameProperty =
        DependencyProperty.Register(nameof(ArtistName), typeof(string), typeof(ArtistCircleCard), new PropertyMetadata(""));

    public static readonly DependencyProperty ImageUrlProperty =
        DependencyProperty.Register(nameof(ImageUrl), typeof(string), typeof(ArtistCircleCard), new PropertyMetadata(""));

    public string ArtistName
    {
        get => (string)GetValue(ArtistNameProperty);
        set => SetValue(ArtistNameProperty, value);
    }

    public string ImageUrl
    {
        get => (string)GetValue(ImageUrlProperty);
        set => SetValue(ImageUrlProperty, value);
    }
}

public class PlaylistCard : Button
{
    static PlaylistCard()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(PlaylistCard), new FrameworkPropertyMetadata(typeof(PlaylistCard)));
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(PlaylistCard), new PropertyMetadata(""));

    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(PlaylistCard), new PropertyMetadata(""));

    public static readonly DependencyProperty CoverArtUrlProperty =
        DependencyProperty.Register(nameof(CoverArtUrl), typeof(string), typeof(PlaylistCard), new PropertyMetadata(""));

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

    public string CoverArtUrl
    {
        get => (string)GetValue(CoverArtUrlProperty);
        set => SetValue(CoverArtUrlProperty, value);
    }
}

public class TrackRowControl : Button
{
    static TrackRowControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(TrackRowControl), new FrameworkPropertyMetadata(typeof(TrackRowControl)));
    }

    public static readonly DependencyProperty TrackNumberProperty =
        DependencyProperty.Register(nameof(TrackNumber), typeof(int), typeof(TrackRowControl), new PropertyMetadata(0));

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(TrackRowControl), new PropertyMetadata(""));

    public static readonly DependencyProperty ArtistProperty =
        DependencyProperty.Register(nameof(Artist), typeof(string), typeof(TrackRowControl), new PropertyMetadata(""));

    public static readonly DependencyProperty AlbumProperty =
        DependencyProperty.Register(nameof(Album), typeof(string), typeof(TrackRowControl), new PropertyMetadata(""));

    public static readonly DependencyProperty DurationProperty =
        DependencyProperty.Register(nameof(Duration), typeof(string), typeof(TrackRowControl), new PropertyMetadata(""));

    public static readonly DependencyProperty CoverArtUrlProperty =
        DependencyProperty.Register(nameof(CoverArtUrl), typeof(string), typeof(TrackRowControl), new PropertyMetadata(""));

    public static readonly DependencyProperty IsPlayingProperty =
        DependencyProperty.Register(nameof(IsPlaying), typeof(bool), typeof(TrackRowControl), new PropertyMetadata(false));

    public static readonly DependencyProperty IsLikedProperty =
        DependencyProperty.Register(nameof(IsLiked), typeof(bool), typeof(TrackRowControl), new PropertyMetadata(false));

    public int TrackNumber
    {
        get => (int)GetValue(TrackNumberProperty);
        set => SetValue(TrackNumberProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Artist
    {
        get => (string)GetValue(ArtistProperty);
        set => SetValue(ArtistProperty, value);
    }

    public string Album
    {
        get => (string)GetValue(AlbumProperty);
        set => SetValue(AlbumProperty, value);
    }

    public string Duration
    {
        get => (string)GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    public string CoverArtUrl
    {
        get => (string)GetValue(CoverArtUrlProperty);
        set => SetValue(CoverArtUrlProperty, value);
    }

    public bool IsPlaying
    {
        get => (bool)GetValue(IsPlayingProperty);
        set => SetValue(IsPlayingProperty, value);
    }

    public bool IsLiked
    {
        get => (bool)GetValue(IsLikedProperty);
        set => SetValue(IsLikedProperty, value);
    }
}

public class HeroHeader : Control
{
    static HeroHeader()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(HeroHeader), new FrameworkPropertyMetadata(typeof(HeroHeader)));
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(HeroHeader), new PropertyMetadata(""));

    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(HeroHeader), new PropertyMetadata(""));

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(HeroHeader), new PropertyMetadata(""));

    public static readonly DependencyProperty ImageUrlProperty =
        DependencyProperty.Register(nameof(ImageUrl), typeof(string), typeof(HeroHeader), new PropertyMetadata(""));

    public static readonly DependencyProperty BackgroundImageUrlProperty =
        DependencyProperty.Register(nameof(BackgroundImageUrl), typeof(string), typeof(HeroHeader), new PropertyMetadata(""));

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

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string ImageUrl
    {
        get => (string)GetValue(ImageUrlProperty);
        set => SetValue(ImageUrlProperty, value);
    }

    public string BackgroundImageUrl
    {
        get => (string)GetValue(BackgroundImageUrlProperty);
        set => SetValue(BackgroundImageUrlProperty, value);
    }
}

public class TagChip : Button
{
    static TagChip()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(TagChip), new FrameworkPropertyMetadata(typeof(TagChip)));
    }

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(TagChip), new PropertyMetadata(""));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
}

public class FavoriteButton : ToggleButton
{
    static FavoriteButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(FavoriteButton), new FrameworkPropertyMetadata(typeof(FavoriteButton)));
    }

    public static readonly DependencyProperty IsFilledProperty =
        DependencyProperty.Register(nameof(IsFilled), typeof(bool), typeof(FavoriteButton), new PropertyMetadata(false));

    public bool IsFilled
    {
        get => (bool)GetValue(IsFilledProperty);
        set => SetValue(IsFilledProperty, value);
    }
}

public class OverflowButton : Button
{
    static OverflowButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(OverflowButton), new FrameworkPropertyMetadata(typeof(OverflowButton)));
    }
}

public class NowPlayingMiniCard : Control
{
    static NowPlayingMiniCard()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NowPlayingMiniCard), new FrameworkPropertyMetadata(typeof(NowPlayingMiniCard)));
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(NowPlayingMiniCard), new PropertyMetadata(""));

    public static readonly DependencyProperty ArtistProperty =
        DependencyProperty.Register(nameof(Artist), typeof(string), typeof(NowPlayingMiniCard), new PropertyMetadata(""));

    public static readonly DependencyProperty CoverArtUrlProperty =
        DependencyProperty.Register(nameof(CoverArtUrl), typeof(string), typeof(NowPlayingMiniCard), new PropertyMetadata(""));

    public static readonly DependencyProperty IsPlayingProperty =
        DependencyProperty.Register(nameof(IsPlaying), typeof(bool), typeof(NowPlayingMiniCard), new PropertyMetadata(false));

    public static readonly DependencyProperty IsLikedProperty =
        DependencyProperty.Register(nameof(IsLiked), typeof(bool), typeof(NowPlayingMiniCard), new PropertyMetadata(false));

    public static readonly DependencyProperty LikeCommandProperty =
        DependencyProperty.Register(nameof(LikeCommand), typeof(ICommand), typeof(NowPlayingMiniCard), new PropertyMetadata(null));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Artist
    {
        get => (string)GetValue(ArtistProperty);
        set => SetValue(ArtistProperty, value);
    }

    public string CoverArtUrl
    {
        get => (string)GetValue(CoverArtUrlProperty);
        set => SetValue(CoverArtUrlProperty, value);
    }

    public bool IsPlaying
    {
        get => (bool)GetValue(IsPlayingProperty);
        set => SetValue(IsPlayingProperty, value);
    }

    public bool IsLiked
    {
        get => (bool)GetValue(IsLikedProperty);
        set => SetValue(IsLikedProperty, value);
    }

    public ICommand? LikeCommand
    {
        get => (ICommand?)GetValue(LikeCommandProperty);
        set => SetValue(LikeCommandProperty, value);
    }
}

public class PlaybackControls : Control
{
    static PlaybackControls()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(PlaybackControls), new FrameworkPropertyMetadata(typeof(PlaybackControls)));
    }

    public static readonly DependencyProperty IsPlayingProperty =
        DependencyProperty.Register(nameof(IsPlaying), typeof(bool), typeof(PlaybackControls), new PropertyMetadata(false));

    public static readonly DependencyProperty IsShuffleProperty =
        DependencyProperty.Register(nameof(IsShuffle), typeof(bool), typeof(PlaybackControls), new PropertyMetadata(false));

    public static readonly DependencyProperty RepeatModeProperty =
        DependencyProperty.Register(nameof(RepeatMode), typeof(RepeatMode), typeof(PlaybackControls), new PropertyMetadata(RepeatMode.None));

    public static readonly DependencyProperty ToggleShuffleCommandProperty =
        DependencyProperty.Register(nameof(ToggleShuffleCommand), typeof(ICommand), typeof(PlaybackControls), new PropertyMetadata(null));

    public static readonly DependencyProperty PreviousCommandProperty =
        DependencyProperty.Register(nameof(PreviousCommand), typeof(ICommand), typeof(PlaybackControls), new PropertyMetadata(null));

    public static readonly DependencyProperty PlayPauseCommandProperty =
        DependencyProperty.Register(nameof(PlayPauseCommand), typeof(ICommand), typeof(PlaybackControls), new PropertyMetadata(null));

    public static readonly DependencyProperty NextCommandProperty =
        DependencyProperty.Register(nameof(NextCommand), typeof(ICommand), typeof(PlaybackControls), new PropertyMetadata(null));

    public static readonly DependencyProperty CycleRepeatModeCommandProperty =
        DependencyProperty.Register(nameof(CycleRepeatModeCommand), typeof(ICommand), typeof(PlaybackControls), new PropertyMetadata(null));

    public bool IsPlaying
    {
        get => (bool)GetValue(IsPlayingProperty);
        set => SetValue(IsPlayingProperty, value);
    }

    public bool IsShuffle
    {
        get => (bool)GetValue(IsShuffleProperty);
        set => SetValue(IsShuffleProperty, value);
    }

    public RepeatMode RepeatMode
    {
        get => (RepeatMode)GetValue(RepeatModeProperty);
        set => SetValue(RepeatModeProperty, value);
    }

    public ICommand? ToggleShuffleCommand
    {
        get => (ICommand?)GetValue(ToggleShuffleCommandProperty);
        set => SetValue(ToggleShuffleCommandProperty, value);
    }

    public ICommand? PreviousCommand
    {
        get => (ICommand?)GetValue(PreviousCommandProperty);
        set => SetValue(PreviousCommandProperty, value);
    }

    public ICommand? PlayPauseCommand
    {
        get => (ICommand?)GetValue(PlayPauseCommandProperty);
        set => SetValue(PlayPauseCommandProperty, value);
    }

    public ICommand? NextCommand
    {
        get => (ICommand?)GetValue(NextCommandProperty);
        set => SetValue(NextCommandProperty, value);
    }

    public ICommand? CycleRepeatModeCommand
    {
        get => (ICommand?)GetValue(CycleRepeatModeCommandProperty);
        set => SetValue(CycleRepeatModeCommandProperty, value);
    }
}

public class ProgressSlider : Slider
{
    static ProgressSlider()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ProgressSlider), new FrameworkPropertyMetadata(typeof(ProgressSlider)));
    }
}

public class VolumeSlider : Slider
{
    static VolumeSlider()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(VolumeSlider), new FrameworkPropertyMetadata(typeof(VolumeSlider)));
    }
}
