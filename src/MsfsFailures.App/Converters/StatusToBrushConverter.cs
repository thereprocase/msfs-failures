using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using MsfsFailures.App.ViewModels;

namespace MsfsFailures.App.Converters;

public sealed class StatusToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not AirframeStatus s) return Brushes.White;
        var key = s switch
        {
            AirframeStatus.Airworthy => "GreenBrush",
            AirframeStatus.Squawks   => "AmberBrush",
            AirframeStatus.MxDue     => "OrangeBrush",
            AirframeStatus.Grounded  => "RedBrush",
            _ => "TextBrush",
        };
        return Application.Current.Resources[key] ?? Brushes.White;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class SeverityToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not SquawkSeverity s) return Brushes.White;
        var key = s switch
        {
            SquawkSeverity.Grounding => "RedBrush",
            SquawkSeverity.Open      => "OrangeBrush",
            SquawkSeverity.Deferred  => "DimBrush",
            _ => "TextBrush",
        };
        return Application.Current.Resources[key] ?? Brushes.White;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
