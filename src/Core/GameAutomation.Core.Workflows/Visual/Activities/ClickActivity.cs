using GameAutomation.Core.Services.Input;
using GameAutomation.Core.Workflows.Visual.Models;

namespace GameAutomation.Core.Workflows.Visual.Activities;

/// <summary>
/// Click at a position on screen.
/// Can use fixed coordinates or data from connected FindTemplate.
/// </summary>
public class ClickActivity : IActivity
{
    public string Type => ActivityTypes.Click;
    public string DisplayName => "Click";
    public string Category => "Input";
    public string Description => "Click at a position on the screen";
    public string Icon => "CursorDefaultClick";

    public IReadOnlyList<Port> GetInputPorts() => new[]
    {
        Port.FlowInput("In"),
        Port.DataInput("X", DataType.Number),
        Port.DataInput("Y", DataType.Number)
    };

    public IReadOnlyList<Port> GetOutputPorts() => new[]
    {
        Port.FlowOutput("Out")
    };

    public IReadOnlyList<ActivityPropertyDefinition> GetPropertyDefinitions() => new[]
    {
        new ActivityPropertyDefinition("X", "X Position", PropertyType.Number)
        {
            DefaultValue = 0,
            Description = "X coordinate (overridden by connected data port)"
        },
        new ActivityPropertyDefinition("Y", "Y Position", PropertyType.Number)
        {
            DefaultValue = 0,
            Description = "Y coordinate (overridden by connected data port)"
        },
        new ActivityPropertyDefinition("ClickType", "Click Type", PropertyType.Enum)
        {
            DefaultValue = "Left",
            Options = new[] { "Left", "Right", "Double", "Middle" },
            Description = "Type of mouse click"
        },
        new ActivityPropertyDefinition("OffsetX", "Offset X", PropertyType.Number)
        {
            DefaultValue = 0,
            Description = "Additional X offset from the target position"
        },
        new ActivityPropertyDefinition("OffsetY", "Offset Y", PropertyType.Number)
        {
            DefaultValue = 0,
            Description = "Additional Y offset from the target position"
        },
        new ActivityPropertyDefinition("DelayBefore", "Delay Before (ms)", PropertyType.Number)
        {
            DefaultValue = 0,
            MinValue = 0,
            MaxValue = 60000,
            Description = "Wait before clicking"
        },
        new ActivityPropertyDefinition("DelayAfter", "Delay After (ms)", PropertyType.Number)
        {
            DefaultValue = 100,
            MinValue = 0,
            MaxValue = 60000,
            Description = "Wait after clicking"
        }
    };

    public async Task<ActivityResult> ExecuteAsync(
        IActivityContext context,
        ActivityNode node,
        IReadOnlyDictionary<string, object?> inputValues)
    {
        // Get position from input ports or properties
        var x = GetIntValue(inputValues, "X") ?? node.GetProperty("X", 0);
        var y = GetIntValue(inputValues, "Y") ?? node.GetProperty("Y", 0);

        // Apply offsets
        var offsetX = node.GetProperty("OffsetX", 0);
        var offsetY = node.GetProperty("OffsetY", 0);
        x += offsetX;
        y += offsetY;

        var clickType = node.GetProperty("ClickType", "Left");
        var delayBefore = node.GetProperty("DelayBefore", 0);
        var delayAfter = node.GetProperty("DelayAfter", 100);

        // Delay before
        if (delayBefore > 0)
        {
            await Task.Delay(delayBefore, context.CancellationToken);
        }

        context.Log($"Clicking at ({x}, {y}) - {clickType}");

        try
        {
            // Move mouse first
            context.Input.MoveMouse(x, y);
            await Task.Delay(50, context.CancellationToken); // Small delay for smooth movement

            // Perform click
            switch (clickType.ToLowerInvariant())
            {
                case "left":
                    context.Input.LeftClick();
                    break;
                case "right":
                    context.Input.RightClick();
                    break;
                case "double":
                    context.Input.DoubleClick();
                    break;
                case "middle":
                    // Middle click not directly available, simulate
                    context.Input.LeftClick(); // Fallback
                    break;
                default:
                    context.Input.LeftClick();
                    break;
            }

            // Delay after
            if (delayAfter > 0)
            {
                await Task.Delay(delayAfter, context.CancellationToken);
            }

            return ActivityResult.Ok();
        }
        catch (Exception ex)
        {
            return ActivityResult.Fail($"Click failed: {ex.Message}");
        }
    }

    private static int? GetIntValue(IReadOnlyDictionary<string, object?> values, string key)
    {
        if (!values.TryGetValue(key, out var value) || value == null)
            return null;

        return value switch
        {
            int i => i,
            long l => (int)l,
            double d => (int)d,
            float f => (int)f,
            _ => null
        };
    }
}
