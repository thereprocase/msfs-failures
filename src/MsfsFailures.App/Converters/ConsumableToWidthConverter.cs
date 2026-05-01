using System.Globalization;
using System.Windows.Data;

namespace MsfsFailures.App.Converters;

// Multiplies a [0..1] value by parameter (or 36 if no parameter).
public sealed class ConsumableToWidthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var v = value is double d ? d : 0.0;
        var max = 36.0;
        if (parameter is string s && double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var p)) max = p;
        else if (parameter is double pd) max = pd;
        return Math.Clamp(v, 0.0, 1.0) * max;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
