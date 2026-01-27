using GameAutomation.Core.Models.GameState;

namespace GameAutomation.Core.Workflows;

/// <summary>
/// Interface for game automation workflows
/// </summary>
public interface IWorkflow
{
    string Name { get; }

    /// <summary>
    /// Executes the workflow with the given game context
    /// </summary>
    Task<WorkflowResult> ExecuteAsync(GameContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines if this workflow can handle the current game state
    /// </summary>
    Task<bool> CanExecuteAsync(GameContext context);
}

public class WorkflowResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}
