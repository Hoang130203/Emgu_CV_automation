namespace GameAutomation.Core.Workflows.Visual.Models;

/// <summary>
/// Types of ports for node connections
/// </summary>
public enum PortType
{
    /// <summary>
    /// Flow port - controls execution order
    /// </summary>
    Flow,

    /// <summary>
    /// Data port - transfers data between nodes
    /// </summary>
    Data
}

/// <summary>
/// Direction of a port
/// </summary>
public enum PortDirection
{
    Input,
    Output
}

/// <summary>
/// Data types for data ports
/// </summary>
public enum DataType
{
    Any,
    Boolean,
    String,
    Number,
    Point,
    Rectangle,
    Image
}

/// <summary>
/// Built-in activity types
/// </summary>
public static class ActivityTypes
{
    public const string Start = "Start";
    public const string End = "End";
    public const string FindTemplate = "FindTemplate";
    public const string Click = "Click";
    public const string TypeText = "TypeText";
    public const string Wait = "Wait";
    public const string Condition = "Condition";
    public const string Loop = "Loop";
    public const string ForEach = "ForEach";
    public const string While = "While";
    public const string OCR = "OCR";
    public const string HttpRequest = "HttpRequest";
    public const string SetVariable = "SetVariable";
    public const string GetVariable = "GetVariable";
    public const string SubWorkflow = "SubWorkflow";
}
