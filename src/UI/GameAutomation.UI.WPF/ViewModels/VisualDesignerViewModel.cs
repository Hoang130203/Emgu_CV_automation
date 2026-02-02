using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameAutomation.Core.Services.Input;
using GameAutomation.Core.Services.Vision;
using GameAutomation.Core.Workflows.Visual;
using GameAutomation.Core.Workflows.Visual.Models;
using GameAutomation.Core.Workflows.Visual.Serialization;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace GameAutomation.UI.WPF.ViewModels;

/// <summary>
/// ViewModel for the Visual Workflow Designer
/// </summary>
public partial class VisualDesignerViewModel : ObservableObject
{
    private readonly ActivityRegistry _registry;
    private readonly WorkflowSerializer _serializer;
    private readonly WorkflowExecutor _executor;
    private CancellationTokenSource? _executionCts;

    [ObservableProperty]
    private VisualWorkflow _currentWorkflow;

    [ObservableProperty]
    private NodeViewModel? _selectedNode;

    [ObservableProperty]
    private bool _isExecuting;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private string _executionLog = "";

    [ObservableProperty]
    private Guid? _executingNodeId;

    public ObservableCollection<NodeViewModel> Nodes { get; } = new();
    public ObservableCollection<ConnectionViewModel> Connections { get; } = new();
    public ObservableCollection<ToolboxItemViewModel> ToolboxItems { get; } = new();

    public VisualDesignerViewModel()
    {
        _registry = new ActivityRegistry();
        _serializer = new WorkflowSerializer();
        _executor = new WorkflowExecutor(_registry);
        _currentWorkflow = new VisualWorkflow("New Workflow");

        // Subscribe to executor events
        _executor.NodeExecuting += OnNodeExecuting;
        _executor.NodeExecuted += OnNodeExecuted;
        _executor.WorkflowCompleted += OnWorkflowCompleted;

        // Populate toolbox
        PopulateToolbox();

        // Create default workflow with Start and End
        CreateDefaultWorkflow();
    }

    private void PopulateToolbox()
    {
        foreach (var category in _registry.GetCategories())
        {
            foreach (var activity in _registry.GetByCategory(category))
            {
                ToolboxItems.Add(new ToolboxItemViewModel
                {
                    Type = activity.Type,
                    DisplayName = activity.DisplayName,
                    Category = activity.Category,
                    Description = activity.Description,
                    Icon = activity.Icon
                });
            }
        }
    }

    private void CreateDefaultWorkflow()
    {
        // Create Start node
        var startActivity = _registry.Get(ActivityTypes.Start)!;
        var startNode = new ActivityNode(ActivityTypes.Start, "Start")
        {
            X = 100,
            Y = 200
        };
        foreach (var port in startActivity.GetInputPorts())
            startNode.InputPorts.Add(new Port(port.Name, port.Type, port.Direction, port.DataType) { Id = Guid.NewGuid() });
        foreach (var port in startActivity.GetOutputPorts())
            startNode.OutputPorts.Add(new Port(port.Name, port.Type, port.Direction, port.DataType) { Id = Guid.NewGuid() });

        // Create End node
        var endActivity = _registry.Get(ActivityTypes.End)!;
        var endNode = new ActivityNode(ActivityTypes.End, "End")
        {
            X = 500,
            Y = 200
        };
        foreach (var port in endActivity.GetInputPorts())
            endNode.InputPorts.Add(new Port(port.Name, port.Type, port.Direction, port.DataType) { Id = Guid.NewGuid() });
        foreach (var port in endActivity.GetOutputPorts())
            endNode.OutputPorts.Add(new Port(port.Name, port.Type, port.Direction, port.DataType) { Id = Guid.NewGuid() });

        _currentWorkflow.AddNode(startNode);
        _currentWorkflow.AddNode(endNode);

        // Add connection
        var connection = new Connection(
            startNode.Id, startNode.OutputPorts[0].Id,
            endNode.Id, endNode.InputPorts[0].Id);
        _currentWorkflow.AddConnection(connection);

        RefreshFromWorkflow();
    }

    private void RefreshFromWorkflow()
    {
        Nodes.Clear();
        Connections.Clear();

        foreach (var node in _currentWorkflow.Nodes)
        {
            var activity = _registry.Get(node.ActivityType);
            Nodes.Add(new NodeViewModel(node, activity));
        }

        foreach (var conn in _currentWorkflow.Connections)
        {
            var sourceNode = Nodes.FirstOrDefault(n => n.Node.Id == conn.SourceNodeId);
            var targetNode = Nodes.FirstOrDefault(n => n.Node.Id == conn.TargetNodeId);
            if (sourceNode != null && targetNode != null)
            {
                Connections.Add(new ConnectionViewModel(conn, sourceNode, targetNode));
            }
        }
    }

