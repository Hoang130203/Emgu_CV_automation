using GameAutomation.UI.WPF.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GameAutomation.UI.WPF.Views;

/// <summary>
/// Code-behind for the Visual Workflow Designer view.
/// Handles drag-drop and mouse interactions.
/// </summary>
public partial class VisualDesignerView : UserControl
{
    private bool _isDraggingNode;
    private Point _dragStartPoint;
    private NodeViewModel? _draggingNodeVm;

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

    #endregion
}
