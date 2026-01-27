namespace GameAutomation.AI.Anthropic;

/// <summary>
/// Interface for Anthropic Claude Vision API integration
/// </summary>
public interface IAnthropicVisionService
{
    /// <summary>
    /// Analyzes an image using Claude Vision
    /// </summary>
    Task<ClaudeVisionResult> AnalyzeImageAsync(byte[] imageData, string prompt);

    /// <summary>
    /// Gets strategic advice for game automation based on screenshots
    /// </summary>
    Task<string> GetGameAdviceAsync(byte[] screenshot, string context);
}

public class ClaudeVisionResult
{
    public string Analysis { get; set; } = string.Empty;
    public List<string> Suggestions { get; set; } = new();
    public Dictionary<string, object> ExtractedData { get; set; } = new();
}
