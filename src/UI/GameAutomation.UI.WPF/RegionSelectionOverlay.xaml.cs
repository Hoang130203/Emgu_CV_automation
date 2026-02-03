using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GameAutomation.Core.Models.Vision;

namespace GameAutomation.UI.WPF;

public partial class RegionSelectionOverlay : Window
{
    private Point _startPoint;
    private bool _isDragging;

    public event EventHandler<SearchRegion>? RegionSelected;

    public RegionSelectionOverlay()
    {
        InitializeComponent();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _startPoint = e.GetPosition(this);
        _isDragging = true;

        SelectionRect.Visibility = Visibility.Visible;
        InfoPanel.Visibility = Visibility.Visible;

        Canvas.SetLeft(SelectionRect, _startPoint.X);
        Canvas.SetTop(SelectionRect, _startPoint.Y);
        SelectionRect.Width = 0;
        SelectionRect.Height = 0;

        CaptureMouse();
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging)
            return;

        var currentPoint = e.GetPosition(this);
        UpdateSelectionRect(currentPoint);
        UpdateInfoDisplay(currentPoint);
    }

    private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging)
            return;

        _isDragging = false;
        ReleaseMouseCapture();

        var currentPoint = e.GetPosition(this);
        var region = CalculateRegion(currentPoint);

        // Only accept if region has meaningful size
        if (SelectionRect.Width > 10 && SelectionRect.Height > 10)
        {
            RegionSelected?.Invoke(this, region);
        }

        Close();
    }

    private void UpdateSelectionRect(Point currentPoint)
    {
        var x = Math.Min(_startPoint.X, currentPoint.X);
        var y = Math.Min(_startPoint.Y, currentPoint.Y);
        var width = Math.Abs(currentPoint.X - _startPoint.X);
        var height = Math.Abs(currentPoint.Y - _startPoint.Y);

        Canvas.SetLeft(SelectionRect, x);
        Canvas.SetTop(SelectionRect, y);
        SelectionRect.Width = width;
        SelectionRect.Height = height;
    }

    private void UpdateInfoDisplay(Point currentPoint)
    {
        var screenWidth = ActualWidth;
        var screenHeight = ActualHeight;

        var x1 = Math.Min(_startPoint.X, currentPoint.X);
        var y1 = Math.Min(_startPoint.Y, currentPoint.Y);
        var x2 = Math.Max(_startPoint.X, currentPoint.X);
        var y2 = Math.Max(_startPoint.Y, currentPoint.Y);

        // Pixel coordinates
        PixelInfo.Text = $"Pixels: ({x1:F0}, {y1:F0}) → ({x2:F0}, {y2:F0})  |  Size: {x2 - x1:F0} x {y2 - y1:F0}";

        // Ratio coordinates
        var startXRatio = x1 / screenWidth;
        var startYRatio = y1 / screenHeight;
        var endXRatio = x2 / screenWidth;
        var endYRatio = y2 / screenHeight;

        RatioInfo.Text = $"Ratios: ({startXRatio:F3}, {startYRatio:F3}) → ({endXRatio:F3}, {endYRatio:F3})";
    }

    private SearchRegion CalculateRegion(Point currentPoint)
    {
        var screenWidth = ActualWidth;
        var screenHeight = ActualHeight;

        var x1 = Math.Min(_startPoint.X, currentPoint.X);
        var y1 = Math.Min(_startPoint.Y, currentPoint.Y);
        var x2 = Math.Max(_startPoint.X, currentPoint.X);
        var y2 = Math.Max(_startPoint.Y, currentPoint.Y);

        return new SearchRegion(
            x1 / screenWidth,
            y1 / screenHeight,
            x2 / screenWidth,
            y2 / screenHeight
        );
    }
}
