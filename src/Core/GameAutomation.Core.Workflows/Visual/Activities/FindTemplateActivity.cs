using GameAutomation.Core.Workflows.Visual.Models;

namespace GameAutomation.Core.Workflows.Visual.Activities;

/// <summary>
/// Find an image template on screen using computer vision.
/// Outputs location if found, branches to Found/NotFound ports.
/// </summary>
public class FindTemplateActivity : IActivity
{
    public string Type => ActivityTypes.FindTemplate;
    public string DisplayName => "Find Template";
    public string Category => "Vision";
    public string Description => "Search for an image template on the screen";
    public string Icon => "ImageSearch";

    public IReadOnlyList<Port> GetInputPorts() => new[]
    {
        Port.FlowInput("In")
    };

    public IReadOnlyList<Port> GetOutputPorts() => new[]
    {
        Port.FlowOutput("Found"),
        Port.FlowOutput("NotFound"),
        Port.DataOutput("X", DataType.Number),
        Port.DataOutput("Y", DataType.Number),
        Port.DataOutput("CenterX", DataType.Number),
        Port.DataOutput("CenterY", DataType.Number),
        Port.DataOutput("Confidence", DataType.Number)
    };

    public IReadOnlyList<ActivityPropertyDefinition> GetPropertyDefinitions() => new[]
    {
        new ActivityPropertyDefinition("TemplatePath", "Template Image", PropertyType.FilePath)
        {
            IsRequired = true,
            Description = "Path to the template image file"
        },
        new ActivityPropertyDefinition("Threshold", "Match Threshold", PropertyType.Number)
        {
            DefaultValue = 0.8,
            MinValue = 0.0,
            MaxValue = 1.0,
            Description = "Minimum confidence (0-1) to consider a match"
        },
        new ActivityPropertyDefinition("WindowTitle", "Target Window", PropertyType.String)
        {
            Description = "Window title to capture (empty for full screen)"
        },
        new ActivityPropertyDefinition("UseMultiScale", "Multi-Scale Search", PropertyType.Boolean)
        {
            DefaultValue = false,
            Description = "Search at multiple scales for better accuracy"
        },
        new ActivityPropertyDefinition("SaveToVariable", "Save Result To", PropertyType.String)
        {
            Description = "Variable name to store the result"
        }
    };

    public async Task<ActivityResult> ExecuteAsync(
        IActivityContext context,
        ActivityNode node,
        IReadOnlyDictionary<string, object?> inputValues)
    {
        var templatePath = node.GetProperty<string>("TemplatePath");
        if (string.IsNullOrEmpty(templatePath))
        {
            return ActivityResult.Fail("Template path is required");
        }

        if (!File.Exists(templatePath))
        {
            return ActivityResult.Fail($"Template file not found: {templatePath}");
        }

        var threshold = node.GetProperty("Threshold", 0.8);
        var windowTitle = node.GetProperty<string>("WindowTitle");
        var useMultiScale = node.GetProperty("UseMultiScale", false);
        var variableName = node.GetProperty<string>("SaveToVariable");

        context.Log($"Searching for template: {Path.GetFileName(templatePath)}");

        try
        {
            // Capture screen
            using var screenshot = context.Vision.CaptureScreen(windowTitle);

            // Find template
            var results = useMultiScale
                ? context.Vision.FindTemplateMultiScale(screenshot, templatePath, threshold)
                : context.Vision.FindTemplate(screenshot, templatePath, threshold);

            if (results.Count > 0)
            {
                var best = results[0]; // Already sorted by confidence
                var centerX = best.X + best.Width / 2;
                var centerY = best.Y + best.Height / 2;

                context.Log($"Template found at ({centerX}, {centerY}) with {best.Confidence:P0} confidence");

                // Save to variable if specified
                if (!string.IsNullOrEmpty(variableName))
                {
                    context.Variables[variableName] = new
                    {
                        Found = true,
                        X = best.X,
                        Y = best.Y,
                        CenterX = centerX,
                        CenterY = centerY,
                        Width = best.Width,
                        Height = best.Height,
                        Confidence = best.Confidence
                    };
                }

                return ActivityResult.Ok(
                    new Dictionary<string, object?>
                    {
                        ["X"] = best.X,
                        ["Y"] = best.Y,
                        ["CenterX"] = centerX,
                        ["CenterY"] = centerY,
                        ["Confidence"] = best.Confidence
                    },
                    "Found");
            }
            else
            {
                context.Log("Template not found");

                if (!string.IsNullOrEmpty(variableName))
                {
                    context.Variables[variableName] = new { Found = false };
                }

                return ActivityResult.Ok(nextPort: "NotFound");
            }
        }
        catch (Exception ex)
        {
            return ActivityResult.Fail($"Template search failed: {ex.Message}");
        }
    }
}
