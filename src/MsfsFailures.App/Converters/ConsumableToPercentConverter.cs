using System.Globalization;
using System.Windows.Data;

namespace MsfsFailures.App.Converters;

public sealed class ConsumableToPercentConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var v = value is double d ? d : 0.0;
        return ((int)Math.Round(v * 100)).ToString(CultureInfo.InvariantCulture);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
