namespace GameAutomation.AI.OpenAI;

/// <summary>
/// Interface for OpenAI GPT-4 Vision API integration
/// </summary>
public interface IOpenAIVisionService
{
    /// <summary>
    /// Analyzes an image using GPT-4 Vision
    /// </summary>
    Task<VisionAnalysisResult> AnalyzeImageAsync(byte[] imageData, string prompt);

    /// <summary>
    /// Asks a question about the game state based on a screenshot
    /// </summary>
    Task<string> AskAboutGameStateAsync(byte[] screenshot, string question);
}

public class VisionAnalysisResult
{
    public string Description { get; set; } = string.Empty;
    public List<string> DetectedObjects { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}