    [RelayCommand]
    private void NewWorkflow()
    {
        if (IsExecuting) return;

        _currentWorkflow = new VisualWorkflow("New Workflow");
        SelectedNode = null;
        CreateDefaultWorkflow();
        StatusMessage = "New workflow created";
    }

    [RelayCommand]
    private async Task OpenWorkflow()
    {
        if (IsExecuting) return;

        var dialog = new OpenFileDialog
        {
            Filter = "Workflow Files (*.workflow.json)|*.workflow.json|All Files (*.*)|*.*",
            DefaultExt = ".workflow.json"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var workflow = await _serializer.LoadFromFileAsync(dialog.FileName);
                if (workflow != null)
                {
                    _currentWorkflow = workflow;
                    CurrentWorkflow = workflow;
                    RefreshFromWorkflow();
                    StatusMessage = $"Opened: {Path.GetFileName(dialog.FileName)}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open workflow: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    private async Task SaveWorkflow()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Workflow Files (*.workflow.json)|*.workflow.json",
            DefaultExt = ".workflow.json",
            FileName = $"{_currentWorkflow.Name}.workflow.json"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _serializer.SaveToFileAsync(_currentWorkflow, dialog.FileName);
                StatusMessage = $"Saved: {Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save workflow: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    private async Task RunWorkflow()
    {
        if (IsExecuting)
        {
            StopWorkflow();
            return;
        }

        // Validate workflow
        var (isValid, errors) = _currentWorkflow.Validate();
        if (!isValid)
        {
            MessageBox.Show($"Workflow validation failed:\n{string.Join("\n", errors)}",
                "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsExecuting = true;
        ExecutionLog = "";
        StatusMessage = "Running...";
        _executionCts = new CancellationTokenSource();

        try
        {
            var visionService = new VisionService();
            var inputService = new InputService();
            var context = new ActivityContext(
                visionService,
                inputService,
                _executionCts.Token,
                msg => Application.Current?.Dispatcher.Invoke(() =>
                {
                    ExecutionLog += $"[{DateTime.Now:HH:mm:ss}] {msg}\n";
                }),
                msg => Application.Current?.Dispatcher.Invoke(() =>
                {
                    ExecutionLog += $"[{DateTime.Now:HH:mm:ss}] ERROR: {msg}\n";
                }));

            var result = await Task.Run(() => _executor.ExecuteAsync(_currentWorkflow, context));

            StatusMessage = result.Success
                ? $"Completed in {result.Duration.TotalSeconds:F1}s"
                : $"Failed: {result.ErrorMessage}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsExecuting = false;
            ExecutingNodeId = null;
            _executionCts?.Dispose();
            _executionCts = null;
        }
    }

    [RelayCommand]
    private void StopWorkflow()
    {
        _executionCts?.Cancel();
        StatusMessage = "Stopping...";
    }

    public void AddNode(string activityType, double x, double y)
    {
        var activity = _registry.Get(activityType);
        if (activity == null) return;

        var node = new ActivityNode(activityType, activity.DisplayName)
        {
            X = x,
            Y = y
        };

        // Add ports from activity definition
        foreach (var port in activity.GetInputPorts())
        {
            node.InputPorts.Add(new Port(port.Name, port.Type, port.Direction, port.DataType) { Id = Guid.NewGuid() });
        }
        foreach (var port in activity.GetOutputPorts())
        {
            node.OutputPorts.Add(new Port(port.Name, port.Type, port.Direction, port.DataType) { Id = Guid.NewGuid() });
        }

        // Set default property values
        foreach (var propDef in activity.GetPropertyDefinitions())
        {
            if (propDef.DefaultValue != null)
            {
                node.Properties[propDef.Name] = propDef.DefaultValue;
            }
        }

        _currentWorkflow.AddNode(node);
        Nodes.Add(new NodeViewModel(node, activity));
        StatusMessage = $"Added {activity.DisplayName}";
    }

    public void DeleteNode(NodeViewModel nodeVm)
    {
        if (nodeVm.Node.ActivityType == ActivityTypes.Start)
        {
            MessageBox.Show("Cannot delete Start node", "Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _currentWorkflow.RemoveNode(nodeVm.Node.Id);

        // Remove associated connections
        var connectionsToRemove = Connections
            .Where(c => c.Connection.SourceNodeId == nodeVm.Node.Id ||
                       c.Connection.TargetNodeId == nodeVm.Node.Id)
            .ToList();

        foreach (var conn in connectionsToRemove)
        {
            Connections.Remove(conn);
        }

        Nodes.Remove(nodeVm);

        if (SelectedNode == nodeVm)
            SelectedNode = null;

        StatusMessage = $"Deleted {nodeVm.DisplayName}";
    }

    public void AddConnection(NodeViewModel sourceNode, Port sourcePort,
                             NodeViewModel targetNode, Port targetPort)
    {
        // Validate connection
        if (sourcePort.Direction != PortDirection.Output ||
            targetPort.Direction != PortDirection.Input)
        {
            return;
        }

        // Check for existing connection to this input
        var existingConn = Connections.FirstOrDefault(c =>
            c.Connection.TargetNodeId == targetNode.Node.Id &&
            c.Connection.TargetPortId == targetPort.Id);

        if (existingConn != null)
        {
            // Remove existing connection
            _currentWorkflow.RemoveConnection(existingConn.Connection.Id);
            Connections.Remove(existingConn);
        }

        var connection = new Connection(
            sourceNode.Node.Id, sourcePort.Id,
            targetNode.Node.Id, targetPort.Id);

        _currentWorkflow.AddConnection(connection);
        Connections.Add(new ConnectionViewModel(connection, sourceNode, targetNode));
        StatusMessage = "Connection added";
    }

    public void RemoveConnection(ConnectionViewModel connVm)
    {
        _currentWorkflow.RemoveConnection(connVm.Connection.Id);
        Connections.Remove(connVm);
        StatusMessage = "Connection removed";
    }

    private void OnNodeExecuting(object? sender, NodeExecutionEventArgs e)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            ExecutingNodeId = e.Node.Id;
            var nodeVm = Nodes.FirstOrDefault(n => n.Node.Id == e.Node.Id);
            if (nodeVm != null)
            {
                nodeVm.IsExecuting = true;
            }
        });
    }

    private void OnNodeExecuted(object? sender, NodeExecutionEventArgs e)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            var nodeVm = Nodes.FirstOrDefault(n => n.Node.Id == e.Node.Id);
            if (nodeVm != null)
            {
                nodeVm.IsExecuting = false;
                nodeVm.LastResult = e.Result?.Success == true ? "Success" : "Failed";
            }
        });
    }

    private void OnWorkflowCompleted(object? sender, WorkflowExecutionResult e)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            ExecutingNodeId = null;
            foreach (var node in Nodes)
            {
                node.IsExecuting = false;
            }
        });
    }
}

