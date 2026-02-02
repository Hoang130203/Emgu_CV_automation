namespace GameAutomation.Core.Workflows.Visual.Models;

/// <summary>
/// Represents a connection between two ports on different nodes.
/// Connections define the flow and data transfer between activities.
/// </summary>
public class Connection
{
    public Connection()
    {
        Id = Guid.NewGuid();
    }

    public Connection(Guid sourceNodeId, Guid sourcePortId, Guid targetNodeId, Guid targetPortId)
        : this()
    {
        SourceNodeId = sourceNodeId;
        SourcePortId = sourcePortId;
        TargetNodeId = targetNodeId;
        TargetPortId = targetPortId;
    }

    /// <summary>
    /// Unique identifier for this connection
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID of the node where this connection starts
    /// </summary>
    public Guid SourceNodeId { get; set; }

    /// <summary>
    /// ID of the output port on the source node
    /// </summary>
    public Guid SourcePortId { get; set; }

    /// <summary>
    /// ID of the node where this connection ends
    /// </summary>
    public Guid TargetNodeId { get; set; }

    /// <summary>
    /// ID of the input port on the target node
    /// </summary>
    public Guid TargetPortId { get; set; }
}
