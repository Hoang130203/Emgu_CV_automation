using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameAutomation.Core.Models.Configuration;
using System.Windows;

namespace GameAutomation.UI.WPF.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly Action<bool>? _closeAction;

    // Vision Settings
    [ObservableProperty]
    private bool _useFeatureMatching;

    [ObservableProperty]
    private FeatureMatchingAlgorithm _featureAlgorithm = FeatureMatchingAlgorithm.ORB;

    [ObservableProperty]
    private int _minMatchCount = 10;

    [ObservableProperty]
    private double _featureMatchRatio = 0.75;

    // Detection Settings
    [ObservableProperty]
    private double _templateThreshold = 0.8;

    [ObservableProperty]
    private int _screenCaptureIntervalMs = 100;

    // AI Settings
    [ObservableProperty]
    private AIProvider _selectedAIProvider = AIProvider.None;

    [ObservableProperty]
    private string _apiKey = string.Empty;

    // Available options for dropdowns
    public FeatureMatchingAlgorithm[] AvailableAlgorithms { get; } =
        Enum.GetValues<FeatureMatchingAlgorithm>();

    public AIProvider[] AvailableAIProviders { get; } =
        Enum.GetValues<AIProvider>();

    public SettingsViewModel()
    {
    }

    public SettingsViewModel(Action<bool> closeAction)
    {
        _closeAction = closeAction;
    }

    public void LoadFromConfiguration(BotConfiguration config)
    {
        UseFeatureMatching = config.UseFeatureMatching;
        FeatureAlgorithm = config.FeatureAlgorithm;
        MinMatchCount = config.MinMatchCount;
        FeatureMatchRatio = config.FeatureMatchRatio;
        TemplateThreshold = config.DetectionConfidenceThreshold;
        ScreenCaptureIntervalMs = config.ScreenCaptureIntervalMs;
        SelectedAIProvider = config.AIProvider;
        ApiKey = config.AIApiKey ?? string.Empty;
    }

    public void SaveToConfiguration(BotConfiguration config)
    {
        config.UseFeatureMatching = UseFeatureMatching;
        config.FeatureAlgorithm = FeatureAlgorithm;
        config.MinMatchCount = MinMatchCount;
        config.FeatureMatchRatio = FeatureMatchRatio;
        config.DetectionConfidenceThreshold = TemplateThreshold;
        config.ScreenCaptureIntervalMs = ScreenCaptureIntervalMs;
        config.AIProvider = SelectedAIProvider;
        config.AIApiKey = string.IsNullOrWhiteSpace(ApiKey) ? null : ApiKey;
    }

    [RelayCommand]
    private void Save()
    {
        _closeAction?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        _closeAction?.Invoke(false);
    }
}
