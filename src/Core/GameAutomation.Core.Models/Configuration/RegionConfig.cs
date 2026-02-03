using System.Text.Json.Serialization;
using GameAutomation.Core.Models.Vision;

namespace GameAutomation.Core.Models.Configuration;

/// <summary>
/// Configuration for custom search regions, stored in JSON
/// </summary>
public class RegionConfig
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    [JsonPropertyName("lastModified")]
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("regions")]
    public Dictionary<string, RegionEntry> Regions { get; set; } = new();
}

/// <summary>
/// Single region entry with ratio-based coordinates
/// </summary>
public class RegionEntry
{
    [JsonPropertyName("startX")]
    public double StartX { get; set; }

    [JsonPropertyName("startY")]
    public double StartY { get; set; }

    [JsonPropertyName("endX")]
    public double EndX { get; set; }

    [JsonPropertyName("endY")]
    public double EndY { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    /// <summary>
    /// Convert to SearchRegion for use in vision service
    /// </summary>
    public SearchRegion ToSearchRegion() => new(StartX, StartY, EndX, EndY);

    /// <summary>
    /// Create from SearchRegion
    /// </summary>
    public static RegionEntry FromSearchRegion(SearchRegion region, string? notes = null) => new()
    {
        StartX = region.StartX,
        StartY = region.StartY,
        EndX = region.EndX,
        EndY = region.EndY,
        Enabled = true,
        Notes = notes
    };
}
