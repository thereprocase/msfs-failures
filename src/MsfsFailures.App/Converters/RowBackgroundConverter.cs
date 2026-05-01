using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace MsfsFailures.App.Converters;

// values: [thisTail (string), selectedTail (string), isLive (bool)]
// Returns Panel2Brush if selected, LiveTintBrush if live, Transparent otherwise.
public sealed class RowBackgroundConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length < 3) return Brushes.Transparent;
        var thisTail = values[0] as string;
        var selTail  = values[1] as string;
        var live     = values[2] is bool b && b;

        if (!string.IsNullOrEmpty(thisTail) && thisTail == selTail)
            return Application.Current.Resources["Panel2Brush"] ?? Brushes.Transparent;
        if (live)
            return Application.Current.Resources["LiveTintBrush"] ?? Brushes.Transparent;
        return Brushes.Transparent;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
