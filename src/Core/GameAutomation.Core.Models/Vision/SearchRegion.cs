namespace GameAutomation.Core.Models.Vision;

/// <summary>
/// Defines a search region using ratio-based coordinates (0.0 to 1.0)
/// Relative to the target window/screen dimensions
/// </summary>
public class SearchRegion
{
    /// <summary>
    /// Start X ratio (0.0 = left edge, 1.0 = right edge)
    /// </summary>
    public double StartX { get; set; } = 0.0;

    /// <summary>
    /// Start Y ratio (0.0 = top edge, 1.0 = bottom edge)
    /// </summary>
    public double StartY { get; set; } = 0.0;

    /// <summary>
    /// End X ratio (0.0 = left edge, 1.0 = right edge)
    /// </summary>
    public double EndX { get; set; } = 1.0;

    /// <summary>
    /// End Y ratio (0.0 = top edge, 1.0 = bottom edge)
    /// </summary>
    public double EndY { get; set; } = 1.0;

    /// <summary>
    /// Full screen region (no restriction)
    /// </summary>
    public static SearchRegion FullScreen => new();

    /// <summary>
    /// Check if this is a full screen region
    /// </summary>
    public bool IsFullScreen =>
        StartX <= 0.0 && StartY <= 0.0 && EndX >= 1.0 && EndY >= 1.0;

    public SearchRegion() { }

    public SearchRegion(double startX, double startY, double endX, double endY)
    {
        StartX = Math.Clamp(startX, 0.0, 1.0);
        StartY = Math.Clamp(startY, 0.0, 1.0);
        EndX = Math.Clamp(endX, 0.0, 1.0);
        EndY = Math.Clamp(endY, 0.0, 1.0);
    }

    /// <summary>
    /// Calculate absolute pixel bounds from dimensions
    /// </summary>
    public (int x, int y, int width, int height) ToPixelBounds(int totalWidth, int totalHeight)
    {
        int x = (int)(StartX * totalWidth);
        int y = (int)(StartY * totalHeight);
        int endXPx = (int)(EndX * totalWidth);
        int endYPx = (int)(EndY * totalHeight);

        return (x, y, endXPx - x, endYPx - y);
    }

    public override string ToString() =>
        $"Region({StartX:F2}, {StartY:F2}) -> ({EndX:F2}, {EndY:F2})";
}