/// <summary>
/// ViewModel for a node on the canvas
/// </summary>
public partial class NodeViewModel : ObservableObject
{
    public ActivityNode Node { get; }
    public IActivity? Activity { get; }

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isExecuting;

    [ObservableProperty]
    private string? _lastResult;

    public string DisplayName => Node.DisplayName;
    public string ActivityType => Node.ActivityType;
    public string Icon => Activity?.Icon ?? "Help";
    public double X
    {
        get => Node.X;
        set { Node.X = value; OnPropertyChanged(); }
    }
    public double Y
    {
        get => Node.Y;
        set { Node.Y = value; OnPropertyChanged(); }
    }

    public IReadOnlyList<Port> InputPorts => Node.InputPorts;
    public IReadOnlyList<Port> OutputPorts => Node.OutputPorts;
    public IReadOnlyList<ActivityPropertyDefinition> PropertyDefinitions =>
        Activity?.GetPropertyDefinitions() ?? Array.Empty<ActivityPropertyDefinition>();

    public NodeViewModel(ActivityNode node, IActivity? activity)
    {
        Node = node;
        Activity = activity;
    }

    public object? GetProperty(string name)
    {
        return Node.Properties.TryGetValue(name, out var value) ? value : null;
    }

    public void SetProperty(string name, object? value)
    {
        Node.Properties[name] = value;
        OnPropertyChanged(nameof(Node));
    }
}

/// <summary>
/// ViewModel for a connection between nodes
/// </summary>
public partial class ConnectionViewModel : ObservableObject
{
    public Connection Connection { get; }
    public NodeViewModel SourceNode { get; }
    public NodeViewModel TargetNode { get; }

    public ConnectionViewModel(Connection connection, NodeViewModel sourceNode, NodeViewModel targetNode)
    {
        Connection = connection;
        SourceNode = sourceNode;
        TargetNode = targetNode;
    }
}

/// <summary>
/// ViewModel for toolbox items
/// </summary>
public class ToolboxItemViewModel
{
    public required string Type { get; init; }
    public required string DisplayName { get; init; }
    public required string Category { get; init; }
    public required string Description { get; init; }
    public required string Icon { get; init; }
}
