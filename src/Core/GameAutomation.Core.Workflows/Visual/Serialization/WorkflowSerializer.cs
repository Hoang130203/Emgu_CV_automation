using GameAutomation.Core.Workflows.Visual.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameAutomation.Core.Workflows.Visual.Serialization;

/// <summary>
/// Serializes and deserializes visual workflows to/from JSON.
/// </summary>
public class WorkflowSerializer
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Serialize a workflow to JSON string
    /// </summary>
    public string Serialize(VisualWorkflow workflow)
    {
        return JsonSerializer.Serialize(workflow, DefaultOptions);
    }

    /// <summary>
    /// Deserialize a workflow from JSON string
    /// </summary>
    public VisualWorkflow? Deserialize(string json)
    {
        return JsonSerializer.Deserialize<VisualWorkflow>(json, DefaultOptions);
    }

    /// <summary>
    /// Save workflow to file
    /// </summary>
    public async Task SaveToFileAsync(VisualWorkflow workflow, string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = Serialize(workflow);
        await File.WriteAllTextAsync(filePath, json);
    }

    /// <summary>
    /// Save workflow to file (sync)
    /// </summary>
    public void SaveToFile(VisualWorkflow workflow, string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = Serialize(workflow);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Load workflow from file
    /// </summary>
    public async Task<VisualWorkflow?> LoadFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Workflow file not found: {filePath}");
        }

        var json = await File.ReadAllTextAsync(filePath);
        return Deserialize(json);
    }

    /// <summary>
    /// Load workflow from file (sync)
    /// </summary>
    public VisualWorkflow? LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Workflow file not found: {filePath}");
        }

        var json = File.ReadAllText(filePath);
        return Deserialize(json);
    }

    /// <summary>
    /// Create a sample workflow for testing
    /// </summary>
    public static VisualWorkflow CreateSampleWorkflow()
    {
        var workflow = new VisualWorkflow("Sample Workflow")
        {
            Description = "A simple find and click workflow"
        };

        // Create Start node
        var startNode = new ActivityNode(ActivityTypes.Start, "Start")
        {
            X = 100,
            Y = 200
        };
        startNode.OutputPorts.Add(Port.FlowOutput("Out"));
        workflow.AddNode(startNode);

        // Create FindTemplate node
        var findNode = new ActivityNode(ActivityTypes.FindTemplate, "Find Button")
        {
            X = 300,
            Y = 200
        };
        findNode.InputPorts.Add(Port.FlowInput("In"));
        findNode.OutputPorts.Add(Port.FlowOutput("Found"));
        findNode.OutputPorts.Add(Port.FlowOutput("NotFound"));
        findNode.OutputPorts.Add(Port.DataOutput("CenterX", DataType.Number));
        findNode.OutputPorts.Add(Port.DataOutput("CenterY", DataType.Number));
        findNode.Properties["TemplatePath"] = "Assets/button.png";
        findNode.Properties["Threshold"] = 0.8;
        workflow.AddNode(findNode);

        // Create Click node
        var clickNode = new ActivityNode(ActivityTypes.Click, "Click Button")
        {
            X = 500,
            Y = 150
        };
        clickNode.InputPorts.Add(Port.FlowInput("In"));
        clickNode.InputPorts.Add(Port.DataInput("X", DataType.Number));
        clickNode.InputPorts.Add(Port.DataInput("Y", DataType.Number));
        clickNode.OutputPorts.Add(Port.FlowOutput("Out"));
        workflow.AddNode(clickNode);

        // Create End nodes
        var endNode1 = new ActivityNode(ActivityTypes.End, "Success")
        {
            X = 700,
            Y = 150
        };
        endNode1.InputPorts.Add(Port.FlowInput("In"));
        workflow.AddNode(endNode1);

        var endNode2 = new ActivityNode(ActivityTypes.End, "Not Found")
        {
            X = 500,
            Y = 300
        };
        endNode2.InputPorts.Add(Port.FlowInput("In"));
        workflow.AddNode(endNode2);

        // Create connections
        // Start -> FindTemplate
        workflow.AddConnection(new Connection(
            startNode.Id, startNode.OutputPorts[0].Id,
            findNode.Id, findNode.InputPorts[0].Id));

        // FindTemplate (Found) -> Click
        workflow.AddConnection(new Connection(
            findNode.Id, findNode.OutputPorts[0].Id,
            clickNode.Id, clickNode.InputPorts[0].Id));

        // FindTemplate CenterX -> Click X
        workflow.AddConnection(new Connection(
            findNode.Id, findNode.OutputPorts[2].Id,
            clickNode.Id, clickNode.InputPorts[1].Id));

        // FindTemplate CenterY -> Click Y
        workflow.AddConnection(new Connection(
            findNode.Id, findNode.OutputPorts[3].Id,
            clickNode.Id, clickNode.InputPorts[2].Id));

        // FindTemplate (NotFound) -> End (Not Found)
        workflow.AddConnection(new Connection(
            findNode.Id, findNode.OutputPorts[1].Id,
            endNode2.Id, endNode2.InputPorts[0].Id));

        // Click -> End (Success)
        workflow.AddConnection(new Connection(
            clickNode.Id, clickNode.OutputPorts[0].Id,
            endNode1.Id, endNode1.InputPorts[0].Id));

        return workflow;
    }
}
