using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MsfsFailures.App.Views.InFlight;

public partial class Gauge : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(Gauge),
            new PropertyMetadata(string.Empty, (d, e) => ((Gauge)d).LabelText.Text = (string)e.NewValue));
    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    public static readonly DependencyProperty UnitProperty =
        DependencyProperty.Register(nameof(Unit), typeof(string), typeof(Gauge),
            new PropertyMetadata(string.Empty, (d, e) => ((Gauge)d).UnitText.Text = (string)e.NewValue));
    public string Unit { get => (string)GetValue(UnitProperty); set => SetValue(UnitProperty, value); }

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(double), typeof(Gauge),
            new PropertyMetadata(0.0, OnAnyChanged));
    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    public static readonly DependencyProperty MaxProperty =
        DependencyProperty.Register(nameof(Max), typeof(double), typeof(Gauge),
            new PropertyMetadata(1.0, OnAnyChanged));
    public double Max { get => (double)GetValue(MaxProperty); set => SetValue(MaxProperty, value); }

    public static readonly DependencyProperty CautionProperty =
        DependencyProperty.Register(nameof(Caution), typeof(double), typeof(Gauge),
            new PropertyMetadata(double.NaN, OnAnyChanged));
    public double Caution { get => (double)GetValue(CautionProperty); set => SetValue(CautionProperty, value); }

    public static readonly DependencyProperty RedlineProperty =
        DependencyProperty.Register(nameof(Redline), typeof(double), typeof(Gauge),
            new PropertyMetadata(double.NaN, OnAnyChanged));
    public double Redline { get => (double)GetValue(RedlineProperty); set => SetValue(RedlineProperty, value); }

    public Gauge() => InitializeComponent();

    private static void OnAnyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((Gauge)d).Refresh();

    private void Refresh()
    {
        var inv = CultureInfo.InvariantCulture;
        ValueText.Text = Math.Abs(Value - Math.Truncate(Value)) > 0.001
            ? Value.ToString("F1", inv)
            : ((int)Math.Round(Value)).ToString(inv);

        Brush textBrush;
        if (!double.IsNaN(Redline) && Value > Redline)
            textBrush = (Brush)System.Windows.Application.Current.Resources["RedBrush"];
        else if (!double.IsNaN(Caution) && Value > Caution)
            textBrush = (Brush)System.Windows.Application.Current.Resources["AmberBrush"];
        else
            textBrush = (Brush)System.Windows.Application.Current.Resources["TextBrush"];
        ValueText.Foreground = textBrush;

        Bar.Value = Value;
        Bar.Max = Max;
        Bar.Caution = double.IsNaN(Caution) ? null : Caution;
        Bar.Redline = double.IsNaN(Redline) ? null : Redline;
    }
}
