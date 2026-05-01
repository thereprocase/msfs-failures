using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace MsfsFailures.App.Converters;

// Maps a consumable level [0..1] to red/orange/amber/green at 25/50/75 thresholds.
public sealed class ConsumableToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var v = value switch
        {
            double d => d,
            float f  => f,
            decimal m => (double)m,
            _ => 0.0,
        };
        var key = v < 0.25 ? "RedBrush"
                : v < 0.5  ? "OrangeBrush"
                : v < 0.75 ? "AmberBrush"
                : "GreenBrush";
        return Application.Current.Resources[key] ?? Brushes.White;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
