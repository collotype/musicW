using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicApp.Enums;
using MusicApp.Models;
using MusicApp.Services;

namespace MusicApp.ViewModels;

public partial class MyWaveViewModel : ObservableObject
{
    private readonly IRecommendationService _recommendationService;
    private readonly IPlaybackService _playbackService;
    private readonly ILibraryService _libraryService;

    private readonly HashSet<string> _suppressedArtistIds = new(StringComparer.OrdinalIgnoreCase);

    [ObservableProperty]
    private WaveSeed _seed = WaveSeed.Home();

    [ObservableProperty]
    private MyWavePresentation _selectedPresentation = MyWavePresentation.Flow;

    [ObservableProperty]
    private string _selectedActivity = "Any";

    [ObservableProperty]
    private string _selectedCharacter = "Balanced";

    [ObservableProperty]
    private string _selectedMood = "Fluid";

    [ObservableProperty]
    private string _selectedLanguage = "Any";

    [ObservableProperty]
    private double _familiarity = 0.55;

    [ObservableProperty]
    private double _popularity = 0.45;

    [ObservableProperty]
    private double _artistVariety = 0.65;

    [ObservableProperty]
    private double _energy = 0.5;

    [ObservableProperty]
    private List<WaveRecommendation> _recommendations = new();

    [ObservableProperty]
    private List<SnippetMoment> _snippets = new();

    public IReadOnlyList<string> ActivityOptions { get; } = new[] { "Any", "Focus", "Workout", "Drive", "Late Night" };
    public IReadOnlyList<string> CharacterOptions { get; } = new[] { "Balanced", "Velvet", "Bold", "Cinematic", "Nocturnal" };
    public IReadOnlyList<string> MoodOptions { get; } = new[] { "Fluid", "Calm", "Electric", "Warm", "Deep" };
    public IReadOnlyList<string> LanguageOptions { get; } = new[] { "Any", "English", "Russian", "Instrumental" };
    public bool HasRecommendations => Recommendations.Count > 0;
    public bool HasSnippets => Snippets.Count > 0;
    public string SeedLabel => Seed.Subtitle;
    public string TunerSummary => $"{SelectedCharacter} • {SelectedActivity} • familiarity {Familiarity:P0} • variety {ArtistVariety:P0} • energy {Energy:P0}";
    public string PresentationSummary => SelectedPresentation == MyWavePresentation.Flow
        ? "Flow keeps the long-form recommendation lane visible."
        : "Snippets leans into short previews and fast decisions.";
    public string FamiliarityLabel => Familiarity >= 0.5 ? "Leaning familiar" : "Leaning discovery";
    public string PopularityLabel => Popularity >= 0.5 ? "Closer to popular cuts" : "Closer to deep cuts";
    public string VarietyLabel => ArtistVariety >= 0.5 ? "Wide artist spread" : "Focused artist orbit";
    public string EnergyLabel => Energy >= 0.5 ? "More energetic" : "More calm";

    public MyWaveViewModel(
        IRecommendationService recommendationService,
        IPlaybackService playbackService,
        ILibraryService libraryService)
    {
        _recommendationService = recommendationService;
        _playbackService = playbackService;
        _libraryService = libraryService;

        ApplyTuner(_recommendationService.CreateTunerFromSettings());
    }

    partial void OnSelectedActivityChanged(string value) => _ = RefreshWaveAsync();
    partial void OnSelectedCharacterChanged(string value) => _ = RefreshWaveAsync();
    partial void OnSelectedMoodChanged(string value) => _ = RefreshWaveAsync();
    partial void OnSelectedLanguageChanged(string value) => _ = RefreshWaveAsync();
    partial void OnFamiliarityChanged(double value) => _ = RefreshWaveAsync();
    partial void OnPopularityChanged(double value) => _ = RefreshWaveAsync();
    partial void OnArtistVarietyChanged(double value) => _ = RefreshWaveAsync();
    partial void OnEnergyChanged(double value) => _ = RefreshWaveAsync();

    public async Task SetSeedAsync(WaveSeed? seed)
    {
        Seed = seed ?? WaveSeed.Home();
        _suppressedArtistIds.Clear();
        await RefreshWaveAsync();
        OnPropertyChanged(nameof(SeedLabel));
    }

