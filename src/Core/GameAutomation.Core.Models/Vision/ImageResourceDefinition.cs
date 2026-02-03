namespace GameAutomation.Core.Models.Vision;

/// <summary>
/// Defines an image resource with its search region for optimized template matching
/// </summary>
public class ImageResourceDefinition
{
    /// <summary>
    /// Unique key identifying this resource (e.g., "daily/02_daily_maintale")
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Relative path to the template file (from Templates root)
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>
    /// Search region for this resource (null = full screen)
    /// </summary>
    public SearchRegion? Region { get; set; }

    /// <summary>
    /// Custom threshold for this resource (null = use default)
    /// </summary>
    public double? Threshold { get; set; }

    /// <summary>
    /// Whether this resource uses multi-scale matching
    /// </summary>
    public bool UseMultiScale { get; set; } = true;

    public ImageResourceDefinition() { }

    public ImageResourceDefinition(string key, string relativePath, SearchRegion? region = null)
    {
        Key = key;
        RelativePath = relativePath;
        Region = region;
    }
}
