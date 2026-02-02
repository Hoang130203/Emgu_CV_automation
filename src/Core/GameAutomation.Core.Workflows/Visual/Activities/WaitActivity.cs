using GameAutomation.Core.Workflows.Visual.Models;

namespace GameAutomation.Core.Workflows.Visual.Activities;

/// <summary>
/// Wait for a specified duration.
/// Simple delay between activities.
/// </summary>
public class WaitActivity : IActivity
{
    public string Type => ActivityTypes.Wait;
    public string DisplayName => "Wait";
    public string Category => "Flow";
    public string Description => "Pause execution for a specified time";
    public string Icon => "Timer";

    public IReadOnlyList<Port> GetInputPorts() => new[]
    {
        Port.FlowInput("In"),
        Port.DataInput("Duration", DataType.Number)
    };

    public IReadOnlyList<Port> GetOutputPorts() => new[]
    {
        Port.FlowOutput("Out")
    };

    public IReadOnlyList<ActivityPropertyDefinition> GetPropertyDefinitions() => new[]
    {
        new ActivityPropertyDefinition("Duration", "Duration (ms)", PropertyType.Number)
        {
            DefaultValue = 1000,
            MinValue = 0,
            MaxValue = 3600000, // 1 hour max
            IsRequired = true,
            Description = "Time to wait in milliseconds"
        },
        new ActivityPropertyDefinition("RandomVariation", "Random Variation (ms)", PropertyType.Number)
        {
            DefaultValue = 0,
            MinValue = 0,
            MaxValue = 10000,
            Description = "Add random variation to duration (±ms)"
        }
    };

    public async Task<ActivityResult> ExecuteAsync(
        IActivityContext context,
        ActivityNode node,
        IReadOnlyDictionary<string, object?> inputValues)
    {
        // Get duration from input port or property
        var duration = inputValues.TryGetValue("Duration", out var inputDuration) && inputDuration != null
            ? Convert.ToInt32(inputDuration)
            : node.GetProperty("Duration", 1000);

        var randomVariation = node.GetProperty("RandomVariation", 0);

        // Add random variation
        if (randomVariation > 0)
        {
            var random = new Random();
            duration += random.Next(-randomVariation, randomVariation + 1);
            duration = Math.Max(0, duration); // Ensure non-negative
        }

        context.Log($"Waiting {duration}ms");

        try
        {
            await Task.Delay(duration, context.CancellationToken);
            return ActivityResult.Ok();
        }
        catch (OperationCanceledException)
        {
            return ActivityResult.Fail("Wait cancelled");
        }
    }
}
