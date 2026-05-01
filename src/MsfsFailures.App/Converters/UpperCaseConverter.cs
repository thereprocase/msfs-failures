using System.Globalization;
using System.Windows.Data;

namespace MsfsFailures.App.Converters;

public sealed class UpperCaseConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value as string ?? string.Empty).ToUpperInvariant();

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
