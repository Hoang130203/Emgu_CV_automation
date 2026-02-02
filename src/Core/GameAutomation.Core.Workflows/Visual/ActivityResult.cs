namespace GameAutomation.Core.Workflows.Visual;

/// <summary>
/// Result of executing an activity.
/// Contains success status, output values, and which port to follow next.
/// </summary>
public class ActivityResult
{
    private ActivityResult() { }

    /// <summary>
    /// Whether the activity executed successfully
    /// </summary>
    public bool Success { get; private init; }

    /// <summary>
    /// Error message if execution failed
    /// </summary>
    public string? ErrorMessage { get; private init; }

    /// <summary>
    /// Output values from this activity (keyed by port name)
    /// </summary>
    public IReadOnlyDictionary<string, object?> OutputValues { get; private init; }
        = new Dictionary<string, object?>();

    /// <summary>
    /// Name of the output flow port to follow next.
    /// If null, follows the default "Out" port.
    /// </summary>
    public string? NextPortName { get; private init; }

    /// <summary>
    /// Whether execution should stop after this activity
    /// </summary>
    public bool ShouldStop { get; private init; }

    /// <summary>
    /// Create a successful result
    /// </summary>
    public static ActivityResult Ok(
        Dictionary<string, object?>? outputValues = null,
        string? nextPort = null)
    {
        return new ActivityResult
        {
            Success = true,
            OutputValues = outputValues ?? new Dictionary<string, object?>(),
            NextPortName = nextPort
        };
    }

    /// <summary>
    /// Create a successful result with a single output value
    /// </summary>
    public static ActivityResult Ok(string outputName, object? value, string? nextPort = null)
    {
        return new ActivityResult
        {
            Success = true,
            OutputValues = new Dictionary<string, object?> { { outputName, value } },
            NextPortName = nextPort
        };
    }

    /// <summary>
    /// Create a failed result
    /// </summary>
    public static ActivityResult Fail(string errorMessage)
    {
        return new ActivityResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }

    /// <summary>
    /// Create a result that stops execution (for End activity)
    /// </summary>
    public static ActivityResult Stop()
    {
        return new ActivityResult
        {
            Success = true,
            ShouldStop = true
        };
    }

    /// <summary>
    /// Create a result for branching (Condition activity)
    /// </summary>
    public static ActivityResult Branch(bool condition)
    {
        return new ActivityResult
        {
            Success = true,
            NextPortName = condition ? "True" : "False"
        };
    }
}
