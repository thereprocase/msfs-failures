# WPF Dense-Data UI: Synthesis & Patterns

## Core Strategy

**OxyPlot** for static/batch heatmaps (sessions tab). **LiveCharts2** for real-time streaming (in-flight sparklines). **IDD.WPF** for interactive dense scatter/heatmaps (bindings registry, log heatmaps). **wpfui** for theming/tables (modern dark aesthetic, Mica blur). Material Design as fallback color library.

## Key Architectural Patterns

### 1. MVVM Chart Binding (OxyPlot)
PlotModel lives in ViewModel; bind via `Model` DP. No codebehind. Series populate from data in PlotModel constructor or via observable collections. For heatmaps, use custom HeatmapSeries subclass of XYAxisSeries.

### 2. Real-Time Update Queuing (LiveCharts2)
Use `ObservableCollection` with `TryRemoving()` for fixed-size buffers. LiveCharts2 batches renders internally; won't jank at 10 Hz with 1000-point rolling window. Prefer over OxyPlot for anything that updates > 1 Hz.

### 3. Dense Pan/Zoom (IDD.WPF)
Study `Navigation` state machine. Input (mouse/touch) → NavigationState (pan offset, zoom level) → render transform. Separates interaction from data. Apply to log viewer for scrubbing.

### 4. Glass-Cockpit Aesthetic
- Dark bg `#1e1e1e`, white/green text
- Wrap charts in `CardAction` with Mica blur (wpfui)
- Monospace body: Consolas 10pt for tables
- Accent: green for normal, amber for caution, red for critical

### 5. Terminal-Style DataGrid
Use wpfui DataGrid with dark theme + Consolas font. Set `EnableColumnVirtualization="True"` for squawk/airframe tables with 1000+ rows.

## Specific Implementation Notes

**Sessions tab heatmap:** OxyPlot HeatmapSeries, bind PlotModel from ViewModel, use ColorAxis for legend.

**In-flight sparklines:** LiveCharts2 LineSeries with 60-point rolling buffer. Update every 100ms via dispatcher binding.

**Log viewer:** IDD.WPF scatter (parameter vs. time), or custom HeatmapSeries (histogram of parameter frequency over time). Pan/zoom via Navigation pattern.

**Bindings registry:** IDD.WPF scatter plot with parameter A vs. parameter B, point opacity = correlation strength.

**Squawk editor:** Dark-themed DataGrid (wpfui), Consolas font, light gray text on dark bg.

**Color palette:** Steal from Material Design dark theme (primary blue, error red) + aviation greens (normal / caution / warning).

## Performance Notes

OxyPlot handles ~10k points before downsampling needed. LiveCharts2 ~5k with animation. IDD.WPF ~1M in sparse scatter. Heatmaps: use texture-backed rendering in IDD or pre-render OxyPlot to bitmap for dense displays.

## Next Steps

Start with OxyPlot for sessions heatmap (simplest), add LiveCharts2 for in-flight sparklines, then IDD.WPF for log viewer pan/zoom. Adopt wpfui Appearance for theme manager.
