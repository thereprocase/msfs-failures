using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace MsfsFailures.App.Controls;

// Circular progress arc with centered numeric value (rounded to int %).
public sealed class Donut : FrameworkElement
{
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(double), typeof(Donut),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    public static readonly DependencyProperty StrokeProperty =
        DependencyProperty.Register(nameof(Stroke), typeof(Brush), typeof(Donut),
            new FrameworkPropertyMetadata(Brushes.LimeGreen, FrameworkPropertyMetadataOptions.AffectsRender));
    public Brush Stroke { get => (Brush)GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

    public static readonly DependencyProperty TrackProperty =
        DependencyProperty.Register(nameof(Track), typeof(Brush), typeof(Donut),
            new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromArgb(15, 255, 255, 255)),
                FrameworkPropertyMetadataOptions.AffectsRender));
    public Brush Track { get => (Brush)GetValue(TrackProperty); set => SetValue(TrackProperty, value); }

    public static readonly DependencyProperty StrokeThicknessProperty =
        DependencyProperty.Register(nameof(StrokeThickness), typeof(double), typeof(Donut),
            new FrameworkPropertyMetadata(6.0, FrameworkPropertyMetadataOptions.AffectsRender));
    public double StrokeThickness { get => (double)GetValue(StrokeThicknessProperty); set => SetValue(StrokeThicknessProperty, value); }

    public static readonly DependencyProperty TextSizeProperty =
        DependencyProperty.Register(nameof(TextSize), typeof(double), typeof(Donut),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
    public double TextSize { get => (double)GetValue(TextSizeProperty); set => SetValue(TextSizeProperty, value); }

    protected override void OnRender(DrawingContext dc)
    {
        var w = ActualWidth; var h = ActualHeight;
        if (w <= 0 || h <= 0) return;
        var size = Math.Min(w, h);
        var cx = w / 2.0; var cy = h / 2.0;
        var r = (size / 2.0) - StrokeThickness;
        if (r <= 0) return;

        var trackPen = new Pen(Track, StrokeThickness);
        dc.DrawEllipse(null, trackPen, new Point(cx, cy), r, r);

        var v = Math.Clamp(Value, 0.0, 1.0);
        if (v > 0)
        {
            var pen = new Pen(Stroke, StrokeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
            var sweep = v * 360.0;
            var startA = -90.0 * Math.PI / 180.0;
            var endA = (-90.0 + sweep) * Math.PI / 180.0;
            var p0 = new Point(cx + r * Math.Cos(startA), cy + r * Math.Sin(startA));
            var p1 = new Point(cx + r * Math.Cos(endA),   cy + r * Math.Sin(endA));
            var isLarge = sweep > 180.0;

            var fig = new PathFigure { StartPoint = p0, IsClosed = false, IsFilled = false };
            fig.Segments.Add(new ArcSegment(p1, new Size(r, r), 0, isLarge, SweepDirection.Clockwise, true));
            var geo = new PathGeometry();
            geo.Figures.Add(fig);
            dc.DrawGeometry(null, pen, geo);
        }

        var textSize = TextSize > 0 ? TextSize : size * 0.26;
        var pct = (int)Math.Round(v * 100.0);
        var ft = new FormattedText(
            pct.ToString(CultureInfo.InvariantCulture),
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily("pack://application:,,,/Assets/Fonts/#JetBrains Mono"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
            textSize,
            Stroke,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(ft, new Point(cx - ft.Width / 2.0, cy - ft.Height / 2.0));
    }
}
