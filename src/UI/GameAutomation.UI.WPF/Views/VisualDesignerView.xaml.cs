using GameAutomation.UI.WPF.ViewModels;
using GameAutomation.Core.Workflows.Visual.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GameAutomation.UI.WPF.Views;

/// <summary>
/// Code-behind for the Visual Workflow Designer view.
/// Handles drag-drop, mouse interactions, connections, zoom/pan.
/// </summary>
public partial class VisualDesignerView : UserControl
{
    private bool _isDraggingNode;
    private Point _dragStartPoint;
    private NodeViewModel? _draggingNodeVm;

    // Connection dragging state
    private bool _isDraggingConnection;
    private NodeViewModel? _connectionSourceNode;
    private Port? _connectionSourcePort;
    private Line? _tempConnectionLine;

    // Zoom/pan state
    private double _zoomLevel = 1.0;
    private Point _panOffset;
    private bool _isPanning;
    private Point _panStart;

    public VisualDesignerView()
    {
        InitializeComponent();
    }

    private VisualDesignerViewModel? ViewModel => DataContext as VisualDesignerViewModel;

    #region Toolbox Drag

    private void ToolboxList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var listBox = sender as ListBox;
        var item = GetDataObjectFromPoint<ToolboxItemViewModel>(listBox, e.GetPosition(listBox));

