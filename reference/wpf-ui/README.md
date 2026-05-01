# WPF Dense-Data UI Reference Library

Five curated open-source WPF libraries for building aviation dashboards with charts, tables, and glass-cockpit aesthetics.

## Quick Navigation

- **[MANIFEST.md](MANIFEST.md)** — Full inventory with licenses, key files to read, and per-library strengths
- **[notes.md](notes.md)** — Synthesis: which library for which feature (sessions, sparklines, log viewer, etc.)

## The Five Libraries (104 MB total)

| Library | Size | Purpose | License |
|---------|------|---------|---------|
| [OxyPlot](oxyplot-src) | 7.2 MB | Lightweight charting for WPF | MIT |
| [LiveCharts2](livecharts2-src) | 29 MB | Real-time animated charts | MIT |
| [InteractiveDataDisplay.WPF](idd-wpf-src) | 3.7 MB | Dense 2D plots (millions of points) | MS-PL |
| [MaterialDesignInXAML](material-design-src) | 31 MB | Material Design theme + DataGrid | MIT |
| [WPF UI (wpfui)](wpfui-src) | 34 MB | Modern fluent design (Mica blur, accent colors) | MIT |

## How to Use These References

### For Sessions Tab (Flight Parameter Heatmap)
Start in [MANIFEST.md](MANIFEST.md), **OxyPlot** section. Read `Source/OxyPlot.Wpf/PlotView.cs` to understand attached-property binding. Then customize `HeatmapSeries` for your parameter grid.

### For In-Flight Sparklines (Real-Time)
**LiveCharts2** is your primary. Read `src/livecharts/LiveChartsCore/Chart.cs` for the render pipeline. Use `ObservableCollection` with `TryRemoving()` for rolling buffers.

### For Log Viewer (Dense Pan/Zoom)
**InteractiveDataDisplay.WPF** excels here. Study `src/InteractiveDataDisplay.WPF/Navigation/Navigation.cs` for the pan/zoom state machine. Apply to scatter or heatmap of your telemetry.

### For Squawk/Airframe Editor Tables
**wpfui DataGrid** (in `wpfui-src/src/Wpf.Ui/Controls/DataGrid/`) is production-grade. Pair with Consolas font and dark theme from **Material Design** or **wpfui**.

### For Color Scheme & Dark Mode
**wpfui Appearance** (blur, auto dark-mode detection) for modern feel, or **Material Design** for polished consistency. Both are drop-in ResourceDictionary merges.

## Architecture Overview

```
┌─────────────────────────────────────────────────────┐
│  Sessions Tab                                        │
│  ┌─────────────────────────────────────────────┐    │
│  │ OxyPlot HeatmapSeries                       │    │
│  │ (PlotModel bound from ViewModel)            │    │
│  └─────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│  In-Flight Sparklines                               │
│  ┌─────────────────────────────────────────────┐    │
│  │ LiveCharts2 LineSeries (60-point rolling)   │    │
│  │ (Observable collection, auto-batched)       │    │
│  └─────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│  Log Viewer                                         │
│  ┌─────────────────────────────────────────────┐    │
│  │ IDD.WPF Scatter or Heatmap                  │    │
│  │ (Pan/zoom via Navigation state machine)     │    │
│  └─────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│  Bindings Registry                                  │
│  ┌─────────────────────────────────────────────┐    │
│  │ IDD.WPF Scatter (param A vs. param B)       │    │
│  │ (Opacity = correlation strength)            │    │
│  └─────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│  Data Tables (Squawk, Airframe)                     │
│  ┌─────────────────────────────────────────────┐    │
│  │ wpfui DataGrid (dark theme, Consolas font)  │    │
│  │ Virtualization built-in                      │    │
│  └─────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│  Theme Manager                                      │
│  ┌─────────────────────────────────────────────┐    │
│  │ wpfui Appearance (Mica, dark auto-detect)   │    │
│  │ + Material Design colors (fallback)          │    │
│  └─────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────┘
```

## Next Actions

1. **Immediate**: Study MANIFEST.md for quick library comparison.
2. **Sessions**: Implement OxyPlot HeatmapSeries binding.
3. **Real-time**: Add LiveCharts2 sparklines to panel.
4. **Advanced**: Build log viewer pan/zoom on IDD.WPF navigation pattern.

See [MANIFEST.md](MANIFEST.md) for specific file paths and performance notes.
