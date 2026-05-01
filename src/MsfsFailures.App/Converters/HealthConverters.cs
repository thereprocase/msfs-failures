using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace MsfsFailures.App.Converters;

// Maps a health index [0..1] to red/orange/amber/green at 0.4/0.65/0.85.
public sealed class HealthBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var v = value is double d ? d : 0.0;
        var key = v < 0.4 ? "RedBrush"
                : v < 0.65 ? "OrangeBrush"
                : v < 0.85 ? "AmberBrush"
                : "GreenBrush";
        return Application.Current.Resources[key] ?? Brushes.White;
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

// Maps tone string ("good"/"warn"/"bad"/"amber"/"dim") to a brush.
public sealed class ToneBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = (value as string) switch
        {
            "warn"  or "amber" => "AmberBrush",
            "bad"   or "red"   => "RedBrush",
            "dim"              => "DimBrush",
            _                  => "GreenBrush",
        };
        return Application.Current.Resources[key] ?? Brushes.White;
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

// Severity strings for events: warn=amber, phase=cyan, wear=green, note=purple, default=dim.
public sealed class EventKindBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = (value as string) switch
        {
            "warn"  => "AmberBrush",
            "phase" => "CyanBrush",
            "wear"  => "GreenBrush",
            "note"  => "PurpleBrush",
            _       => "DimBrush",
        };
        return Application.Current.Resources[key] ?? Brushes.White;
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

// Latent precursor: <0.25 green, <0.6 amber, else red
public sealed class PrecursorBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var v = value is double d ? d : 0.0;
        var key = v < 0.25 ? "GreenBrush" : v < 0.6 ? "AmberBrush" : "RedBrush";
        return Application.Current.Resources[key] ?? Brushes.White;
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

// Multiplies a [0..1] value by 100 and rounds to int.
public sealed class PercentIntConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var v = value is double d ? d : 0.0;
        return ((int)Math.Round(v * 100)).ToString(CultureInfo.InvariantCulture);
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

// Position a marker on a 0..1 axis as Thickness.Left for left-aligning into a Canvas-like layout.
public sealed class PercentToThicknessConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var v = value is double d ? d : 0.0;
        return new Thickness(v * 100, 0, 0, 0);
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
