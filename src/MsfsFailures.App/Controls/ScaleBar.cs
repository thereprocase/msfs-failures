using System.Windows;
using System.Windows.Media;

namespace MsfsFailures.App.Controls;

// Three-zone scale bar: green up to Caution, amber Caution→Redline, red beyond. Needle at Value.
public sealed class ScaleBar : FrameworkElement
{
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(double), typeof(ScaleBar),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    public static readonly DependencyProperty MinProperty =
        DependencyProperty.Register(nameof(Min), typeof(double), typeof(ScaleBar),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
    public double Min { get => (double)GetValue(MinProperty); set => SetValue(MinProperty, value); }

    public static readonly DependencyProperty MaxProperty =
        DependencyProperty.Register(nameof(Max), typeof(double), typeof(ScaleBar),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));
    public double Max { get => (double)GetValue(MaxProperty); set => SetValue(MaxProperty, value); }

    public static readonly DependencyProperty CautionProperty =
        DependencyProperty.Register(nameof(Caution), typeof(double), typeof(ScaleBar),
            new FrameworkPropertyMetadata(0.7, FrameworkPropertyMetadataOptions.AffectsRender));
    public double Caution { get => (double)GetValue(CautionProperty); set => SetValue(CautionProperty, value); }

    public static readonly DependencyProperty RedlineProperty =
        DependencyProperty.Register(nameof(Redline), typeof(double), typeof(ScaleBar),
            new FrameworkPropertyMetadata(0.9, FrameworkPropertyMetadataOptions.AffectsRender));
    public double Redline { get => (double)GetValue(RedlineProperty); set => SetValue(RedlineProperty, value); }

    private static readonly Brush GreenZone = new SolidColorBrush(Color.FromArgb(51, 168, 201, 122));
    private static readonly Brush AmberZone = new SolidColorBrush(Color.FromArgb(64, 212, 162, 76));
    private static readonly Brush RedZone   = new SolidColorBrush(Color.FromArgb(64, 194, 84, 80));
    private static readonly Brush BgFill    = new SolidColorBrush(Color.FromArgb(10, 255, 255, 255));

    static ScaleBar()
    {
        GreenZone.Freeze(); AmberZone.Freeze(); RedZone.Freeze(); BgFill.Freeze();
    }

    protected override void OnRender(DrawingContext dc)
    {
        var w = ActualWidth;
        var h = ActualHeight;
        if (w <= 0 || h <= 0) return;
        var range = Max - Min;
        if (range <= 0) return;
        var trackH = Math.Min(h, 10.0);
        var trackY = (h - trackH) / 2.0;

        dc.DrawRectangle(BgFill, null, new Rect(0, trackY, w, trackH));

        var cX = (Caution - Min) / range * w;
        var rX = (Redline - Min) / range * w;
        cX = Math.Clamp(cX, 0, w);
        rX = Math.Clamp(rX, 0, w);
        dc.DrawRectangle(GreenZone, null, new Rect(0, trackY, cX, trackH));
        dc.DrawRectangle(AmberZone, null, new Rect(cX, trackY, Math.Max(0, rX - cX), trackH));
        dc.DrawRectangle(RedZone,   null, new Rect(rX, trackY, Math.Max(0, w - rX), trackH));

        var pct = Math.Clamp((Value - Min) / range, 0, 1);
        var nx = pct * w;
        var text = (Brush)System.Windows.Application.Current.Resources["TextBrush"];
        dc.DrawRectangle(text, null, new Rect(nx - 1, trackY - 3, 2, trackH + 6));
    }
}