    public async Task RefreshWaveAsync()
    {
        var tuner = CreateTuner();
        await _recommendationService.SaveTunerAsync(tuner);

        Recommendations = _recommendationService.CreateWave(Seed, tuner, 24)
            .Where(item => !_suppressedArtistIds.Contains(item.Track.ArtistId))
            .ToList();

        Snippets = _recommendationService.CreateSnippets(Seed, tuner, 8)
            .Where(item => !_suppressedArtistIds.Contains(item.Track.ArtistId))
            .ToList();

        OnPropertyChanged(nameof(HasRecommendations));
        OnPropertyChanged(nameof(HasSnippets));
        OnPropertyChanged(nameof(TunerSummary));
        OnPropertyChanged(nameof(SeedLabel));
        OnPropertyChanged(nameof(PresentationSummary));
        OnPropertyChanged(nameof(FamiliarityLabel));
        OnPropertyChanged(nameof(PopularityLabel));
        OnPropertyChanged(nameof(VarietyLabel));
        OnPropertyChanged(nameof(EnergyLabel));
    }

    [RelayCommand]
    private Task PlayTrack(WaveRecommendation? recommendation)
    {
        if (recommendation == null)
        {
            return Task.CompletedTask;
        }

        return _playbackService.PlayAsync(recommendation.Track, Recommendations.Select(item => item.Track).ToList());
    }

    [RelayCommand]
    private async Task PlaySnippet(SnippetMoment? snippet)
    {
        if (snippet == null)
        {
            return;
        }

        await _playbackService.PlayAsync(snippet.Track, Snippets.Select(item => item.Track).ToList());
        await _playbackService.SeekAsync(snippet.StartTime);
    }

    [RelayCommand]
    private async Task MoreLikeThis(WaveRecommendation? recommendation)
    {
        if (recommendation == null)
        {
            return;
        }

        await SetSeedAsync(new WaveSeed
        {
            Type = WaveSeedType.Track,
            Id = recommendation.Track.Id,
            Title = recommendation.Track.Title,
            Subtitle = $"Started from {recommendation.Track.ArtistName}"
        });
    }

    [RelayCommand]
    private async Task LessLikeThis(WaveRecommendation? recommendation)
    {
        if (recommendation == null)
        {
            return;
        }

        _suppressedArtistIds.Add(recommendation.Track.ArtistId);
        await RefreshWaveAsync();
    }

    [RelayCommand]
    private async Task ToggleLike(WaveRecommendation? recommendation)
    {
        if (recommendation == null)
        {
            return;
        }

        if (_libraryService.AllTracks.All(track => track.Id != recommendation.Track.Id))
        {
            await _libraryService.AddTrackAsync(recommendation.Track);
        }

        await _libraryService.ToggleLikeAsync(recommendation.Track.Id);
        await RefreshWaveAsync();
    }

    [RelayCommand]
    private void ShowFlow()
    {
        SelectedPresentation = MyWavePresentation.Flow;
    }

    [RelayCommand]
    private void ShowSnippets()
    {
        SelectedPresentation = MyWavePresentation.Snippets;
        OnPropertyChanged(nameof(PresentationSummary));
    }

    [RelayCommand]
    private async Task ResetTuner()
    {
        ApplyTuner(new WaveTunerSettings());
        await RefreshWaveAsync();
    }

    private void ApplyTuner(WaveTunerSettings tuner)
    {
        SelectedActivity = tuner.Activity;
        SelectedCharacter = tuner.Character;
        SelectedMood = tuner.Mood;
        SelectedLanguage = tuner.Language;
        Familiarity = tuner.Familiarity;
        Popularity = tuner.Popularity;
        ArtistVariety = tuner.ArtistVariety;
        Energy = tuner.Energy;
    }

    private WaveTunerSettings CreateTuner()
    {
        return new WaveTunerSettings
        {
            Activity = SelectedActivity,
            Character = SelectedCharacter,
            Mood = SelectedMood,
            Language = SelectedLanguage,
            Familiarity = Familiarity,
            Popularity = Popularity,
            ArtistVariety = ArtistVariety,
            Energy = Energy
        };
    }
}
