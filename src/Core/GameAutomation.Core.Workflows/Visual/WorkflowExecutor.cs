using GameAutomation.Core.Workflows.Visual.Models;

namespace GameAutomation.Core.Workflows.Visual;

/// <summary>
/// Executes visual workflows by traversing the node graph.
/// </summary>
public class WorkflowExecutor
{
    private readonly ActivityRegistry _registry;

    public WorkflowExecutor(ActivityRegistry? registry = null)
    {
        _registry = registry ?? new ActivityRegistry();
    }

    /// <summary>
    /// Event raised when a node starts executing
    /// </summary>
    public event EventHandler<NodeExecutionEventArgs>? NodeExecuting;

    /// <summary>
    /// Event raised when a node finishes executing
    /// </summary>
    public event EventHandler<NodeExecutionEventArgs>? NodeExecuted;

    /// <summary>
    /// Event raised when workflow execution completes
    /// </summary>
    public event EventHandler<WorkflowExecutionResult>? WorkflowCompleted;

    /// <summary>
    /// Execute a workflow
    /// </summary>
    public async Task<WorkflowExecutionResult> ExecuteAsync(
        VisualWorkflow workflow,
        IActivityContext context)
    {
        var result = new WorkflowExecutionResult
        {
            WorkflowId = workflow.Id,
            WorkflowName = workflow.Name,
            StartedAt = DateTime.UtcNow
        };

        try
        {
            // Validate workflow
            var (isValid, errors) = workflow.Validate();
            if (!isValid)
            {
                result.Success = false;
                result.ErrorMessage = string.Join("; ", errors);
                return result;
            }

            // Find start node
            var startNode = workflow.FindStartNode();
            if (startNode == null)
            {
                result.Success = false;
                result.ErrorMessage = "No Start node found";
                return result;
            }

            // Execute from start node
            var currentNode = startNode;
            string? nextPortName = null;

            while (currentNode != null && !context.CancellationToken.IsCancellationRequested)
            {
                // Skip disabled nodes
                if (currentNode.IsDisabled)
                {
                    currentNode = GetNextNode(workflow, currentNode, "Out");
                    continue;
                }

                // Get activity for this node type
                var activity = _registry.Get(currentNode.ActivityType);
                if (activity == null)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Unknown activity type: {currentNode.ActivityType}";
                    return result;
                }

                // Raise executing event
                OnNodeExecuting(currentNode, activity);

                // Gather input values from connected data ports
                var inputValues = GatherInputValues(workflow, currentNode, context);

                // Execute the activity
                var activityResult = await activity.ExecuteAsync(context, currentNode, inputValues);
                result.NodesExecuted++;

                // Raise executed event
                OnNodeExecuted(currentNode, activity, activityResult);

                // Handle result
                if (!activityResult.Success)
                {
                    result.Success = false;
                    result.ErrorMessage = activityResult.ErrorMessage;
                    result.FailedNodeId = currentNode.Id;
                    return result;
                }

                // Store output values in context for data port connections
                StoreOutputValues(currentNode, activityResult, context);

                // Check for stop
                if (activityResult.ShouldStop)
                {
                    break;
                }

                // Get next node
                nextPortName = activityResult.NextPortName ?? "Out";
                currentNode = GetNextNode(workflow, currentNode, nextPortName);
            }

            // Check if cancelled
            if (context.CancellationToken.IsCancellationRequested)
            {
                result.Success = false;
                result.ErrorMessage = "Workflow execution was cancelled";
                return result;
            }

            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Workflow execution failed: {ex.Message}";
        }
        finally
        {
            result.CompletedAt = DateTime.UtcNow;
            WorkflowCompleted?.Invoke(this, result);
        }

        return result;
    }

    private Dictionary<string, object?> GatherInputValues(
        VisualWorkflow workflow,
        ActivityNode node,
        IActivityContext context)
    {
        var values = new Dictionary<string, object?>();

        foreach (var inputPort in node.InputPorts.Where(p => p.Type == PortType.Data))
        {
            // Find connection to this input port
            var connection = workflow.GetConnectionToPort(node.Id, inputPort.Id);
            if (connection == null) continue;

            // Get the source node and port
            var sourceNode = workflow.FindNode(connection.SourceNodeId);
            if (sourceNode == null) continue;

            // Get output value from context
            var outputKey = $"{sourceNode.Id}:{connection.SourcePortId}";
            if (context.Variables.TryGetValue(outputKey, out var value))
            {
                values[inputPort.Name] = value;
            }
        }

        return values;
    }

    private void StoreOutputValues(
        ActivityNode node,
        ActivityResult result,
        IActivityContext context)
    {
        foreach (var outputPort in node.OutputPorts.Where(p => p.Type == PortType.Data))
        {
            if (result.OutputValues.TryGetValue(outputPort.Name, out var value))
            {
                var outputKey = $"{node.Id}:{outputPort.Id}";
                context.Variables[outputKey] = value;
            }
        }
    }

    private ActivityNode? GetNextNode(
        VisualWorkflow workflow,
        ActivityNode currentNode,
        string portName)
    {
        // Find the output port
        var outputPort = currentNode.FindPort(portName, PortDirection.Output);
        if (outputPort == null)
        {
            // Try default "Out" port if specified port not found
            outputPort = currentNode.FindPort("Out", PortDirection.Output);
            if (outputPort == null) return null;
        }

        // Find connection from this port
        var connections = workflow.GetConnectionsFromPort(currentNode.Id, outputPort.Id);
        var connection = connections.FirstOrDefault();
        if (connection == null) return null;

        // Return target node
        return workflow.FindNode(connection.TargetNodeId);
    }

    private void OnNodeExecuting(ActivityNode node, IActivity activity)
    {
        NodeExecuting?.Invoke(this, new NodeExecutionEventArgs
        {
            Node = node,
            Activity = activity
        });
    }

    private void OnNodeExecuted(ActivityNode node, IActivity activity, ActivityResult result)
    {
        NodeExecuted?.Invoke(this, new NodeExecutionEventArgs
        {
            Node = node,
            Activity = activity,
            Result = result
        });
    }
}

/// <summary>
/// Event args for node execution events
/// </summary>
public class NodeExecutionEventArgs : EventArgs
{
    public required ActivityNode Node { get; init; }
    public required IActivity Activity { get; init; }
    public ActivityResult? Result { get; init; }
}

/// <summary>
/// Result of workflow execution
/// </summary>
public class WorkflowExecutionResult
{
    public Guid WorkflowId { get; set; }
    public string WorkflowName { get; set; } = "";
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? FailedNodeId { get; set; }
    public int NodesExecuted { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public TimeSpan Duration => CompletedAt - StartedAt;
}
