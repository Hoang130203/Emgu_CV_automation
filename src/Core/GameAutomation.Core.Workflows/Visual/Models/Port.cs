using System.Text.Json.Serialization;

namespace GameAutomation.Core.Workflows.Visual.Models;

/// <summary>
/// Represents an input or output port on a node.
/// Ports are connection points for flow control and data transfer.
/// </summary>
public class Port
{
    public Port()
    {
        Id = Guid.NewGuid();
    }

    public Port(string name, PortType type, PortDirection direction, DataType dataType = DataType.Any)
        : this()
    {
        Name = name;
        Type = type;
        Direction = direction;
        DataType = dataType;
    }

    /// <summary>
    /// Unique identifier for this port
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Display name of the port
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Type of port (Flow or Data)
    /// </summary>
    public PortType Type { get; set; }

    /// <summary>
    /// Direction of the port (Input or Output)
    /// </summary>
    public PortDirection Direction { get; set; }

    /// <summary>
    /// Data type for Data ports
    /// </summary>
    public DataType DataType { get; set; } = DataType.Any;

    /// <summary>
    /// Whether this port allows multiple connections (only for output ports)
    /// </summary>
    public bool AllowMultiple { get; set; }

    /// <summary>
    /// Create a flow input port
    /// </summary>
    public static Port FlowInput(string name = "In") =>
        new(name, PortType.Flow, PortDirection.Input);

    /// <summary>
    /// Create a flow output port
    /// </summary>
    public static Port FlowOutput(string name = "Out") =>
        new(name, PortType.Flow, PortDirection.Output);

    /// <summary>
    /// Create a data input port
    /// </summary>
    public static Port DataInput(string name, DataType dataType = DataType.Any) =>
        new(name, PortType.Data, PortDirection.Input, dataType);

    /// <summary>
    /// Create a data output port
    /// </summary>
    public static Port DataOutput(string name, DataType dataType = DataType.Any) =>
        new(name, PortType.Data, PortDirection.Output, dataType);
}
