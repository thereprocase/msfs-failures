using System.Windows;
using System.Windows.Media;

namespace MsfsFailures.App.Controls;

// Horizontal bar: filled to Value, with optional caution/redline tick marks above the track.
public sealed class MarkerBar : FrameworkElement
{
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(double), typeof(MarkerBar),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    public static readonly DependencyProperty MinProperty =
        DependencyProperty.Register(nameof(Min), typeof(double), typeof(MarkerBar),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
    public double Min { get => (double)GetValue(MinProperty); set => SetValue(MinProperty, value); }

    public static readonly DependencyProperty MaxProperty =
        DependencyProperty.Register(nameof(Max), typeof(double), typeof(MarkerBar),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));
    public double Max { get => (double)GetValue(MaxProperty); set => SetValue(MaxProperty, value); }

    public static readonly DependencyProperty FillProperty =
        DependencyProperty.Register(nameof(Fill), typeof(Brush), typeof(MarkerBar),
            new FrameworkPropertyMetadata(Brushes.LimeGreen, FrameworkPropertyMetadataOptions.AffectsRender));
    public Brush Fill { get => (Brush)GetValue(FillProperty); set => SetValue(FillProperty, value); }

    public static readonly DependencyProperty CautionProperty =
        DependencyProperty.Register(nameof(Caution), typeof(double?), typeof(MarkerBar),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
    public double? Caution { get => (double?)GetValue(CautionProperty); set => SetValue(CautionProperty, value); }

    public static readonly DependencyProperty RedlineProperty =
        DependencyProperty.Register(nameof(Redline), typeof(double?), typeof(MarkerBar),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
    public double? Redline { get => (double?)GetValue(RedlineProperty); set => SetValue(RedlineProperty, value); }

    private static readonly Brush TrackBrush = new SolidColorBrush(Color.FromArgb(10, 255, 255, 255));

    static MarkerBar()
    {
        TrackBrush.Freeze();
    }

    protected override void OnRender(DrawingContext dc)
    {
        var w = ActualWidth;
        var h = ActualHeight;
        if (w <= 0 || h <= 0) return;

        var range = Max - Min;
        if (range <= 0) return;
        var pct = Math.Clamp((Value - Min) / range, 0, 1);

        // Track
        var trackHeight = Math.Min(h, 5.0);
        var trackY = h - trackHeight;
        dc.DrawRectangle(TrackBrush, null, new Rect(0, trackY, w, trackHeight));
        // Fill
        dc.DrawRectangle(Fill, null, new Rect(0, trackY, w * pct, trackHeight));

        // Tick markers (rise above the track)
        var amber = (Brush)System.Windows.Application.Current.Resources["AmberBrush"];
        var red = (Brush)System.Windows.Application.Current.Resources["RedBrush"];

        if (Caution is double c)
        {
            var x = (c - Min) / range * w;
            dc.DrawRectangle(amber, null, new Rect(x - 0.5, trackY - 2, 1, trackHeight + 4));
        }
        if (Redline is double r)
        {
            var x = (r - Min) / range * w;
            dc.DrawRectangle(red, null, new Rect(x - 0.5, trackY - 2, 1, trackHeight + 4));
        }
    }
}
