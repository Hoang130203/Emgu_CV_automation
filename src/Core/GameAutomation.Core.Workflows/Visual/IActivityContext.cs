using GameAutomation.Core.Services.Input;
using GameAutomation.Core.Services.Vision;

namespace GameAutomation.Core.Workflows.Visual;

/// <summary>
/// Context passed to activities during execution.
/// Provides access to services and shared state.
/// </summary>
public interface IActivityContext
{
    /// <summary>
    /// Vision service for template matching and screen capture
    /// </summary>
    IVisionService Vision { get; }

    /// <summary>
    /// Input service for mouse and keyboard operations
    /// </summary>
    IInputService Input { get; }

    /// <summary>
    /// Shared variables between activities in a workflow
    /// </summary>
    IDictionary<string, object?> Variables { get; }

    /// <summary>
    /// Cancellation token for stopping workflow execution
    /// </summary>
    CancellationToken CancellationToken { get; }

    /// <summary>
    /// Log a message during execution
    /// </summary>
    void Log(string message);

    /// <summary>
    /// Log an error message
    /// </summary>
    void LogError(string message);
}
