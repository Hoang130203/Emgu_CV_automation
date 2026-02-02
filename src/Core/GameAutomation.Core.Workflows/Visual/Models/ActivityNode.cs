namespace GameAutomation.Core.Workflows.Visual.Models;

/// <summary>
/// Represents a node in the visual workflow canvas.
/// Each node corresponds to an activity that can be executed.
/// </summary>
public class ActivityNode
{
    public ActivityNode()
    {
        Id = Guid.NewGuid();
        Properties = new Dictionary<string, object?>();
        InputPorts = new List<Port>();
        OutputPorts = new List<Port>();
    }

    public ActivityNode(string activityType, string displayName) : this()
    {
        ActivityType = activityType;
        DisplayName = displayName;
    }

    /// <summary>
    /// Unique identifier for this node
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Type of activity (matches IActivity.Type)
    /// </summary>
    public string ActivityType { get; set; } = "";

    /// <summary>
    /// Display name shown on the node
    /// </summary>
    public string DisplayName { get; set; } = "";

    /// <summary>
    /// X position on canvas
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Y position on canvas
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Activity-specific properties (template path, delay time, etc.)
    /// </summary>
    public Dictionary<string, object?> Properties { get; set; }

    /// <summary>
    /// Input ports for this node
    /// </summary>
    public List<Port> InputPorts { get; set; }

    /// <summary>
    /// Output ports for this node
    /// </summary>
    public List<Port> OutputPorts { get; set; }

    /// <summary>
    /// Optional comment/description for documentation
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// Whether this node is disabled (skipped during execution)
    /// </summary>
    public bool IsDisabled { get; set; }

    /// <summary>
    /// Get property value with type conversion
    /// </summary>
    public T? GetProperty<T>(string key, T? defaultValue = default)
    {
        if (!Properties.TryGetValue(key, out var value) || value == null)
            return defaultValue;

        if (value is T typedValue)
            return typedValue;

        // Handle JSON deserialization converting numbers
        try
        {
            if (typeof(T) == typeof(int) && value is long longVal)
                return (T)(object)(int)longVal;
            if (typeof(T) == typeof(double) && value is long longVal2)
                return (T)(object)(double)longVal2;
            if (typeof(T) == typeof(float) && value is double doubleVal)
                return (T)(object)(float)doubleVal;

            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Set property value
    /// </summary>
    public void SetProperty(string key, object? value)
    {
        Properties[key] = value;
    }

    /// <summary>
    /// Find port by ID
    /// </summary>
    public Port? FindPort(Guid portId)
    {
        return InputPorts.FirstOrDefault(p => p.Id == portId)
            ?? OutputPorts.FirstOrDefault(p => p.Id == portId);
    }

    /// <summary>
    /// Find port by name
    /// </summary>
    public Port? FindPort(string name, PortDirection direction)
    {
        var ports = direction == PortDirection.Input ? InputPorts : OutputPorts;
        return ports.FirstOrDefault(p => p.Name == name);
    }
}
