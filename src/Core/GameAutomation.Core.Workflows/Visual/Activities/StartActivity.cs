using GameAutomation.Core.Workflows.Visual.Models;

namespace GameAutomation.Core.Workflows.Visual.Activities;

/// <summary>
/// Start node - entry point for workflow execution.
/// Every workflow must have exactly one Start node.
/// </summary>
public class StartActivity : IActivity
{
    public string Type => ActivityTypes.Start;
    public string DisplayName => "Start";
    public string Category => "Flow";
    public string Description => "Entry point for the workflow";
    public string Icon => "Play";

    public IReadOnlyList<Port> GetInputPorts() => Array.Empty<Port>();

    public IReadOnlyList<Port> GetOutputPorts() => new[]
    {
        Port.FlowOutput("Out")
    };

    public IReadOnlyList<ActivityPropertyDefinition> GetPropertyDefinitions() =>
        Array.Empty<ActivityPropertyDefinition>();

    public Task<ActivityResult> ExecuteAsync(
        IActivityContext context,
        ActivityNode node,
        IReadOnlyDictionary<string, object?> inputValues)
    {
        context.Log("Workflow started");
        return Task.FromResult(ActivityResult.Ok());
    }
}
