using GameAutomation.Core.Models.Configuration;
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

    // === Feature Matching Methods ===

    /// <summary>
    /// Find template using feature matching (ORB/SIFT)
    /// Better for different scales, rotations, or color variations
    /// </summary>
    /// <param name="screenshot">Screenshot bitmap</param>
    /// <param name="templatePath">Path to template image file</param>
    /// <param name="algorithm">Feature matching algorithm (ORB or SIFT)</param>
    /// <param name="minMatchCount">Minimum number of good matches required</param>
    /// <param name="ratioThreshold">Lowe's ratio test threshold (0.0 to 1.0)</param>
    /// <returns>List of detection results</returns>
    List<DetectionResult> FindTemplateWithFeatures(
        Bitmap screenshot,
        string templatePath,
        FeatureMatchingAlgorithm algorithm = FeatureMatchingAlgorithm.ORB,
        int minMatchCount = 10,
        double ratioThreshold = 0.75);

    /// <summary>
    /// Unified template finding method - automatically chooses strategy
    /// </summary>
    /// <param name="screenshot">Screenshot bitmap</param>
    /// <param name="templatePath">Path to template image file</param>
    /// <param name="useFeatureMatching">Use feature matching instead of template matching</param>
    /// <param name="threshold">Match threshold for template matching (0.0 to 1.0)</param>
    /// <param name="algorithm">Feature matching algorithm if useFeatureMatching is true</param>
    /// <param name="minMatchCount">Min matches for feature matching</param>
    /// <param name="ratioThreshold">Lowe's ratio for feature matching</param>
    /// <returns>List of detection results</returns>
    List<DetectionResult> FindTemplateAuto(
        Bitmap screenshot,
        string templatePath,
        bool useFeatureMatching = false,
        double threshold = 0.8,
        FeatureMatchingAlgorithm algorithm = FeatureMatchingAlgorithm.ORB,
        int minMatchCount = 10,
        double ratioThreshold = 0.75);

    /// <summary>
    /// Find template using multi-scale matching (searches at different sizes)
    /// Best for icons that may appear at different scales on different machines
    /// </summary>
    /// <param name="screenshot">Screenshot bitmap</param>
    /// <param name="templatePath">Path to template image file</param>
    /// <param name="threshold">Match threshold (0.0 to 1.0)</param>
    /// <param name="minScale">Minimum scale factor (e.g., 0.5 = 50%)</param>
    /// <param name="maxScale">Maximum scale factor (e.g., 2.0 = 200%)</param>
    /// <param name="scaleSteps">Number of scale steps to try</param>
    /// <returns>List of detection results with best match across all scales</returns>
    List<DetectionResult> FindTemplateMultiScale(
        Bitmap screenshot,
        string templatePath,
        double threshold = 0.7,
        double minScale = 0.5,
        double maxScale = 2.0,
        int scaleSteps = 10);

    // === ROI-Based Search Methods (Optimized) ===

    /// <summary>
    /// Find template in a specific region of interest (ROI) for faster search
    /// </summary>
    /// <param name="screenshot">Screenshot bitmap</param>
    /// <param name="templatePath">Path to template image file</param>
    /// <param name="region">Search region (ratio-based)</param>
    /// <param name="threshold">Match threshold (0.0 to 1.0)</param>
    /// <returns>List of detection results with coordinates relative to full screenshot</returns>
    List<DetectionResult> FindTemplateInRegion(
        Bitmap screenshot,
        string templatePath,
        SearchRegion region,
        double threshold = 0.8);

    /// <summary>
    /// Find template using multi-scale matching within a specific region of interest
    /// </summary>
    /// <param name="screenshot">Screenshot bitmap</param>
    /// <param name="templatePath">Path to template image file</param>
    /// <param name="region">Search region (ratio-based), null = full screen</param>
    /// <param name="threshold">Match threshold (0.0 to 1.0)</param>
    /// <param name="minScale">Minimum scale factor</param>
    /// <param name="maxScale">Maximum scale factor</param>
    /// <param name="scaleSteps">Number of scale steps to try</param>
    /// <returns>List of detection results with coordinates relative to full screenshot</returns>
    List<DetectionResult> FindTemplateMultiScaleInRegion(
        Bitmap screenshot,
        string templatePath,
        SearchRegion? region,
        double threshold = 0.7,
        double minScale = 0.5,
        double maxScale = 2.0,
        int scaleSteps = 10);

    /// <summary>
    /// Get window position and size for coordinate transformation
    /// </summary>
    /// <param name="windowTitle">Window title to find</param>
    /// <returns>Window bounds (x, y, width, height) or null if not found</returns>
    (int x, int y, int width, int height)? GetWindowBounds(string windowTitle);
}
