using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace MsfsFailures.App.Converters;

// Returns OrangeBrush if the value is a bool true, else TextBrush.
public sealed class AlertBoolToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var alert = value is bool b && b;
        var key = alert ? "OrangeBrush" : "TextBrush";
        return Application.Current.Resources[key] ?? Brushes.White;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class SquawkCountToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var n = value is int i ? i : 0;
        var key = n > 0 ? "OrangeBrush" : "FaintBrush";
        return Application.Current.Resources[key] ?? Brushes.White;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class MelToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var ok = value is bool b && b;
        var key = ok ? "DimBrush" : "RedBrush";
        return Application.Current.Resources[key] ?? Brushes.White;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class IntZeroBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var n = System.Convert.ToDouble(value, culture);
        var threshold = 0.0;
        if (parameter is string s && double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var t)) threshold = t;
        var key = n > threshold ? "OrangeBrush" : "TextBrush";
        return Application.Current.Resources[key] ?? Brushes.White;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
