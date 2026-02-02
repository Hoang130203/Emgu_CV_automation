using GameAutomation.Core.Workflows.Visual.Models;

namespace GameAutomation.Core.Workflows.Visual.Activities;

/// <summary>
/// End node - terminates workflow execution.
/// Every workflow must have at least one End node.
/// </summary>
public class EndActivity : IActivity
{
    public string Type => ActivityTypes.End;
    public string DisplayName => "End";
    public string Category => "Flow";
    public string Description => "Terminates the workflow";
    public string Icon => "Stop";

    public IReadOnlyList<Port> GetInputPorts() => new[]
    {
        Port.FlowInput("In")
    };

    public IReadOnlyList<Port> GetOutputPorts() => Array.Empty<Port>();

    public IReadOnlyList<ActivityPropertyDefinition> GetPropertyDefinitions() =>
        Array.Empty<ActivityPropertyDefinition>();

    public Task<ActivityResult> ExecuteAsync(
        IActivityContext context,
        ActivityNode node,
        IReadOnlyDictionary<string, object?> inputValues)
    {
        context.Log("Workflow completed");
        return Task.FromResult(ActivityResult.Stop());
    }
}
