namespace GameAutomation.Core.Models.Vision;

/// <summary>
/// Represents the result of a vision detection operation
/// </summary>
public class DetectionResult
{
    public bool Found { get; set; }
    public double Confidence { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string? Label { get; set; }
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}
