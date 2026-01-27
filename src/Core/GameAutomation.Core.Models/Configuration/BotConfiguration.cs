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
}

public enum AIProvider
{
    None,
    MLNet,
    OpenAI,
    Anthropic
}
