using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MsfsFailures.App.Views.InFlight;

public partial class Trend : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(Trend),
            new PropertyMetadata(string.Empty, (d, e) => ((Trend)d).TitleText.Text = (string)e.NewValue));
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    public static readonly DependencyProperty UnitProperty =
        DependencyProperty.Register(nameof(Unit), typeof(string), typeof(Trend),
            new PropertyMetadata(string.Empty, (d, e) => ((Trend)d).UnitText.Text = (string)e.NewValue));
    public string Unit { get => (string)GetValue(UnitProperty); set => SetValue(UnitProperty, value); }

    public static readonly DependencyProperty DataProperty =
        DependencyProperty.Register(nameof(Data), typeof(IReadOnlyList<double>), typeof(Trend),
            new PropertyMetadata(null, OnDataChanged));
    public IReadOnlyList<double>? Data { get => (IReadOnlyList<double>?)GetValue(DataProperty); set => SetValue(DataProperty, value); }

    public static readonly DependencyProperty NowProperty =
        DependencyProperty.Register(nameof(Now), typeof(double), typeof(Trend),
            new PropertyMetadata(0.0, OnNowChanged));
    public double Now { get => (double)GetValue(NowProperty); set => SetValue(NowProperty, value); }

    public static readonly DependencyProperty CautionProperty =
        DependencyProperty.Register(nameof(Caution), typeof(double), typeof(Trend),
            new PropertyMetadata(double.NaN, OnDataChanged));
    public double Caution { get => (double)GetValue(CautionProperty); set => SetValue(CautionProperty, value); }

    public static readonly DependencyProperty RedlineProperty =
        DependencyProperty.Register(nameof(Redline), typeof(double), typeof(Trend),
            new PropertyMetadata(double.NaN, OnDataChanged));
    public double Redline { get => (double)GetValue(RedlineProperty); set => SetValue(RedlineProperty, value); }

    public Trend() => InitializeComponent();

    private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((Trend)d).Refresh();
    private static void OnNowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)  => ((Trend)d).Refresh();

    private void Refresh()
    {
        SparkChart.Data    = Data;
        SparkChart.Caution = double.IsNaN(Caution) ? null : Caution;
        SparkChart.Redline = double.IsNaN(Redline) ? null : Redline;

        var inv = CultureInfo.InvariantCulture;
        NowText.Text = ((int)Math.Round(Now)).ToString(inv);

        if (Data is { Count: > 0 } data)
        {
            var peak = data[0]; var sum = 0.0;
            foreach (var v in data) { if (v > peak) peak = v; sum += v; }
            var avg = sum / data.Count;
            PeakText.Text = ((int)Math.Round(peak)).ToString(inv);
            AvgText.Text  = ((int)Math.Round(avg)).ToString(inv);

            Brush peakBrush;
            if (!double.IsNaN(Redline) && peak > Redline) peakBrush = (Brush)System.Windows.Application.Current.Resources["RedBrush"];
            else if (!double.IsNaN(Caution) && peak > Caution) peakBrush = (Brush)System.Windows.Application.Current.Resources["AmberBrush"];
            else peakBrush = (Brush)System.Windows.Application.Current.Resources["TextBrush"];
            PeakText.Foreground = peakBrush;
        }
    }
}
