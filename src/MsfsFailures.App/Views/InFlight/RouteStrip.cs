using System.Globalization;
using System.Windows;
using System.Windows.Media;
using MsfsFailures.App.ViewModels;

namespace MsfsFailures.App.Views.InFlight;

// Horizontal route strip: progress bar + waypoint dots + waypoint labels.
public sealed class RouteStrip : FrameworkElement
{
    public static readonly DependencyProperty WaypointsProperty =
        DependencyProperty.Register(nameof(Waypoints), typeof(IReadOnlyList<RouteWaypoint>), typeof(RouteStrip),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));
    public IReadOnlyList<RouteWaypoint>? Waypoints
    {
        get => (IReadOnlyList<RouteWaypoint>?)GetValue(WaypointsProperty);
        set => SetValue(WaypointsProperty, value);
    }

    public static readonly DependencyProperty ProgressProperty =
        DependencyProperty.Register(nameof(Progress), typeof(double), typeof(RouteStrip),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
    public double Progress { get => (double)GetValue(ProgressProperty); set => SetValue(ProgressProperty, value); }

    public RouteStrip()
    {
        Height = 50;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var w = double.IsInfinity(availableSize.Width) ? 200 : availableSize.Width;
        return new Size(w, 50);
    }

    protected override void OnRender(DrawingContext dc)
    {
        var w = ActualWidth;
        var h = ActualHeight;
        if (w <= 0 || h <= 0) return;

        var trackY = 8.0;
        var trackBrush = (Brush)System.Windows.Application.Current.Resources["RuleBrush"];
        var greenBrush = (Brush)System.Windows.Application.Current.Resources["GreenBrush"];
        var amberBrush = (Brush)System.Windows.Application.Current.Resources["AmberBrush"];
        var faintBrush = (Brush)System.Windows.Application.Current.Resources["FaintBrush"];
        var panel2     = (Brush)System.Windows.Application.Current.Resources["Panel2Brush"];
        var textBrush  = (Brush)System.Windows.Application.Current.Resources["TextBrush"];

        dc.DrawRectangle(trackBrush, null, new Rect(0, trackY, w, 2));
        dc.DrawRectangle(greenBrush, null, new Rect(0, trackY, w * Math.Clamp(Progress, 0, 1), 2));

        var wps = Waypoints;
        if (wps == null) return;

        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var monoFamily = new FontFamily("pack://application:,,,/Assets/Fonts/#JetBrains Mono");
        var monoBold   = new Typeface(monoFamily, FontStyles.Normal, FontWeights.Bold,    FontStretches.Normal);
        var monoNormal = new Typeface(monoFamily, FontStyles.Normal, FontWeights.Normal,  FontStretches.Normal);

        for (var i = 0; i < wps.Count; i++)
        {
            var wp = wps[i];
            var x = wp.Position * w;
            var passed = wp.Position < Progress;
            var here = Math.Abs(wp.Position - Progress) < 0.08;

            Brush dotFill = panel2;
            Brush dotStroke = faintBrush;
            if (here) { dotFill = amberBrush; dotStroke = amberBrush; }
            else if (passed) { dotFill = greenBrush; dotStroke = greenBrush; }

            dc.DrawEllipse(dotFill, new Pen(dotStroke, 2), new Point(x, trackY + 1), 5, 5);

            var idText = new FormattedText(wp.Id, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                                           monoBold, 10, textBrush, dpi);
            var timeText = new FormattedText(wp.Time, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                                             monoNormal, 9, faintBrush, dpi);
            var idX = Math.Clamp(x - idText.Width / 2, 0, w - idText.Width);
            var timeX = Math.Clamp(x - timeText.Width / 2, 0, w - timeText.Width);
            dc.DrawText(idText, new Point(idX, trackY + 12));
            dc.DrawText(timeText, new Point(timeX, trackY + 12 + idText.Height + 1));
        }
    }
}
