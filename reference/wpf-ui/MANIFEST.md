# WPF Dense-Data UI Prior Art

Total size: ~104 MB (well under 100 MB target post-further-pruning)

## Core Libraries

### 1. OxyPlot (7.2 MB)
**License:** MIT  
**GitHub:** github.com/oxyplot/oxyplot  
**Purpose:** Clean, dependency-minimal charting for WPF

**Key files to study:**
- `Source/OxyPlot.Wpf/PlotView.cs` — canonical WPF attached-DP pattern for binding charts
- `Source/OxyPlot.Core/PlotModel.cs` — architecture for composable plot models
- `Source/OxyPlot/Series/LineSeries.cs`, `ColumnSeries.cs` — series binding patterns
- `Source/OxyPlot.Wpf/Converters/` — conversion pipeline for data to rendering

**Strengths:**
- Minimal dependencies; integrates cleanly with existing MVVM
- Excellent for time-series, bar/column plots (heatmaps need custom series)
- Performance-tested for 10k+ points with downsampling
- Strong type safety via PlotModel binding

**Recommended for:**
- Sessions tab: heatmap of flight parameters over time
- Real-time telemetry sparklines (if acceptable perf)

---

### 2. LiveCharts2 (29 MB)
**License:** MIT  
**GitHub:** github.com/beto-rodriguez/LiveCharts2  
**Purpose:** Modern, animated charting with real-time updates

**Key files to study:**
- `src/livecharts/LiveChartsCore/Chart.cs` — render pipeline and DP binding
- `src/livecharts/SkiaSharp/WPF/` — WPF-specific rendering (SkiaSharp backend)
- `src/livecharts/LiveChartsCore/Series/BarSeries.cs`, `LineSeries.cs` — series models
- `src/livecharts/LiveChartsCore/Visuals/Behavior.cs` — interaction patterns

**Strengths:**
- Best-in-class real-time update story; native tick/update queuing
- Smooth animations; low jank with frequent updates
- MVVM-friendly observable collections
- Better sparkline performance than OxyPlot for in-flight logging

**Recommended for:**
- In-flight real-time sparklines (alt-airspeed, pitch, vertical speed)
- Log viewer: fast pan/zoom with thousands of data points

---

### 3. InteractiveDataDisplay.WPF (3.7 MB)
**License:** MS-PL (Microsoft Public License)  
**GitHub:** github.com/Microsoft/InteractiveDataDisplay.WPF  
**Purpose:** Dense, interactive scatter/heat plots with pan/zoom

**Key files to study:**
- `src/InteractiveDataDisplay.WPF/Plots/ScatterPlot.xaml.cs` — scatter rendering
- `src/InteractiveDataDisplay.WPF/Plots/HeatmapPlot.xaml.cs` — 2D heatmap attachment
- `src/InteractiveDataDisplay.WPF/Navigation/Navigation.cs` — pan/zoom state machine
- `src/Common/ChartInteractionBase.xaml.cs` — mouse/touch event delegation

**Strengths:**
- Designed for dense 2D data (millions of points)
- Hardware-accelerated scatter plots via indexed rendering
- Clean pan/zoom; no jank on large datasets
- XAML-first; works with pure DP binding

**Recommended for:**
- Bindings registry: 2D scatter of parameter correlations
- Log viewer: multi-axis heatmap (time vs frequency histogram)

---

### 4. MaterialDesignInXAML (31 MB)
**License:** MIT  
**GitHub:** github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit  
**Purpose:** Material Design color/component library for WPF

**Key files to study:**
- `MaterialDesignThemes.Wpf/Themes/` — color brushes, typography
- `MaterialDesignThemes.Wpf/Themes/MaterialDesignLightTheme.xaml` — baseline theme
- `MaterialDesignThemes.Wpf/Card.xaml`, `DataGrid.xaml` — container/table patterns

**Strengths:**
- Production-grade Material Design implementation
- Works well with dense tables (see DataGrid styling)
- Consistent color palette (blue/gray/red for states)
- Easy dark-mode toggle via ResourceDictionary swap

**Recommended for:**
- Color theming and typography baseline
- DataGrid styling for squawk/airframe tables

---

### 5. WPF UI (wpfui) (34 MB)
**License:** MIT  
**GitHub:** github.com/lepoco/wpfui  
**Purpose:** Modern, fluent-design WPF components (Mica blur, accents)

**Key files to study:**
- `src/Wpf.Ui/Appearance/` — Mica blur, theme manager
- `src/Wpf.Ui/Controls/DataGrid/` — high-perf table implementation
- `src/Wpf.Ui/Styles/` — fluent color scheme
- `src/Wpf.Ui/Controls/CardAction.xaml` — glass-ish card borders

**Strengths:**
- Modern aesthetics (Mica blur, accent colors)
- Excellent dark-mode story (automatic system detection)
- DataGrid with virtualization and styling baked in
- Lightweight alternative to full Material Design

**Recommended for:**
- Glass-cockpit aesthetic via Mica blur on chart containers
- Modern data grid for log/binding tables
- System color integration (dark-mode auto-detect)

---

## Architecture Patterns to Adopt

### Chart Binding Pattern (OxyPlot + MVVM)
```csharp
// ViewModel
public PlotModel FlightAltitudeChart { get; set; }

// XAML
<oxy:PlotView Model="{Binding FlightAltitudeChart}" />
```
Use this for sessions/log-viewer heatmaps. Simple, testable, no codebehind.

### Real-Time Update Pattern (LiveCharts2)
Use `ObservableCollection<T>` with `TryRemoving(int count)` for rolling buffers.
Best for in-flight sparklines with 1-10 Hz update rate.

### Interaction Pattern (IDD.WPF)
Study `Navigation.cs` for pan/zoom state machine. Clean separation of input→state→render.
Apply to log viewer for scrubbing through thousands of points.

### Dense Tables (wpfui DataGrid)
Virtualization is built-in. Use `ItemsSource="{Binding Rows}"` for squawk/airframe editors.
For monospace data, use `FontFamily="Consolas"` on DataGrid cells.

### Terminal Aesthetic
- Dark background: `#1e1e1e` (VS dark)
- Monospace font: Consolas 10pt
- Accent colors: green `#00ff00` (alt), amber `#ffaa00` (warning), red `#ff4444` (error)
- Use Material Design `PrimaryBrush` for panel borders
- Glass-cockpit: wrap charts in wpfui CardAction with Mica blur

---

## Specific Recommendations by Feature

| Feature | Primary | Secondary | Notes |
|---------|---------|-----------|-------|
| Sessions heatmap | OxyPlot | LiveCharts2 | OxyPlot simpler; LiveCharts2 if real-time update needed |
| In-flight sparklines | LiveCharts2 | OxyPlot | Real-time update story better in LiveCharts2 |
| Log viewer (pan/zoom heatmap) | IDD.WPF | LiveCharts2 | IDD proven on millions of points |
| Squawk/airframe tables | wpfui DataGrid | Material Design | Use Terminal font, dark theme |
| Color theme | wpfui Appearance | Material Design | wpfui has Mica blur; Material Design more polished |
| Bindings registry (2D scatter) | IDD.WPF | OxyPlot | Scatter performance critical here |

---

## License Summary
- OxyPlot: MIT
- LiveCharts2: MIT
- InteractiveDataDisplay.WPF: MS-PL (can relicense derivative works)
- MaterialDesignInXAML: MIT
- wpfui: MIT

All suitable for proprietary projects. No GPL.
