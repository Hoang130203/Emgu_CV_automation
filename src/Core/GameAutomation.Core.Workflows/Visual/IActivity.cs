using GameAutomation.Core.Workflows.Visual.Models;

namespace GameAutomation.Core.Workflows.Visual;

/// <summary>
/// Interface for all visual workflow activities.
/// Each activity type implements this to define its behavior.
/// </summary>
public interface IActivity
{
    /// <summary>
    /// Unique type identifier (e.g., "FindTemplate", "Click")
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Human-readable display name
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Category for toolbox grouping
    /// </summary>
    string Category { get; }

    /// <summary>
    /// Description of what this activity does
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Icon name for UI (Material Design icon name)
    /// </summary>
    string Icon { get; }

    /// <summary>
    /// Get the input ports for this activity type
    /// </summary>
    IReadOnlyList<Port> GetInputPorts();

    /// <summary>
    /// Get the output ports for this activity type
    /// </summary>
    IReadOnlyList<Port> GetOutputPorts();

    /// <summary>
    /// Get the property definitions for this activity
    /// </summary>
    IReadOnlyList<ActivityPropertyDefinition> GetPropertyDefinitions();

    /// <summary>
    /// Execute this activity with the given context and node data
    /// </summary>
    /// <param name="context">Execution context with services and variables</param>
    /// <param name="node">The node instance with properties and connections</param>
    /// <param name="inputValues">Values from connected input data ports</param>
    /// <returns>Result including output values and next port to follow</returns>
    Task<ActivityResult> ExecuteAsync(
        IActivityContext context,
        ActivityNode node,
        IReadOnlyDictionary<string, object?> inputValues);
}

/// <summary>
/// Definition of a property that can be configured on an activity
/// </summary>
public class ActivityPropertyDefinition
{
    public ActivityPropertyDefinition(string name, string displayName, PropertyType type)
    {
        Name = name;
        DisplayName = displayName;
        Type = type;
    }

    public string Name { get; }
    public string DisplayName { get; }
    public PropertyType Type { get; }
    public object? DefaultValue { get; init; }
    public bool IsRequired { get; init; }
    public string? Description { get; init; }
    public string[]? Options { get; init; } // For Enum type
    public double? MinValue { get; init; }  // For Number type
    public double? MaxValue { get; init; }  // For Number type
}

/// <summary>
/// Types of properties for the property editor
/// </summary>
public enum PropertyType
{
    String,
    Number,
    Boolean,
    FilePath,
    Enum,
    Point,
    Rectangle,
    Color
}
