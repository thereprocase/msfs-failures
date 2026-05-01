using System.Windows;
using System.Windows.Media;

namespace MsfsFailures.App.Controls;

// Sparkline. Auto-scales to min/max of Data; draws polyline + optional area fill + caution/redline guides.
public sealed class Spark : FrameworkElement
{
    public static readonly DependencyProperty DataProperty =
        DependencyProperty.Register(nameof(Data), typeof(IReadOnlyList<double>), typeof(Spark),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
    public IReadOnlyList<double>? Data { get => (IReadOnlyList<double>?)GetValue(DataProperty); set => SetValue(DataProperty, value); }

    public static readonly DependencyProperty StrokeProperty =
        DependencyProperty.Register(nameof(Stroke), typeof(Brush), typeof(Spark),
            new FrameworkPropertyMetadata(Brushes.LimeGreen, FrameworkPropertyMetadataOptions.AffectsRender));
    public Brush Stroke { get => (Brush)GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

    public static readonly DependencyProperty FillProperty =
        DependencyProperty.Register(nameof(Fill), typeof(bool), typeof(Spark),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));
    public bool Fill { get => (bool)GetValue(FillProperty); set => SetValue(FillProperty, value); }

    public static readonly DependencyProperty CautionProperty =
        DependencyProperty.Register(nameof(Caution), typeof(double?), typeof(Spark),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
    public double? Caution { get => (double?)GetValue(CautionProperty); set => SetValue(CautionProperty, value); }

    public static readonly DependencyProperty RedlineProperty =
        DependencyProperty.Register(nameof(Redline), typeof(double?), typeof(Spark),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
    public double? Redline { get => (double?)GetValue(RedlineProperty); set => SetValue(RedlineProperty, value); }

    protected override void OnRender(DrawingContext dc)
    {
        var data = Data;
        if (data == null || data.Count < 2 || ActualWidth <= 0 || ActualHeight <= 0) return;

        var w = ActualWidth; var h = ActualHeight;
        var min = data[0]; var max = data[0];
        foreach (var v in data) { if (v < min) min = v; if (v > max) max = v; }
        var range = (max - min);
        if (range <= 0) range = 1;

        Point Map(int i, double v) =>
            new(i / (double)(data.Count - 1) * w, h - (v - min) / range * h);

        // Caution/redline guide lines
        var amber = (Brush)System.Windows.Application.Current.Resources["AmberBrush"];
        var red = (Brush)System.Windows.Application.Current.Resources["RedBrush"];

        if (Caution is double c && c >= min && c <= max)
        {
            var y = h - (c - min) / range * h;
            var pen = new Pen(amber, 0.5) { DashStyle = new DashStyle([3, 2], 0) };
            dc.DrawLine(pen, new Point(0, y), new Point(w, y));
        }
        if (Redline is double r && r >= min && r <= max)
        {
            var y = h - (r - min) / range * h;
            var pen = new Pen(red, 0.5) { DashStyle = new DashStyle([3, 2], 0) };
            dc.DrawLine(pen, new Point(0, y), new Point(w, y));
        }

        var pts = new List<Point>(data.Count);
        for (var i = 0; i < data.Count; i++) pts.Add(Map(i, data[i]));

        if (Fill)
        {
            var fillFig = new PathFigure { StartPoint = new Point(0, h), IsClosed = true, IsFilled = true };
            foreach (var p in pts) fillFig.Segments.Add(new LineSegment(p, false));
            fillFig.Segments.Add(new LineSegment(new Point(w, h), false));
            var fillGeo = new PathGeometry();
            fillGeo.Figures.Add(fillFig);
            var fillBrush = Stroke.Clone();
            fillBrush.Opacity = 0.15;
            fillBrush.Freeze();
            dc.DrawGeometry(fillBrush, null, fillGeo);
        }

        var lineFig = new PathFigure { StartPoint = pts[0], IsClosed = false, IsFilled = false };
        for (var i = 1; i < pts.Count; i++) lineFig.Segments.Add(new LineSegment(pts[i], true));
        var lineGeo = new PathGeometry();
        lineGeo.Figures.Add(lineFig);
        dc.DrawGeometry(null, new Pen(Stroke, 1.0), lineGeo);
    }
}
