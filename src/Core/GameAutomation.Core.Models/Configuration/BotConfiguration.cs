namespace GameAutomation.Core.Models.Configuration;

/// <summary>
/// Configuration settings for the game automation bot
/// </summary>
public class BotConfiguration
{
    public string GameWindowTitle { get; set; } = string.Empty;
    public int ScreenCaptureIntervalMs { get; set; } = 100;
    public double DetectionConfidenceThreshold { get; set; } = 0.8;
    public bool UseAI { get; set; } = false;
    public AIProvider AIProvider { get; set; } = AIProvider.None;
    public string? AIApiKey { get; set; }

    // Vision Settings - Feature Matching
    public bool UseFeatureMatching { get; set; } = false;
    public FeatureMatchingAlgorithm FeatureAlgorithm { get; set; } = FeatureMatchingAlgorithm.ORB;
    public int MinMatchCount { get; set; } = 10;
    public double FeatureMatchRatio { get; set; } = 0.75; // Lowe's ratio test
}

public enum AIProvider
{
    None,
    MLNet,
    OpenAI,
    Anthropic
}

/// <summary>
/// Feature matching algorithms for template detection
/// </summary>
public enum FeatureMatchingAlgorithm
{
    /// <summary>
    /// Scale-Invariant Feature Transform - more accurate, handles scale/rotation well
    /// </summary>
    SIFT,

    /// <summary>
    /// Oriented FAST and Rotated BRIEF - faster, patent-free, good for real-time
    /// </summary>
    ORB
}
