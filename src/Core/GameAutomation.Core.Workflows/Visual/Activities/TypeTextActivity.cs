using GameAutomation.Core.Workflows.Visual.Models;

namespace GameAutomation.Core.Workflows.Visual.Activities;

/// <summary>
/// Type text using keyboard input.
/// Can type static text or text from a variable.
/// </summary>
public class TypeTextActivity : IActivity
{
    public string Type => ActivityTypes.TypeText;
    public string DisplayName => "Type Text";
    public string Category => "Input";
    public string Description => "Type text using the keyboard";
    public string Icon => "Keyboard";

    public IReadOnlyList<Port> GetInputPorts() => new[]
    {
        Port.FlowInput("In"),
        Port.DataInput("Text", DataType.String)
    };

    public IReadOnlyList<Port> GetOutputPorts() => new[]
    {
        Port.FlowOutput("Out")
    };

    public IReadOnlyList<ActivityPropertyDefinition> GetPropertyDefinitions() => new[]
    {
        new ActivityPropertyDefinition("Text", "Text to Type", PropertyType.String)
        {
            IsRequired = true,
            Description = "The text to type (can include {variable} placeholders)"
        },
        new ActivityPropertyDefinition("DelayBetweenKeys", "Key Delay (ms)", PropertyType.Number)
        {
            DefaultValue = 50,
            MinValue = 0,
            MaxValue = 1000,
            Description = "Delay between each keystroke"
        },
        new ActivityPropertyDefinition("PressEnter", "Press Enter After", PropertyType.Boolean)
        {
            DefaultValue = false,
            Description = "Press Enter key after typing"
        },
        new ActivityPropertyDefinition("ClearFirst", "Clear Field First", PropertyType.Boolean)
        {
            DefaultValue = false,
            Description = "Select all and delete before typing (Ctrl+A, Delete)"
        }
    };

    public async Task<ActivityResult> ExecuteAsync(
        IActivityContext context,
        ActivityNode node,
        IReadOnlyDictionary<string, object?> inputValues)
    {
        // Get text from input port or property
        var text = inputValues.TryGetValue("Text", out var inputText) && inputText != null
            ? inputText.ToString()
            : node.GetProperty<string>("Text");

        if (string.IsNullOrEmpty(text))
        {
            return ActivityResult.Fail("Text is required");
        }

        // Replace variable placeholders {varName}
        text = ReplaceVariables(text, context.Variables);

        var delayBetweenKeys = node.GetProperty("DelayBetweenKeys", 50);
        var pressEnter = node.GetProperty("PressEnter", false);
        var clearFirst = node.GetProperty("ClearFirst", false);

        context.Log($"Typing: {(text.Length > 50 ? text[..50] + "..." : text)}");

        try
        {
            // Clear field first if requested
            if (clearFirst)
            {
                context.Input.KeyCombination(
                    Services.Input.VirtualKeyCode.CONTROL,
                    Services.Input.VirtualKeyCode.A);
                await Task.Delay(50, context.CancellationToken);
                context.Input.KeyPress(Services.Input.VirtualKeyCode.DELETE);
                await Task.Delay(100, context.CancellationToken);
            }

            // Type text with delay
            await context.Input.TypeTextAsync(text, delayBetweenKeys);

            // Press Enter if requested
            if (pressEnter)
            {
                await Task.Delay(50, context.CancellationToken);
                context.Input.KeyPress(Services.Input.VirtualKeyCode.RETURN);
            }

            return ActivityResult.Ok();
        }
        catch (Exception ex)
        {
            return ActivityResult.Fail($"Typing failed: {ex.Message}");
        }
    }

    private static string ReplaceVariables(string text, IDictionary<string, object?> variables)
    {
        // Replace {varName} with variable values
        foreach (var kvp in variables)
        {
            var placeholder = "{" + kvp.Key + "}";
            if (text.Contains(placeholder))
            {
                text = text.Replace(placeholder, kvp.Value?.ToString() ?? "");
            }
        }
        return text;
    }
}
