using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MsfsFailures.App.Logging;

/// <summary>
/// Converts a Serilog level string ("INFORMATION", "WARNING", "ERROR", "DEBUG", etc.)
/// to a brush — or, when ConverterParameter=="abbrev", to a short 3-char label.
/// </summary>
[ValueConversion(typeof(string), typeof(object))]
public sealed class LogLevelToBrushConverter : IValueConverter
{
    // Brushes are frozen so they can be used from any thread safely.
    private static readonly SolidColorBrush InfBrush  = Freeze(new SolidColorBrush(Color.FromRgb(0xDD, 0xD6, 0xC4)));
    private static readonly SolidColorBrush WrnBrush  = Freeze(new SolidColorBrush(Color.FromRgb(0xD4, 0xA2, 0x4C)));
    private static readonly SolidColorBrush ErrBrush  = Freeze(new SolidColorBrush(Color.FromRgb(0xC2, 0x54, 0x50)));
    private static readonly SolidColorBrush DbgBrush  = Freeze(new SolidColorBrush(Color.FromRgb(0x54, 0x50, 0x3F)));
    private static readonly SolidColorBrush FatalBrush = Freeze(new SolidColorBrush(Color.FromRgb(0xC2, 0x54, 0x50)));

    private static SolidColorBrush Freeze(SolidColorBrush b) { b.Freeze(); return b; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var level = value as string ?? string.Empty;
        bool abbrev = parameter is string p && p == "abbrev";

        if (abbrev)
        {
            return level switch
            {
                "INFORMATION" => "INF",
                "WARNING"     => "WRN",
                "ERROR"       => "ERR",
                "DEBUG"       => "DBG",
                "FATAL"       => "FTL",
                "VERBOSE"     => "VRB",
                _             => level.Length >= 3 ? level[..3] : level,
            };
        }

        return level switch
        {
            "INFORMATION" => InfBrush,
            "WARNING"     => WrnBrush,
            "ERROR"       => ErrBrush,
            "FATAL"       => FatalBrush,
            _             => DbgBrush,
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