        if (item != null)
        {
            var data = new DataObject("ToolboxItem", item);
            DragDrop.DoDragDrop(listBox, data, DragDropEffects.Copy);
        }
    }

    #endregion

    #region Canvas Drop

    private void WorkflowCanvas_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent("ToolboxItem"))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void WorkflowCanvas_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent("ToolboxItem"))
        {
            var item = e.Data.GetData("ToolboxItem") as ToolboxItemViewModel;
            if (item != null && ViewModel != null)
            {
                var position = e.GetPosition(WorkflowCanvas);
                ViewModel.AddNode(item.Type, position.X - 60, position.Y - 20);
            }
        }
    }

    private void WorkflowCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Deselect when clicking empty canvas
        if (ViewModel != null && e.OriginalSource == WorkflowCanvas)
        {
            foreach (var node in ViewModel.Nodes)
            {
                node.IsSelected = false;
            }
            ViewModel.SelectedNode = null;
        }

        // Cancel connection drag if clicking empty space
        if (_isDraggingConnection)
        {
            CancelConnectionDrag();
        }

        // Set focus for keyboard events
        Focus();
    }

    private void WorkflowCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(WorkflowCanvas);

        // Update temp connection line
        if (_isDraggingConnection)
        {
            UpdateConnectionDrag(pos);
        }

        // Pan with right mouse button
        if (_isPanning)
        {
            Canvas_MouseMove_Pan(e.GetPosition(this));
        }
    }

    private void WorkflowCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        // Cancel incomplete connection
        if (_isDraggingConnection)
        {
            CancelConnectionDrag();
        }
    }

    #endregion

    #region Node Drag

    private void Node_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var border = sender as FrameworkElement;
        var nodeVm = border?.DataContext as NodeViewModel;

        if (nodeVm != null && ViewModel != null)
        {
            // Select node
            foreach (var n in ViewModel.Nodes)
            {
                n.IsSelected = false;
            }
            nodeVm.IsSelected = true;
            ViewModel.SelectedNode = nodeVm;

            // Start drag
            _isDraggingNode = true;
            _dragStartPoint = e.GetPosition(WorkflowCanvas);
            _draggingNodeVm = nodeVm;
            border?.CaptureMouse();
        }

        e.Handled = true;
    }

    private void Node_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDraggingNode && _draggingNodeVm != null)
        {
            var currentPos = e.GetPosition(WorkflowCanvas);
            var deltaX = currentPos.X - _dragStartPoint.X;
            var deltaY = currentPos.Y - _dragStartPoint.Y;

            _draggingNodeVm.X += deltaX;
            _draggingNodeVm.Y += deltaY;

            // Prevent negative positions
            if (_draggingNodeVm.X < 0) _draggingNodeVm.X = 0;
            if (_draggingNodeVm.Y < 0) _draggingNodeVm.Y = 0;

            _dragStartPoint = currentPos;
        }
    }

    private void Node_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDraggingNode)
        {
            _isDraggingNode = false;
            _draggingNodeVm = null;
            (sender as FrameworkElement)?.ReleaseMouseCapture();
        }
    }

    #endregion

    #region Property Editor

    private void PropertyTextBox_Loaded(object sender, RoutedEventArgs e)
    {
        // Load current property value when textbox is created
        var textBox = sender as TextBox;
        var propertyName = textBox?.Tag as string;

        if (textBox != null && propertyName != null && ViewModel?.SelectedNode != null)
        {
            var value = ViewModel.SelectedNode.GetProperty(propertyName);
            textBox.Text = value?.ToString() ?? "";
        }
    }

    private void PropertyTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var textBox = sender as TextBox;
        var propertyName = textBox?.Tag as string;

        if (textBox != null && propertyName != null && ViewModel?.SelectedNode != null)
        {
            ViewModel.SelectedNode.SetProperty(propertyName, textBox.Text);
        }
    }

    #endregion

    #region Port Drag Events

    private void OutputPort_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var ellipse = sender as FrameworkElement;
        var port = ellipse?.Tag as Port;
        var nodeVm = FindNodeFromElement(ellipse);

        if (port != null && nodeVm != null)
        {
            var pos = e.GetPosition(WorkflowCanvas);
            StartConnectionDrag(nodeVm, port, pos);
            e.Handled = true;
        }
    }

    private void InputPort_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDraggingConnection) return;

        var ellipse = sender as FrameworkElement;
        var port = ellipse?.Tag as Port;
        var nodeVm = FindNodeFromElement(ellipse);

        if (port != null && nodeVm != null)
        {
            CompleteConnectionDrag(nodeVm, port);
            e.Handled = true;
        }
    }

    #endregion

    #region Connection Drag (Port to Port)

    /// <summary>
    /// Start connection drag from output port
    /// </summary>
    public void StartConnectionDrag(NodeViewModel sourceNode, Port sourcePort, Point startPoint)
    {
        if (sourcePort.Direction != PortDirection.Output) return;

        _isDraggingConnection = true;
        _connectionSourceNode = sourceNode;
        _connectionSourcePort = sourcePort;

        // Create temporary line
        _tempConnectionLine = new Line
        {
            Stroke = new SolidColorBrush(Color.FromRgb(102, 126, 234)), // #667eea
            StrokeThickness = 2,
            StrokeDashArray = new DoubleCollection { 4, 2 },
            X1 = sourceNode.X + 120, // Approximate right edge
            Y1 = sourceNode.Y + 30,
            X2 = startPoint.X,
            Y2 = startPoint.Y,
            IsHitTestVisible = false
        };
        WorkflowCanvas.Children.Add(_tempConnectionLine);
        WorkflowCanvas.CaptureMouse();
    }

    /// <summary>
    /// Update temp line during drag
    /// </summary>
    private void UpdateConnectionDrag(Point currentPoint)
    {
        if (_tempConnectionLine != null)
        {
            _tempConnectionLine.X2 = currentPoint.X;
            _tempConnectionLine.Y2 = currentPoint.Y;
        }
    }

    /// <summary>
    /// Complete connection on drop to input port
    /// </summary>
    public void CompleteConnectionDrag(NodeViewModel targetNode, Port targetPort)
    {
        if (_connectionSourceNode != null && _connectionSourcePort != null &&
            targetPort.Direction == PortDirection.Input)
        {
            ViewModel?.AddConnection(_connectionSourceNode, _connectionSourcePort, targetNode, targetPort);
        }
        CancelConnectionDrag();
    }

    /// <summary>
    /// Cancel connection drag
    /// </summary>
    private void CancelConnectionDrag()
    {
        _isDraggingConnection = false;
        _connectionSourceNode = null;
        _connectionSourcePort = null;

        if (_tempConnectionLine != null)
        {
            WorkflowCanvas.Children.Remove(_tempConnectionLine);
            _tempConnectionLine = null;
        }
        WorkflowCanvas.ReleaseMouseCapture();
    }

    #endregion

    #region Zoom and Pan

    private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Zoom with Ctrl+Scroll
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            var delta = e.Delta > 0 ? 0.1 : -0.1;
            _zoomLevel = Math.Clamp(_zoomLevel + delta, 0.25, 3.0);
            ApplyZoom();
            e.Handled = true;
        }
    }

    private void Canvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Start pan
        _isPanning = true;
        _panStart = e.GetPosition(this);
        WorkflowCanvas.CaptureMouse();
        e.Handled = true;
    }

    private void Canvas_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            WorkflowCanvas.ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    private void Canvas_MouseMove_Pan(Point currentPos)
    {
        if (_isPanning)
        {
            var delta = currentPos - _panStart;
            _panOffset.X += delta.X;
            _panOffset.Y += delta.Y;
            ApplyZoom();
            _panStart = currentPos;
        }
    }

    private void ApplyZoom()
    {
        var transform = new TransformGroup();
        transform.Children.Add(new ScaleTransform(_zoomLevel, _zoomLevel));
        transform.Children.Add(new TranslateTransform(_panOffset.X, _panOffset.Y));
        WorkflowCanvas.RenderTransform = transform;
    }

    /// <summary>
    /// Reset zoom to 100%
    /// </summary>
    public void ResetZoom()
    {
        _zoomLevel = 1.0;
        _panOffset = new Point(0, 0);
        ApplyZoom();
    }

    #endregion

    #region Delete and Keyboard

    private void UserControl_KeyDown(object sender, KeyEventArgs e)
    {
        if (ViewModel == null) return;

        // Delete selected node
        if (e.Key == Key.Delete && ViewModel.SelectedNode != null)
        {
            ViewModel.DeleteNode(ViewModel.SelectedNode);
            e.Handled = true;
        }
        // Ctrl+Z for undo (placeholder)
        else if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
        {
            // TODO: Implement undo
            e.Handled = true;
        }
        // Ctrl+Y for redo (placeholder)
        else if (e.Key == Key.Y && Keyboard.Modifiers == ModifierKeys.Control)
        {
            // TODO: Implement redo
            e.Handled = true;
        }
    }

    #endregion

    #region Helpers

    private static T? GetDataObjectFromPoint<T>(ItemsControl? itemsControl, Point point) where T : class
    {
        if (itemsControl == null) return null;

        var element = itemsControl.InputHitTest(point) as FrameworkElement;
        while (element != null)
        {
            if (element.DataContext is T data)
            {
                return data;
            }
            element = element.Parent as FrameworkElement;
        }
        return null;
    }

    /// <summary>
    /// Find NodeViewModel from a visual element
    /// </summary>
    private NodeViewModel? FindNodeFromElement(DependencyObject? element)
    {
        while (element != null)
        {
            if (element is FrameworkElement fe && fe.DataContext is NodeViewModel nvm)
                return nvm;
            element = VisualTreeHelper.GetParent(element);
        }
        return null;
    }

    #endregion
}
