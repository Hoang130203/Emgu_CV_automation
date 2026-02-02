namespace GameAutomation.Core.Workflows.Visual.Models;

/// <summary>
/// Represents a complete visual workflow that can be saved, loaded, and executed.
/// Contains nodes (activities) and connections between them.
/// </summary>
public class VisualWorkflow
{
    public VisualWorkflow()
    {
        Id = Guid.NewGuid();
        Nodes = new List<ActivityNode>();
        Connections = new List<Connection>();
        CreatedAt = DateTime.UtcNow;
        ModifiedAt = DateTime.UtcNow;
    }

    public VisualWorkflow(string name) : this()
    {
        Name = name;
    }

    /// <summary>
    /// Unique identifier for this workflow
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name of the workflow
    /// </summary>
    public string Name { get; set; } = "New Workflow";

    /// <summary>
    /// Description of what the workflow does
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// Version string for tracking changes
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Author of the workflow
    /// </summary>
    public string Author { get; set; } = "";

    /// <summary>
    /// All activity nodes in the workflow
    /// </summary>
    public List<ActivityNode> Nodes { get; set; }

    /// <summary>
    /// All connections between nodes
    /// </summary>
    public List<Connection> Connections { get; set; }

    /// <summary>
    /// When this workflow was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this workflow was last modified
    /// </summary>
    public DateTime ModifiedAt { get; set; }

    /// <summary>
    /// Find node by ID
    /// </summary>
    public ActivityNode? FindNode(Guid nodeId)
    {
        return Nodes.FirstOrDefault(n => n.Id == nodeId);
    }

    /// <summary>
    /// Find the Start node
    /// </summary>
    public ActivityNode? FindStartNode()
    {
        return Nodes.FirstOrDefault(n => n.ActivityType == ActivityTypes.Start);
    }

    /// <summary>
    /// Get all connections from a specific output port
    /// </summary>
    public IEnumerable<Connection> GetConnectionsFromPort(Guid nodeId, Guid portId)
    {
        return Connections.Where(c => c.SourceNodeId == nodeId && c.SourcePortId == portId);
    }

    /// <summary>
    /// Get the connection to a specific input port (should be only one)
    /// </summary>
    public Connection? GetConnectionToPort(Guid nodeId, Guid portId)
    {
        return Connections.FirstOrDefault(c => c.TargetNodeId == nodeId && c.TargetPortId == portId);
    }

    /// <summary>
    /// Add a node to the workflow
    /// </summary>
    public void AddNode(ActivityNode node)
    {
        Nodes.Add(node);
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Remove a node and all its connections
    /// </summary>
    public void RemoveNode(Guid nodeId)
    {
        Nodes.RemoveAll(n => n.Id == nodeId);
        Connections.RemoveAll(c => c.SourceNodeId == nodeId || c.TargetNodeId == nodeId);
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Add a connection between two ports
    /// </summary>
    public void AddConnection(Connection connection)
    {
        Connections.Add(connection);
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Remove a connection
    /// </summary>
    public void RemoveConnection(Guid connectionId)
    {
        Connections.RemoveAll(c => c.Id == connectionId);
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Validate workflow structure
    /// </summary>
    public (bool IsValid, List<string> Errors) Validate()
    {
        var errors = new List<string>();

        // Check for Start node
        var startNodes = Nodes.Count(n => n.ActivityType == ActivityTypes.Start);
        if (startNodes == 0)
            errors.Add("Workflow must have a Start node");
        else if (startNodes > 1)
            errors.Add("Workflow can only have one Start node");

        // Check for End node
        var endNodes = Nodes.Count(n => n.ActivityType == ActivityTypes.End);
        if (endNodes == 0)
            errors.Add("Workflow must have at least one End node");

        // Check for orphan nodes (no connections)
        foreach (var node in Nodes.Where(n => n.ActivityType != ActivityTypes.Start))
        {
            var hasIncoming = Connections.Any(c => c.TargetNodeId == node.Id);
            if (!hasIncoming)
                errors.Add($"Node '{node.DisplayName}' has no incoming connections");
        }

        return (errors.Count == 0, errors);
    }
}
