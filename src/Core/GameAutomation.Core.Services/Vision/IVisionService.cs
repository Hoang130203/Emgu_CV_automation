using GameAutomation.Core.Models.Vision;
using System.Drawing;

namespace GameAutomation.Core.Services.Vision;

/// <summary>
/// Interface for computer vision operations using EmguCV
/// </summary>
public interface IVisionService
{
    // === Async Methods (Original) ===

    /// <summary>
    /// Captures the current screen or specific window (async)
    /// </summary>
    Task<byte[]> CaptureScreenAsync(string? windowTitle = null);

    /// <summary>
    /// Detects an object or template in the captured image (async)
    /// </summary>
    Task<DetectionResult> DetectAsync(byte[] image, byte[] template);

    /// <summary>
    /// Finds text in the image using OCR (async)
    /// </summary>
    Task<string> ExtractTextAsync(byte[] image);

    /// <summary>
    /// Detects colors in a specific region (async)
    /// </summary>
    Task<bool> DetectColorAsync(byte[] image, int x, int y, int width, int height, byte r, byte g, byte b, int tolerance);

    // === Synchronous Workflow-Friendly Methods ===

    /// <summary>
    /// Captures the current screen or specific window (sync)
    /// Returns bitmap for immediate processing
    /// </summary>
    Bitmap CaptureScreen(string? windowTitle = null);

    /// <summary>
    /// Tìm template trong screenshot với threshold
    /// Returns list of all matches sorted by confidence (highest first)
    /// </summary>
    /// <param name="screenshot">Screenshot bitmap</param>
    /// <param name="templatePath">Path to template image file</param>
    /// <param name="threshold">Match threshold (0.0 to 1.0), default 0.8</param>
    /// <returns>List of detection results</returns>
    List<DetectionResult> FindTemplate(Bitmap screenshot, string templatePath, double threshold = 0.8);

    /// <summary>
    /// Extract text from bitmap using OCR (sync)
    /// </summary>
    string ExtractText(Bitmap image);

    /// <summary>
    /// Check if specific color exists at region (sync)
    /// </summary>
    bool DetectColor(Bitmap image, int x, int y, int width, int height, Color targetColor, int tolerance = 10);

    /// <summary>
    /// Find all occurrences of a specific color in image
    /// </summary>
    List<Point> FindColorPositions(Bitmap image, Color targetColor, int tolerance = 10);
}
