using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GameAutomation.UI.WPF.Converters;

/// <summary>
/// Converts null to Visibility.Visible (shows placeholder when null)
/// </summary>
public class NullToVisibleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value == null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts null to Visibility.Collapsed (hides when null)
/// </summary>
public class NullToCollapsedConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value == null ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts count to Visibility (Visible when 0, Collapsed otherwise)
/// Used for empty state display
/// </summary>
public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean to Visibility
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return b ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Visibility v)
        {
            return v == Visibility.Visible;
        }
        return false;
    }
}

/// <summary>
/// Inverted boolean to Visibility
/// </summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return b ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a node to a Point for connection endpoints
/// Parameter: "Input" for input port center, "Output" for output port center
/// </summary>
public class NodeToPointConverter : IValueConverter
{
    private const double NodeWidth = 120;
    private const double NodeHeaderHeight = 32;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double x)
        {
            // For X coordinate binding, return a dummy point - actual binding needs multi-value
            return new System.Windows.Point(x, 0);
        }
        return new System.Windows.Point(0, 0);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts source node to bezier control point 1 (curves out to the right)
/// </summary>
public class NodeToBezierPoint1Converter : IValueConverter
{
    private const double ControlPointOffset = 80;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Return a default point - actual implementation needs multi-binding
        return new System.Windows.Point(0, 0);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts target node to bezier control point 2 (curves in from the left)
/// </summary>
public class NodeToBezierPoint2Converter : IValueConverter
{
    private const double ControlPointOffset = 80;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Return a default point - actual implementation needs multi-binding
        return new System.Windows.Point(0, 0);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
