# JPSoftworks.DesktopToolkit.Controls.Charts

Fluent controls for WinUI 3, including bar, line, scatter, spider, heatmap, Sankey, funnel, live area, doughnut, treemap, icicle, and sunburst charts, usage meters, and radial gauges.

The library targets .NET 8 with Windows SDK 10.0.19041.0 and can be consumed by
.NET 8 or later WinUI applications. The minimum OS is Windows 10 version 1809.
It depends on Windows App SDK 1.8.260317003 and Win2D 1.4.0. Use a Windows
architecture (x64, x86, or ARM64) for the application; the library is AnyCPU.

## Install and use

For a local preview, run `./eng/Build.ps1` from the Desktop Toolkit checkout in PowerShell.
Add the checkout's `artifacts/packages` directory to your NuGet sources and keep your regular sources enabled for dependencies.

Run the following command from your WinUI app project.

```powershell
dotnet add package JPSoftworks.DesktopToolkit.Controls.Charts --version 0.1.0-preview.1
```

Keep the standard `XamlControlsResources` in your application's resources.
The package supplies `Themes/Generic.xaml`; no manual dictionary merge is needed.
It also includes XML documentation beside the assembly for IntelliSense.

```xml
<Page
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:charts="using:JPSoftworks.DesktopToolkit.Charts">
    <charts:LiveAreaGraph x:Name="Graph" Height="260" TooltipValueSuffix="%" />
</Page>
```

```csharp
using JPSoftworks.DesktopToolkit.Charts;
using Windows.UI;

Graph.HistoryDuration = TimeSpan.FromSeconds(60);
Graph.AddSeries("CPU", Color.FromArgb(255, 0, 120, 212),
    Color.FromArgb(48, 0, 120, 212));
Graph.AppendSample(42);
```

## Controls

| Control | Contract |
| --- | --- |
| [`BarChart`](#barchart) | A bindable snapshot of categories and series, displayed as grouped, stacked, or 100% stacked bars in either orientation. |
| [`LineChart`](#linechart) | Bind supplied series to a numeric or UTC time axis. Supports gaps, optional fill, stacked area, and inspection. |
| [`LineChartOverview`](#linechartoverview) | A compact navigator for panning and resizing a line chart viewport. |
| [`ScatterChart`](#scatterchart) | Compare numeric X and Y measurements across one or more series. |
| [`SpiderChart`](#spiderchart) | Compare profiles across labeled dimensions with independent ranges. |
| [`HeatmapChart`](#heatmapchart) | Color a matrix of measurements or a calendar of daily activity. |
| [`SankeyChart`](#sankeychart) | Follow weighted flows between nodes in a directed acyclic network. |
| [`FunnelChart`](#funnelchart) | Compare counts and conversion through ordered stages. |
| [`TreemapChart`](#treemapchart) | Compare weighted branches as nested rectangles. |
| [`IcicleChart`](#iciclechart) | Compare weighted branches in aligned rows or columns by depth. |
| [`SunburstChart`](#sunburstchart) | Compare weighted branches by angle across concentric rings. |
| [`LiveAreaGraph`](#liveareagraph) | Append timestamped values for one or more series. Supports live scrolling, a compact sparkline presentation, and pointer or keyboard inspection. |
| [`VerticalUsageBar`](#verticalusagebar) | A `RangeBase` meter with vertical or horizontal orientation and bindable stacked contributions. |
| [`RadialGauge`](#radialgauge) | A read-only `RangeBase` gauge with a configurable arc, formatted value label, and optional threshold bands. |
| [`DoughnutGraph`](#doughnutgraph) | Bind slices or stream their values. Displays a doughnut or a filled pie with an optional responsive legend. |

See [Styling and theming](#styling-and-theming) for the style reference for each control.

Bind `Data="{x:Bind ViewModel.ChartData, Mode=OneWay}"` and raise
`INotifyPropertyChanged` when publishing a new snapshot. Snapshot constructors
copy their data arrays. Update the view model binding on the UI thread.

### Color palettes

`ChartPalette` includes `Default`, `Vivid`, `Soft`, and `Earth` categorical
sets. Set `ChartPalette.Colors` on a bar, line, scatter, spider, treemap, icicle,
or sunburst chart. It accepts one-way bindings and picks the light or dark
colors automatically. A series with its own color keeps that color.

```xml
<charts:BarChart Data="{x:Bind UsageData}" charts:ChartPalette.Colors="{x:Bind UsageColors}" />
```

```csharp
public ChartColorSet UsageColors { get; } = ChartPalette.Vivid;
```

Use `ChartPalette.CreateFromAccent(accentColor, count: 6)` to generate
categorical colors, or construct a `ChartColorSet` from your own colors.
For streaming graphs, doughnuts, and usage meters, pass the colors from
`palette.GetColor(index, chart.ActualTheme)` to the series setup. The index
cycles when it exceeds the number of colors in the set.

Use `ChartPalette.CreateRamp(accentColor, count: 5)` for an ordered range of
intensities. Pair the ramp with ascending values through
`HeatmapColorScale.FromPalette`. The `ActivityGreen` preset begins with a
neutral zero and includes four green intensities for contribution calendars.

```csharp
Calendar.ColorScale = HeatmapColorScale.FromPalette(
    ChartPalette.ActivityGreen, [0, 1, 3, 5, 8], Calendar.ActualTheme, isStepped: true);
```

Refresh an assigned heatmap scale on `ActualThemeChanged` to pick up the
other variant. The gallery's daily contributions example shows this pattern.

### Bar chart

```xml
<charts:BarChart x:Name="Comparison" Height="240" Orientation="Horizontal"
                 ValueFormat="0.#' GB'" AutomationProperties.Name="Storage change by category" />
```

```csharp
Comparison.Data = new BarChartData(
    ["Apps", "Downloads", "Media"],
    [
        new BarChartSeries("Last week", [6, -7, 3]),
        new BarChartSeries("This week", [3, -4, null]),
    ]);
```

Each series supplies a value for every category. Use `Mode` to choose the layout.

| Mode | Behavior |
| --- | --- |
| `Grouped` (default) | Each category has a slot per series. Null values leave gaps, and negative bars extend from zero in the opposite direction. |
| `Stacked` | Each category has one stack. Positive and negative values accumulate independently from zero in series order. The automatic axis includes each signed total. Zero and null values take no space. |
| `Stacked100` | Each category has its own 0-100% stack. Only positive values contribute to the total. Categories with no positive values are empty. Inspect a segment to see its original value and its share. |

For grouped and regular stacked charts, set a nonpositive `Minimum` or a
nonnegative `Maximum` to fix either end of the automatic axis. A 100% stack
always uses a 0-100% axis. `ValueFormat` formats raw values, including the
original value shown when inspecting a percentage segment.
Provide a new snapshot on the UI thread or bind `Data` to an updated snapshot.
Use an optional series brush to override its default color.
Hover over bars or use the arrow keys, Home, and End while the chart has focus.
Set `BarSpacing` to a finite, non-negative number of DIPs to pack bars across
the plot with spacing between them and no outer gaps. The default is `NaN`
for automatic sizing and spacing.

In `Stacked` and `Stacked100`, `SegmentGap` adds space between segments and
`SegmentCornerRadius` sets the maximum radius. Both default to 0 DIPs.
`SegmentRounding="OuterCorners"` is the default. It rounds the stack ends
and leaves the joins square, including zero where positive and negative
segments meet. Set `SegmentRounding="AllCorners"` to round segments individually.
Bar ends that touch the visible zero baseline stay square in every mode.
The gap and radius shrink as needed to keep narrow segments visible.
Use the gallery's **Round corners** picker on **Sign-ups by platform** to
compare the two styles.

### Line chart

```xml
<charts:LineChart Height="250" Data="{x:Bind Latency, Mode=OneWay}"
                  XValueFormat="0' r/s'" ValueFormat="0' ms'"
                  AutomationProperties.Name="Latency by request rate" />
```

```csharp
public LineChartData Latency { get; } = new(
[
    new LineChartSeries("Before tuning", [new(0, 32), new(30, null), new(60, 72), new(100, 110)]),
    new LineChartSeries("After tuning", [new(0, 21), new(25, 29), new(70, 54), new(100, 76)]),
]);
```

Each series supplies its own ascending X coordinates. Pass `DateTimeOffset`
values instead of numbers for a time axis. All series in a snapshot share the
same X axis type, and timestamps are plotted and formatted in UTC. Set
`XValueFormat` to a numeric or date format that matches your data.

A null Y value splits the line and the area beneath it. A lone point is shown
as a dot. Set `ShowAreaFill` to true to fill each uninterrupted run to zero
or to the nearest plot edge when zero is outside the Y range. Supply optional
series colors, `StrokeThickness`, `IsStrokeDashed`, and `ShowAreaFill` to style
individual series. In stacked area mode every band is filled.

`Interpolation` defaults to `Linear`. Set it to `Monotone` to smooth runs of
three or more samples on either axis. The curve passes through every sample
and stays within the range of adjacent Y values. Repeated X values use a
straight vertical segment. The fill follows the same curves and gaps.

Assign a new `Data` snapshot on the UI thread or notify a one-way binding when
the source changes. Hover near a point or use arrow keys, Home, and End while
the chart is focused to inspect supplied values.

#### Zoom and pan

Set `IsViewportInteractionEnabled="True"` to enable panning and range
selection on the plot. Drag with the left mouse button to select a range.
Drag inside the selection to move it, or drag either fence to resize it.
Escape cancels the current drag. Escape or a click away from the selection
clears it. Set `IsSelectionEnabled="False"` to clear the range and turn
off selection for this chart.

Zoom gestures are off by default. With viewport interaction enabled, set
`IsZoomEnabled="True"` to enable Ctrl+wheel, two-finger pinch, and
Ctrl+Up/Ctrl+Down. Clicking inside a selection or pressing Enter then zooms
to the selected range. Pair gesture zoom with a visible viewport readout
and a way to fit the full range.

Use middle-drag, Shift+wheel, or horizontal scrolling to pan a narrowed
viewport. With the chart focused, Ctrl+Left and Ctrl+Right pan and Ctrl+Home
fits the chart. The ordinary arrow keys still inspect values.

```xml
<charts:LineChart Data="{x:Bind Timeline}" Viewport="{x:Bind RequestedWindow, Mode=OneWay}"
                  IsViewportInteractionEnabled="True" IsZoomEnabled="True" />
```

Construct a `LineChartViewport` with numeric bounds or two
`DateTimeOffset` values. A compatible viewport takes priority over the
usual X-axis bounds. A new data snapshot reapplies the requested viewport.
Bind to the read-only `VisibleViewport` and `SelectedRange` properties with
one-way binding. `VisibleViewport` is null while the chart uses automatic or
configured axis bounds. `SelectedRange` is null until a range is selected.
`ViewportChanged` and `SelectionChanged` also support event handlers.
Use `ZoomBy`, `PanBy`, and `ResetViewport` for external navigation. These
methods work independently of the chart's gesture settings.
`LineChartData.Extent` reports the full sample range. Use `ZoomToSelection`
or `ClearSelection` from a toolbar. Selection works on numeric and time
axes. A viewport or data change clears it.

```xml
<charts:LineChart x:Name="Timeline" Data="{x:Bind TimelineData}"
                  IsViewportInteractionEnabled="True" />
<TextBlock Text="{x:Bind Timeline.VisibleViewport.Minimum, Mode=OneWay}" />
<TextBlock Text="{x:Bind Timeline.SelectedRange.Maximum, Mode=OneWay}" />
```

#### Timeline overview

Pair a `LineChartOverview` with a line chart to show its full numeric or
time range. The overview can navigate its target independently of the target's
gesture settings. Drag either handle to resize the visible
window. Drag inside the window to pan or click elsewhere to center it. Ctrl+wheel and a
two-finger pinch zoom on the overview. With the overview focused, use
Left/Right to pan, Page Up/Page Down to move by most of the visible window,
and Home/End to reach either end. Ctrl+Up/Ctrl+Down zoom and Ctrl+Home fits.

```xml
<charts:LineChart x:Name="TimelineChart" Data="{x:Bind Timeline}"
                  IsViewportInteractionEnabled="True" />
<charts:LineChartOverview Target="{x:Bind TimelineChart}"
                          AutomationProperties.Name="Timeline overview" />
```

#### Linked timelines

Assign the same `LineChartViewportGroup` to each chart's `ViewportGroup`.
Panning, zooming, or fitting any member updates the shared X window. The
group fits the combined sample range, and each chart keeps its own series
and Y axis. Use the same X-axis type for all members. If the group contains
both types, only charts matching the first available extent are linked.
Set `IsSelectionSynchronized` on the group to share range previews, edits,
and committed selections across compatible members. The option defaults to
false. Set `IsSelectionEnabled="False"` on a member to exclude it from
selection synchronization while keeping its viewport linked. A shared
viewport change clears the selection. Use `ClearSelection` on any member
that shares the selection to clear it.

```xml
<Page.Resources>
    <charts:LineChartViewportGroup x:Key="TimelineWindow" IsSelectionSynchronized="True" />
</Page.Resources>

<!-- Inside a panel -->
<charts:LineChart x:Name="HeapChart" Data="{x:Bind HeapTimeline}"
                  ViewportGroup="{StaticResource TimelineWindow}" IsViewportInteractionEnabled="True" />
<charts:LineChart Data="{x:Bind BufferTimeline}"
                  ViewportGroup="{StaticResource TimelineWindow}" IsViewportInteractionEnabled="True" />
<charts:LineChartOverview Target="{x:Bind HeapChart}" />
```

An overview attached to a member previews that member's series across the
shared extent and navigates the whole group. The gallery's Memory Analysis page
uses one overview for its heap and buffer timelines.

#### Stacked area

```xml
<charts:LineChart Height="250" Mode="StackedArea" Data="{x:Bind Requests}"
                  AutomationProperties.Name="Requests by service" />
```

Use identical ordered X coordinates in every series, including timestamps
that represent the same instants. Supply a null Y at a missing measurement.
The gap also breaks bands higher in series order because their baselines are
unknown there. Lower bands can continue. A chart with mismatched X coordinates
displays a message until it receives aligned data.

Positive and negative values accumulate separately from zero. The axis includes
the cumulative totals, and inspection shows each series' original contribution.
Stacked area fills the bands by default. `Interpolation="Monotone"` eases each
interval consistently across bands so shared boundaries stay aligned.

### Compact sparkline

```xml
<charts:LiveAreaGraph x:Name="Trend" Height="56"
                      IsSparkline="True" ShowAreaFill="False" ShowLatestValueMarker="True"
                      Background="Transparent" BorderThickness="0"
                      AutomationProperties.Name="CPU usage history" />
```

Use the same `AddSeries` and `AppendSample` calls as for a full live graph.
`IsSparkline` hides the grid and lets the default template fit a shorter tile.
`ShowAreaFill` defaults to true. Set it to false for a line-only presentation.
`ShowLatestValueMarker` draws a dot for each series at its rendered right
endpoint, including during a transition. The fill and marker options work
in both presentations and can be updated through binding.

Set `Interpolation="Smooth"` for frequent updates. The curve starts each new
transition at the currently plotted value and carries its slope where the trend
continues without overshooting. The transition may stretch to match the observed
cadence, up to twice `TransitionDuration`. This can add a short visual lag.
`Eased` remains the default. A duration of zero gives immediate steps in both modes.

### Scatter chart

```xml
<charts:ScatterChart x:Name="Requests" Height="280"
                     XValueFormat="0' KB'" YValueFormat="0' ms'"
                     AutomationProperties.Name="Request size versus latency" />
```

```csharp
Requests.Data = new ScatterChartData(
[
    new ScatterChartSeries("Cached", [new(4, 12), new(16, 22), new(16, 31)]),
    new ScatterChartSeries("Uncached", [new(4, 48), new(16, 80, "Cold start"), new(64, 126)]),
]);
```

Supply finite numeric coordinates in any order. Points can share either
coordinate. Hover picks the nearest point within reach. Keyboard navigation
visits each point in X then Y order, including overlapping points.

Both Cartesian charts accept bindable `XAxis` and `YAxis` options. For example,
`new ChartAxisOptions(0, 100, "0'%'")` fixes the numeric range and formats
the tick labels. Use `ShowGridLines` to override the chart's grid setting for
one axis or `ShowLabels` to hide its labels. Numeric bounds apply to the numeric
axes. On a time-based line chart, the X options apply to UTC tick formatting
and visibility. The inspector uses the chart's existing value formats.

### Reference lines

Set `ChartAxisOptions.ReferenceLines` on a line or scatter chart's `XAxis`
or `YAxis`, or on a bar chart's `ValueAxis`. The line is perpendicular to
its axis, so the bar chart line rotates with the bars. A 100% stacked bar
uses values from 0 to 1 (for example, 0.85 for 85%).

```csharp
private ChartAxisOptions LatencyAxis { get; } = new()
{
    ReferenceLines = (ChartReferenceLine[])[new(80, "80 ms budget")],
};
```

```xml
<charts:LineChart Data="{x:Bind Latencies}" YAxis="{x:Bind LatencyAxis}" />
```

Use the `DateTimeOffset` overload for a time-based line chart's X axis.
Labels are optional. Use `Stroke`, `StrokeThickness`, and `IsStrokeDashed`
to style individual lines. Reference lines stay visible when axes are
hidden. They use the current scale and viewport, so values outside the
plot range are omitted. Set explicit axis bounds when you need to keep
an otherwise out-of-range limit in view. To update reference lines from
a binding, replace the axis options snapshot.

### Milestone markers

Bind `LineChart.Milestones` or `ScatterChart.Milestones` to a
`ChartMilestones` snapshot. A milestone with only X appears as a diamond at
the top of the plot. Supply Y to outline a specific point, including one
between samples. Use numeric X values on numeric charts and
`DateTimeOffset` on time-based line charts.

```csharp
private ChartMilestones Events { get; } = new(
    new ChartMilestone(new DateTimeOffset(2026, 1, 12, 9, 15, 0, TimeSpan.Zero), "Firmware rollout"));
```

```xml
<charts:LineChart Data="{x:Bind SensorData}" Milestones="{x:Bind Events}" />
```

Milestones follow the visible axes and zoom window without changing their
ranges. The chart can omit overlapping captions while keeping their
diamonds. Set `ChartMilestone.Brush` to customize a diamond or replace the
snapshot to update the events.

### Compact dashboard charts

Set `ChartAxisAppearance.Presentation` on a bar, line, scatter, spider, or
heatmap chart. `Standard` uses the chart and individual axis settings.
`Subtle` keeps configured labels and hides axis and grid lines. `Hidden`
also removes the labels and gives their space to the plot. These settings
preserve the axis ranges, tick formats, and inspector values.

Use `ChartAxisOptions` for individual axes. `ShowLabels`, `LabelOpacity`,
`ShowAxisLine`, and `ShowGridLines` control the axis appearance. The default
styles use Fluent secondary text and divider brushes. Override them with
`ChartAxisAppearance.LabelBrush` or `ChartAxisAppearance.LineBrush`.

```csharp
private BarChartData MemorySplit { get; } = new(["Installed memory"],
    [new("In use", [23.6]), new("Modified", [0.7]), new("Standby", [5.1]), new("Free", [2.6])]);
```

```xml
<charts:BarChart Orientation="Horizontal" Mode="Stacked100" InspectionMode="Category"
                 Height="48" MinHeight="0" Padding="0" BorderThickness="0" Background="Transparent"
                 SegmentGap="3" SegmentCornerRadius="4"
                 charts:ChartAxisAppearance.Presentation="Hidden"
                 Data="{x:Bind MemorySplit}" ValueFormat="0.#' GB'"
                 AutomationProperties.Name="Installed memory split" />
```

Bar charts use `CategoryAxis` for category labels and `ValueAxis` for
numeric ticks, the zero line, and the grid. The memory split uses the
[stacked bar styling options](#bar-chart) for its rounded ends and gaps.

Line and scatter charts expose
the options through `XAxis` and `YAxis`. Spider charts use `AxisOptions`
for spokes, rings, and labels. Heatmaps use `RowAxis` and `ColumnAxis` for
label visibility and opacity. See **Dashboard widgets** in the gallery.
For an edge-to-edge line or scatter plot, use `Presentation="Hidden"` and set
`Padding` and `BorderThickness` to zero. Use **Appearance and inspection**
on the chart pages to try the shared settings and the options each control
supports.

### Spider chart

```xml
<charts:SpiderChart Height="320" Data="{x:Bind ComparisonData}" />
```

```csharp
public SpiderChartData ComparisonData { get; } = new(
    [new("Battery", 0, 20) { ValueFormat = "0' hours'" },
     new("Display", 0, 10), new("Portability", 0, 10)],
    [new("Travel", [17, 6, 9]), new("Workstation", [10, 9, 5])]);
```

Each series supplies one value per spoke in axis order. A null value leaves
the adjoining edges open and disables the fill for that series. The chart
maps each value to its own axis range. The inspector reports the raw measurement.

### Sankey chart

```xml
<charts:SankeyChart Height="280" Data="{x:Bind Flows}" ValueFormat="N0" />
```

```csharp
public SankeyChartData Flows { get; } = new(
    [new("incoming", "Incoming"), new("review", "Review"), new("closed", "Closed")],
    [new("incoming", "review", 70), new("incoming", "closed", 30), new("review", "closed", 55)]);
```

Positive links must form a directed acyclic network. Node keys are unique and
ordinal. Use finite, non-negative values. Zero links and nodes without active
links are omitted. Each node must have finite inbound and outbound totals.
The chart keeps terminal nodes in the final column and centers smaller flows at
nodes where incoming and outgoing totals differ. Input order controls the
vertical order inside each column. Use `NodeWidth`, `NodeSpacing`, and
`ShowLabels` to adjust the layout. Links inherit the source node color unless
given a brush. Nodes use `ChartPalette.Colors` unless given a brush.

### Funnel chart

```xml
<charts:FunnelChart Height="240" Data="{x:Bind Conversion}" ValueFormat="N0" />
```

```csharp
public FunnelChartData Conversion { get; } = new(
    new("Started", 1200), new("Activated", 550), new("Subscribed", 160));
```

Stage widths are proportional to the largest value. Every stage retains its
input position, including zero values and increases. A tapered band narrows
along a smooth curve toward the next smaller stage. Set `IsTapered="False"` for
rectangular bands. Use `StageAlignment` to align the bands to the left,
center, or right. `StageSpacing` and `StageCornerRadius` customize the
individual bands. `ShowLabels` and `ChartPalette.Colors` control the
appearance. A stage brush takes precedence over the palette.

Use `Presentation="Flow"` for left-to-right ribbons. With cohort data, each
ribbon changes thickness with its own values. The inspector shows stage totals
and cohort counts. The legend supports the shared `ChartLegend` settings,
and its optional values show each cohort's count at the final stage.

```xml
<charts:FunnelChart Height="280" Presentation="Flow" Data="{x:Bind Cohorts}" />
```

```csharp
public FunnelChartData Cohorts { get; } = FunnelChartData.FromSeries(
    ["Logged", "Assigned", "Closed"],
    new("Web", [840, 545, 414]),
    new("Email", [450, 310, 212]));
```

Both diagrams support the shared inspector and context-menu provider. In a
funnel, the inspector includes conversion from the first and previous stages
when those baselines are positive.

### Heatmap

```xml
<charts:HeatmapChart Height="220" Data="{x:Bind LatencyData}" ValueFormat="0' ms'" />
```

```csharp
public HeatmapChartData LatencyData { get; } = new(
    new double?[,] { { 42, 65, null }, { 30, 0, 54 } },
    ["Americas", "Europe"], ["Morning", "Afternoon", "Evening"]);
```

Null cells leave blank space. Zero is a measured value and receives a color.
The default color scale follows the data. Set `ColorScale` to a
`HeatmapColorScale` with ascending stops to share a scale across heatmaps.
Use stepped stops for discrete activity bands.

For a contribution calendar with Sunday as the first day, make a matrix of
seven rows by the number of displayed weeks. Populate it by date and bind the
result with `KeepCellsSquare="True"`. Leave dates outside the range null.

```csharp
var firstSunday = start.AddDays(-(int)start.DayOfWeek);
var weeks = (end - firstSunday).Days / 7 + 1;
var cells = new HeatmapChartCell?[7, weeks];
var months = new string?[weeks];
for (var day = start; day <= end; day = day.AddDays(1))
{
    var week = (day - firstSunday).Days / 7;
    cells[(int)day.DayOfWeek, week] = new HeatmapChartCell(CountForDay(day),
        Timestamp: new DateTimeOffset(day.Ticks, TimeSpan.Zero));
    if (day == start || day.Day == 1)
    {
        months[week] = day.ToString("MMM");
    }
}
var calendar = new HeatmapChartData(cells, [null, "Mon", null, "Wed", null, "Fri", null], months);
```

### Treemap

```xml
<charts:TreemapChart x:Name="Folders" Height="360" ValueFormat="0.#' GB'"
                     AutomationProperties.Name="Workspace folder sizes in gigabytes" />
```

```csharp
Folders.Data = new HierarchyChartData(new HierarchyChartNode("workspace", "Workspace",
    new HierarchyChartNode("projects", "Projects", 54),
    HierarchyChartNode.CreateDeferredBranch("packages", "Packages", 40)));

// Bind the updated snapshot on the UI thread when the child summaries arrive.
Folders.Data = Folders.Data!.WithChildren("packages",
    new HierarchyChartNode("nuget", "NuGet", 24),
    new HierarchyChartNode("npm", "npm", 16));
```

Use unique keys throughout the hierarchy. Labels can repeat. Ordinary branches
sum their children. A deferred branch uses a caller-supplied total before child
nodes are available. `WithChildren` returns a new snapshot and recalculates
ancestor totals, so an estimate can be refined when details arrive. The caller
owns retrieval and can populate branches incrementally from a cached index or an
async scan.

`MaxDepth` defaults to three levels, starting with the root's children. The
chart frames each expanded branch, with the parent total in the header when it
fits and its children inset below. Hover a
tile or use the keyboard to read its path, value, and share of its parent. A
deferred tile shows that child details are pending.

`TileSpacing` sets the gap between tiles and `BranchPadding` sets the side
and bottom insets inside expanded branches. Both are measured in DIPs and
default to 2 and 8. Set `TileCornerRadius` to 0 for square corners, or use
a positive radius. Automatic rounding uses 4 DIPs for leaves and 6 DIPs for
expanded branches. `ShowValueLabels` toggles on-tile size labels, and
`ValueLabelOpacity` controls their emphasis from 0 to 1. Contrast themes use
full opacity, and the inspector always reports values.

Set `TreemapChart.Layout` to choose how siblings divide the available space.

- `TreemapLayouts.Squarified` sorts by value and favors compact rectangles. This is the default.
- `TreemapLayouts.SliceAndDice` preserves input order, alternating columns and rows at each level.
- `TreemapLayouts.Binary` splits roughly weight-balanced groups along the longer side of each rectangle.

A custom `ITreemapLayout` receives the input nodes, available bounds, and
one-based depth at each visible level. Return bounds in plot coordinates,
identified by the nodes' input indices. Placement order determines keyboard
inspection order. Treemap supplies spacing, branch expansion, and inspection.
The strategy runs on the UI thread when the chart lays out its tiles.

```csharp
using JPSoftworks.DesktopToolkit.Charts;
using Windows.Foundation;

// Assign new ReverseSlices() to TreemapChart.Layout.
sealed class ReverseSlices : ITreemapLayout
{
    public IReadOnlyList<TreemapPlacement> Arrange(
        IReadOnlyList<HierarchyChartNode> nodes, Rect bounds, int depth)
    {
        var reversed = nodes.Reverse().ToArray();
        return TreemapLayouts.SliceAndDice.Arrange(reversed, bounds, depth)
            .Select(tile => new TreemapPlacement(nodes.Count - 1 - tile.NodeIndex, tile.Bounds))
            .ToArray();
    }
}
```

### Icicle

```xml
<charts:IcicleChart Data="{x:Bind FolderData}" Height="360"
                     Orientation="Horizontal" ValueFormat="0.#' GB'"
                     AutomationProperties.Name="Workspace folder sizes by hierarchy level" />
```

Bind the same `HierarchyChartData` snapshot as a Treemap. Icicle draws the
root and up to `MaxDepth` levels below it. Levels run from top to bottom by
default, or from left to right with `Orientation="Horizontal"`. Each child
occupies its share of its parent's span. Deferred branches use their known
totals and reveal another level when the caller supplies children with
`WithChildren`. Hover a tile or focus the chart and use the arrow keys to
read its path, value, and share of its parent.

### Sunburst

```xml
<charts:SunburstChart Data="{x:Bind FolderData}" Height="400"
                      MaxDepth="3" StartAngle="-90" ValueFormat="0.#' GB'"
                      AutomationProperties.Name="Workspace folder sizes in concentric rings" />
```

The root occupies the center. Each ring represents another level, and each
segment occupies its share of its parent's angle. `MaxDepth` limits the rings
outside the root. `StartAngle` is measured in degrees clockwise from the right
and defaults to -90, so the first branch begins at the top. Deferred branches
occupy their known share until children arrive. Inspect any segment for its path,
value, and share of its parent, including segments too small for labels.

Treemap, Icicle, and Sunburst raise `NodeInvoked` when a tile or segment is tapped or activated
with Enter during keyboard inspection. A handler can set `Handled` to consume
the input and dismiss the inspector. Use `TryGetTileBounds` for the animation
anchor and assign a new `HierarchyChartData` rooted at the chosen branch.
The gallery demonstrates zooming in and returning to an ancestor through a
breadcrumb trail.

### Radial gauge

```xml
<charts:RadialGauge x:Name="Battery" Height="176" Maximum="100"
                    Value="68" ValueFormat="0'%'"
                    AutomationProperties.Name="Battery charge" />
```

```csharp
Battery.SetBands(
    new RadialGaugeBand(0, 20, Color.FromArgb(255, 212, 68, 78), "Low"),
    new RadialGaugeBand(20, 40, Color.FromArgb(255, 190, 125, 36), "Reserve"),
    new RadialGaugeBand(40, 100, Color.FromArgb(255, 43, 151, 110), "Normal"));
```

Use `Minimum`, `Maximum`, and `Value` to set or bind the range. The default
range is 0 to 1. `ValueFormat` formats the visible and accessible value label
without changing the numeric value exposed by UI Automation.

Call `SetBands` on the UI thread with finite, non-overlapping intervals in
the gauge's units. Bands are clipped to the visible range. Each starts at
its lower bound and ends before its upper bound, with the gauge maximum
included in a band that ends there. The active band's optional label is
visible alongside the value and available through UI Automation. Call
`SetBands()` to clear the bands.

Set `UseActiveBandColor="True"` to color the value arc from the current band.
Uncovered values and contrast themes use the normal indicator brush.
Set `StartAngle` in degrees clockwise from the right and `SweepAngle` to a
value greater than zero and up to 360 degrees. Both default to 180, which
draws the upper semicircle from left to right. `StartAngle="135"` with
`SweepAngle="270"` gives a wider arc with a gap at the bottom. Set
`IsDirectionReversed="True"` to swap the minimum and maximum endpoints along
the same arc. The fill, needle, and bands follow the reversed value direction.
`IsMirrored="True"` reflects the configured arc horizontally and can be used
with either value direction.
Set `IsValueLabelCentered="True"` to anchor the value and status labels
at the center of the circle, including when the arc is rotated or mirrored.

### Usage meter and doughnut

Bind `DoughnutGraph.Data` to a `DoughnutGraphData` snapshot and
`VerticalUsageBar.Data` to a `VerticalUsageBarData` snapshot. Each combines
series definitions and values in one update. Clearing the usage snapshot
reveals the meter's current `Value`. Use the streaming methods below for
producer-thread updates, and clear `Data` before switching to them.

```xml
<charts:VerticalUsageBar Orientation="Horizontal" IsCompact="True"
    Maximum="256" Value="164" SegmentGap="2"
    ShowLegend="True" ValueFormat="0' GB'" Data="{x:Bind StorageData}"
    AutomationProperties.Name="Workspace drive capacity" />
```

`IsCompact` uses a solid 8-DIP track. Use `TrackThickness` to change its
thickness, and `SegmentGap` to separate contributions. Stacks use the meter's
capacity, so the unused portion remains visible. Use `LegendOrientation`
for wrapping rows or a column, and `LegendPlacement` to choose a side.
`ShowLegendValues` toggles the values, while `ValueFormat` formats both the
legend and inspection values. `ValueText` remains a separate headline.

Bind gauge thresholds through `RadialGauge.BandsSource` and replace the array
when thresholds change. Bind both live graph bounds together through
`LiveAreaGraph.ValueRange`. `HistoryDuration`, `TransitionDuration`, `GridMode`,
`GridSpacing`, and `IsPaused` also accept bindings.

Bind `LiveAreaGraph.SeriesSource` to a `LiveGraphSeriesCollection` to define
the sample columns from a view model. Replace the snapshot on the UI thread.
Keep the names and order to restyle existing history. Changing names or order
clears history. Coordinate schema changes with the producer so its next
sample uses the new column order. Clearing `SeriesSource` returns to
imperative `AddSeries` setup. `AppendSample` works on a producer thread with
either setup.

```xml
<charts:LiveAreaGraph x:Name="Graph"
                      SeriesSource="{x:Bind ViewModel.LiveSeries, Mode=OneWay}" />
```

```xml
<charts:VerticalUsageBar Width="64" Height="180" Value="42" ValueText="42%" />
<charts:DoughnutGraph x:Name="Storage" Width="220" Height="270" ShowLegend="True"
                      CenterValue="42%" CenterLabel="used" />
<charts:DoughnutGraph x:Name="StoragePie" Height="250" Mode="Pie" ShowLegend="True" />
```

```csharp
Storage.AddSeries("Used", Color.FromArgb(255, 0, 120, 212));
Storage.AddSeries("Available", Color.FromArgb(255, 96, 117, 138));
Storage.SetValues(42, 58);
```

Use the same series and values for either shape. `Mode` defaults to
`Doughnut`, which uses `RingThickness`, `CenterValue`, and `CenterLabel`.
`Pie` draws contiguous sectors to the center. `GapAngle` applies to the
doughnut. `TransitionDuration` applies to both shapes.

`ShowLegend` enables the slice names and shares from the latest values.
The legend sits below the plot at narrow widths and beside it in wider
layouts. Scroll through longer legends to see every slice. `ValueFormat`
sets the numeric format for the input amount in the inspector.

Create controls and configure their properties and series on the UI thread.
`AppendSample` and both `SetValues` methods accept calls from producer threads;
`SetValues` coalesces pending updates. The library does not sample data for you.
Stop your own producers when the hosting page unloads.

Configure every series before sending values. Values must be finite and match
the configured series count. Meter and doughnut contributions must be non-negative.
Live graph timestamps must be chronological. Appending while paused is rejected.
`LiveAreaGraph.Clear()` removes history and preserves series; `DoughnutGraph.Clear()`
transitions slices to zero. Set `TransitionDuration` to zero for immediate updates.

Stacked meter `SetValues` deliberately does not change `Value` or `ValueText`.
Set them separately to expose the appropriate headline value to UI Automation.
Contributions use the meter's range units and add up to the used capacity; unused
capacity stays empty. For example, apps and media can share a 256 GB drive:

```csharp
StorageMeter.Orientation = Microsoft.UI.Xaml.Controls.Orientation.Horizontal;
StorageMeter.Maximum = 256;
StorageMeter.AddSeries("Apps", new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(255, 0, 120, 212)));
StorageMeter.AddSeries("Media", new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(255, 156, 93, 216)));
StorageMeter.SetValues(64, 96);
StorageMeter.Value = 160;
StorageMeter.ValueText = "160 GB";
```

## Legends

Set `ShowLegend` on a chart with a built-in legend. Series entries pack by
content width and wrap to fit. Use `ChartLegend.Layout` for a column or wrapping
rows, `ChartLegend.Alignment` for positioning, and `ChartLegend.ItemSpacing` for
the space between entries in the main direction. The defaults are `Auto`
layout, `Left` alignment, and 12 DIPs of spacing.
Long legends for bar, line, scatter, spider, and doughnut charts can scroll
within the chart.

Use `ChartLegend.Placement` to position a built-in legend at `Top`, `Bottom`,
`Start`, or `End` relative to the plot. `Start` and `End` follow the chart's
flow direction and default to a column. `Auto` keeps each chart's default,
including responsive doughnut placement and the usage bar's own
`LegendPlacement` setting.

```xml
<charts:LineChart Data="{x:Bind History}" ShowLegend="True"
    charts:ChartLegend.Placement="End"
    charts:ChartLegend.ItemSpacing="12"
    charts:ChartLegend.ValueMode="Value"
    charts:ChartLegend.HighlightOnHover="True" />
```

Set `HighlightOnHover="True"` to emphasize the matching series or slice while
pointing at a legend entry. It defaults to false. `ValueMode="Hidden"`
shows names alone. `Value` shows the last non-missing observation per line or bar
series, the last X/Y pair for scatter, or the current amount for slices and
usage segments. Doughnuts and pies show percentage shares by default.
Usage bars use `LegendOrientation` and `ShowLegendValues` in automatic layout
and default value mode. Spider legends show series names. Heatmap legends
are color keys and support `ChartLegend.Alignment`.

Set `ChartLegend.ItemTemplate` to change the contents of each entry. The
template receives a `ChartLegendItem` with `Name`, `ValueText`, `Fill`, `Stroke`,
and `IsStrokeDashed`. Use one-way bindings for entries whose values change. Heatmap color
keys expose the formatted scale value in `Name`.

Place a `ChartLegendView` anywhere in the page layout and point its `Chart`
property at a chart with a built-in legend. Set `ShowLegend="False"` to hide the
built-in copy. The detached view uses the chart's swatches, formatted values,
item template, and hover highlighting. With `ChartLegend.Layout="Auto"`, the
detached view uses a column. Give the view a constrained height when it should scroll.

```xml
<Grid ColumnDefinitions="*,Auto" ColumnSpacing="12">
    <charts:LineChart x:Name="Timeline" Data="{x:Bind History}" ShowLegend="False"
        charts:ChartLegend.ValueMode="Value" charts:ChartLegend.HighlightOnHover="True" />
    <charts:ChartLegendView Grid.Column="1" Chart="{x:Bind Timeline}"
        VerticalAlignment="Center" MaxHeight="220" />
</Grid>
```

```xml
<DataTemplate x:Key="LegendEntry" x:DataType="charts:ChartLegendItem">
    <StackPanel Orientation="Horizontal" Spacing="6">
        <Border Width="8" Height="8" CornerRadius="4"
                Background="{x:Bind Fill, Mode=OneWay}" />
        <TextBlock Text="{x:Bind Name, Mode=OneWay}" />
        <TextBlock Text="{x:Bind ValueText, Mode=OneWay}" />
    </StackPanel>
</DataTemplate>
```

Apply the resource with `charts:ChartLegend.ItemTemplate="{StaticResource LegendEntry}"`.

The gallery's **Legend options** let users change the supported settings.

## Data inspection

Hover the plot to inspect values. To inspect with the keyboard, tab to the
chart, use Left/Right to move through its values, Home/End to jump, and Escape
to dismiss the readout. The bar chart, line chart, pie, doughnut, usage meter,
and radial gauge also support tapping the plot.

- Bar charts display the category, series, and original value. A 100% stacked
  segment also shows its share. Set `InspectionMode` to `Category` to compare
  series in a category, or `Series` to compare categories in one series.
- Line charts snap to the nearest X with data and list the available series at
  that X. Missing values stay absent, and stacked area displays raw contributions.
  The inspection guide eases between samples over 120 milliseconds. Set
  `InspectionTransitionDuration` to tune that movement, or set it to zero for
  instant movement. The guide also moves instantly when Windows animations
  are disabled.
- Spider charts list the measurements for all series on the selected spoke.
- Heatmaps show the selected cell's label or date and its value. Use Left/Right
  to move between columns and Up/Down to move between rows.
- Live graphs show series values and the sample age at the inspection position.
- Pie and doughnut slices display their input value and their share of the total.
- Usage meters show the headline value or the selected contribution.
- Radial gauges show the current value, status, and range, or the limits of a
  selected threshold band.

Set `charts:ChartInspection.IsEnabled="False"` to disable inspection on any of
the controls. On `LiveAreaGraph`, `IsTooltipEnabled` also controls inspection.
Use `ChartInspection.HoverDelay` to adjust the initial pointer dwell on any
chart. The default is 200 milliseconds for popups and immediate for the
live graph's default style. Keyboard and touch inspection open immediately.
Use `ChartInspection.TransitionDuration` to change the card's 90-millisecond
slide or set it to zero for instant movement. Windows animation preferences
also apply. On a line chart, `InspectionTransitionDuration` controls the guide.

```xml
<charts:LineChart Data="{x:Bind Data}" ShowLegend="True"
    charts:ChartAxisAppearance.Presentation="Subtle"
    charts:ChartInspection.HoverDelay="0:0:0"
    charts:ChartInspection.TransitionDuration="0:0:0.09" />
```

Give each chart an `AutomationProperties.Name` that identifies its data.
Set `ChartInspection.ContentTemplate` to supply the readout. The template
receives `ChartInspectionContent` with formatted `Text`, formatted `Rows`
and their swatches, and structured `Values` containing the data and its items
where available. Use one-way binding to follow the current target.

```xml
<DataTemplate x:Key="Readout" x:DataType="charts:ChartInspectionContent">
    <TextBlock Text="{x:Bind Text, Mode=OneWay}" TextWrapping="Wrap" />
</DataTemplate>
```

Apply the resource with `charts:ChartInspection.ContentTemplate="{StaticResource Readout}"`.
The live graph creates the custom readout when inspection opens.

When replacing the chart's chrome, provide a `PART_Inspector` popup with a
`ContentControl` child. An optional `PART_InspectorReadout` (`ContentPresenter`)
in that child's template receives the formatted rows or the custom readout.
Its location within the template can vary.

To retain keyboard inspection when replacing `BarChart.Data`, supply stable
`categoryKeys` in the data constructor and set `Key` on each series. Category
inspection needs category keys, and series inspection needs series keys.

### Context menus for chart data

Attach an `IChartContextMenuProvider` with `ChartContextMenu.SetProvider`.
Right-click or press and hold a chart item to request a flyout. For keyboard
input, focus the chart, select an item with the arrow keys, and press Shift+F10
or the context menu key. For a shared menu for the whole chart, use WinUI's
`ContextFlyout`, which takes precedence when set.

```csharp
using JPSoftworks.DesktopToolkit.Charts;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

// In the page constructor after InitializeComponent:
// ChartContextMenu.SetProvider(Folders, new FolderMenuProvider(OpenFolder));
sealed class FolderMenuProvider(Action<HierarchyChartNode> openFolder) : IChartContextMenuProvider
{
    public FlyoutBase? CreateFlyout(ChartContextMenuRequest request)
    {
        if (request.Values is not [{ Item: HierarchyChartNode node }] || node.Children.Count == 0)
        {
            return null;
        }

        var menu = new MenuFlyout();
        var open = new MenuFlyoutItem { Text = "Open folder" };
        open.Click += (_, _) => openFolder(node);
        menu.Items.Add(open);
        return menu;
    }
}
```

The request carries the chart, an anchor position in chart coordinates, the
formatted inspection text, and the targeted data. Hierarchy charts include the
node. Bar charts use the active `InspectionMode`, including grouped bars, and
line charts include the available samples at the selected X. Scatter charts,
live graphs, pie and doughnut charts, usage meters, and radial gauges supply
their corresponding inspection data. Return null when the target has no
applicable commands.

Doughnut charts and usage meters follow the Windows animation setting. When
animations are disabled, updates apply immediately.

The default templates follow the application theme. Override standard control
brushes, border, corner radius, or the control template as appropriate. Some
chart options are CLR properties rather than dependency properties; configure
those in code when dynamic binding is required.

## Styling and theming

Use the guide for the control you want to style. The gallery's **Styling**
link shows the same guidance and a copyable XAML example next to each live
preview. For the timeline overview, open **Memory analysis**.

### Shared styling

- Set appearance through an app-level or page-level keyed `Style`. Put it in a `ResourceDictionary` with `xmlns:charts="using:JPSoftworks.DesktopToolkit.Charts"`, then assign it with `Style="{StaticResource YourStyleKey}"`. Use local properties or bindings for per-instance differences.
- Where the template binds them, `Background`, `BorderBrush`, `BorderThickness`, `CornerRadius`, `Padding`, and `Foreground` style the surface and text. `CornerRadius` rounds the frame. Chart-specific radius settings affect the plotted marks.
- Use theme resources for surfaces and text. Set `ChartPalette.Colors` on supported categorical charts. An explicit series brush or color takes priority in the standard themes. Check light, dark, and contrast themes with your actual data.
- `ChartAxisAppearance.Presentation` controls axis chrome on supported charts. `Hidden` gives the plot more space and keeps reference lines available. `ChartAxisOptions` provides per-axis settings. Use `ChartAxisAppearance.GridLineBrush` for Cartesian grid strokes and `ChartAxisAppearance.LineBrush` for baselines. Grid strokes use the chart's `BorderBrush` when no grid brush is supplied.
- Use `ChartLegend.Layout`, `Alignment`, `ItemSpacing`, `ValueMode`, and `HighlightOnHover` on supported series legends. `ChartInspection` controls inspector enablement, hover delay, and transition duration. Keep an `AutomationProperties.Name` that describes the data.
- To replace the chrome, start with the current control template in `Themes/Generic.xaml`. Keep the listed part names, types, and visual states for the features you use. Preserve `TemplateBinding` values and keyboard focus visuals, and give the plot a positive size. For most charts, a transparent `PART_Plot` background receives input. `PART_Inspector` is a `Popup` whose child is a `ContentControl`.
- Use regular setters before retemplating. The control renders its plot into the template host. Keep the inspector's `PART_InspectorReadout` if deriving from the supplied inspector template and retaining its swatches.

### BarChart

- **Appearance** Set `Orientation` and `Mode` for rows, columns, and grouped or stacked bars. A finite `BarSpacing` fills the category span with the requested gap. `SegmentGap`, `SegmentCornerRadius`, and `SegmentRounding` style stacks. `OuterCorners` keeps joined edges square and `AllCorners` rounds every segment.
- **Colors and labels** Use `BarChartSeries.Brush` to pin a series color or `ChartPalette.Colors` for theme-aware defaults. `CategoryAxis` controls category labels and `ValueAxis` controls numeric labels, grid lines, and reference lines. `ShowLegend` and `ChartLegend` control the series key.
- **Template parts** Keep `PART_Plot` (`Canvas`), `PART_Legend` (`Grid`) for the series key, `PART_Details` (`TextBlock`) for the inline readout, and `PART_Inspector` for the popup.
- **Gallery** In the gallery, compare **Compact CPU comparison** and **Sign-ups by platform**. The latter exposes gap and rounding in its sidebar.

```xml
<Style x:Key="CompactStack" TargetType="charts:BarChart">
    <Setter Property="Orientation" Value="Horizontal" />
    <Setter Property="Mode" Value="Stacked100" />
    <Setter Property="SegmentGap" Value="3" />
    <Setter Property="SegmentCornerRadius" Value="4" />
    <Setter Property="SegmentRounding" Value="AllCorners" />
    <Setter Property="charts:ChartAxisAppearance.Presentation" Value="Hidden" />
</Style>
```

### LineChart

- **Appearance** Use `ShowAreaFill`, `Interpolation`, and `Mode` to choose a line, smooth trend, or stacked area. Set `LineChartSeries.LineColor`, `FillColor`, `StrokeThickness`, `IsStrokeDashed`, and its `ShowAreaFill` override for individual series.
- **Colors and labels** Use `XAxis` and `YAxis` for independent labels and grid lines. Add targets through `ChartAxisOptions.ReferenceLines` and events through `Milestones`. `ShowLegend` and `ChartLegend` control the series key. `InspectionTransitionDuration` controls the inspection guide.
- **Template parts** Keep `PART_Plot` (`Canvas`), `PART_Legend` (`Grid`), `PART_Details` (`TextBlock`), and `PART_Inspector`.
- **Gallery** In the gallery, compare **Latency by request rate** with **Compact latency trend** and try the fill and interpolation options on **Sensor temperatures by time (UTC)**.

```xml
<Style x:Key="CompactTrend" TargetType="charts:LineChart">
    <Setter Property="MinHeight" Value="0" />
    <Setter Property="Padding" Value="0" />
    <Setter Property="BorderThickness" Value="0" />
    <Setter Property="Background" Value="Transparent" />
    <Setter Property="ShowAreaFill" Value="True" />
    <Setter Property="Interpolation" Value="Monotone" />
    <Setter Property="ShowLegend" Value="False" />
    <Setter Property="charts:ChartAxisAppearance.Presentation" Value="Hidden" />
</Style>
```

### LineChartOverview

- **Appearance** Set `Height`, `Padding`, `Background`, `BorderBrush`, `BorderThickness`, and `CornerRadius` to fit the overview into a dashboard. `Foreground` colors the viewport outline.
- **Colors and labels** Bind `Target` to the main `LineChart`. The overview takes trace data and series colors from that chart. Assign the series colors or `ChartPalette.Colors` on the target.
- **Template parts** Keep `PART_Track` (`Canvas`) and give it a hit-testable background, such as `Transparent`, so panning and resize handles can receive input.
- **Gallery** In the gallery, open **Memory analysis** and use the overview above the linked charts. The viewport outline and handles remain visible while zooming.

```xml
<Style x:Key="TimelineNavigator" TargetType="charts:LineChartOverview">
    <Setter Property="Height" Value="52" />
    <Setter Property="Padding" Value="8,4" />
    <Setter Property="CornerRadius" Value="6" />
    <Setter Property="Foreground" Value="{ThemeResource AccentTextFillColorPrimaryBrush}" />
</Style>
```

### ScatterChart

- **Appearance** Set `MarkerSize` for the marker diameter. Use `ScatterChartSeries.MarkerColor` for an explicit color or `ChartPalette.Colors` for defaults.
- **Colors and labels** Use `XAxis` and `YAxis` for the separate dimensions. Reference lines can mark limits on either axis and `Milestones` can highlight individual points. `ShowLegend` and `ChartLegend` control the series key.
- **Template parts** Keep `PART_Plot` (`Canvas`), `PART_Legend` (`Grid`), `PART_Details` (`TextBlock`), and `PART_Inspector`.
- **Gallery** In the gallery, change **Marker size** in **Request size versus latency** and compare it with **Scatter with subtle axes**.

```xml
<Style x:Key="CompactScatter" TargetType="charts:ScatterChart">
    <Setter Property="MarkerSize" Value="6" />
    <Setter Property="ShowGrid" Value="False" />
    <Setter Property="charts:ChartAxisAppearance.Presentation" Value="Subtle" />
    <Setter Property="charts:ChartLegend.ItemSpacing" Value="8" />
</Style>
```

### SpiderChart

- **Appearance** Set `RingCount` for the rings and `ShowAreaFill` for filled profiles. `SpiderChartSeries.LineColor` and `FillColor` override the palette and derived fill.
- **Colors and labels** Use `ChartAxisAppearance.LabelBrush` and `LineBrush` for labels and radial guides. `AxisOptions` controls label visibility and opacity as well as the radial guides. This legend displays series names.
- **Template parts** Keep `PART_Plot` (`Canvas`), `PART_Legend` (`Grid`), and `PART_Inspector`.
- **Gallery** In the gallery, toggle the area fill and use **Options** to compare the axes and palettes.

```xml
<Style x:Key="ProfileRadar" TargetType="charts:SpiderChart">
    <Setter Property="RingCount" Value="4" />
    <Setter Property="ShowAreaFill" Value="False" />
    <Setter Property="charts:ChartAxisAppearance.Presentation" Value="Subtle" />
    <Setter Property="charts:ChartLegend.Alignment" Value="Center" />
</Style>
```

### HeatmapChart

- **Appearance** Use `ColorScale` for continuous or stepped values. `HeatmapColorScale.FromPalette` pairs a theme-specific ramp such as `ChartPalette.ActivityGreen` with numeric stops.
- **Colors and labels** `KeepCellsSquare`, `CellSpacing`, and `CellCornerRadius` shape the grid. Use `RowAxis` and `ColumnAxis` for label visibility and opacity. `ShowLegend` toggles the color key and `ChartLegend.Alignment` positions it.
- **Template parts** Keep `PART_Plot` (`Canvas`) for the drawing surface, `PART_Legend` (`Grid`) for the color key, and `PART_Inspector`.
- **Gallery** In the gallery, compare **Daily contributions** and **Request latency tiles**, or change the scale and cell geometry in **Request latency by region**.

```xml
<Style x:Key="ActivityTiles" TargetType="charts:HeatmapChart">
    <Setter Property="KeepCellsSquare" Value="True" />
    <Setter Property="CellSpacing" Value="3" />
    <Setter Property="CellCornerRadius" Value="2" />
    <Setter Property="charts:ChartLegend.Alignment" Value="Right" />
</Style>
```

### SankeyChart

- **Appearance** Set `NodeWidth` and `NodeSpacing` to change the balance between nodes and ribbons. `ShowLabels` controls the node text and `ValueFormat` controls the displayed values.
- **Colors and labels** Use `ChartPalette.Colors` for node defaults. Specify `SankeyChartNode.Brush` for a node or `SankeyChartLink.Brush` for a ribbon. A ribbon without its own brush takes its color from the source node.
- **Template parts** Keep `PART_Plot` (`Canvas`) and `PART_Inspector`.
- **Gallery** In the gallery, use the node width, spacing, and label controls to compare a compact flow with a more open layout.

```xml
<Style x:Key="CompactFlows" TargetType="charts:SankeyChart">
    <Setter Property="NodeWidth" Value="12" />
    <Setter Property="NodeSpacing" Value="8" />
    <Setter Property="ShowLabels" Value="True" />
    <Setter Property="Padding" Value="8" />
</Style>
```

### FunnelChart

- **Appearance** `Presentation=Stages` uses `StageAlignment`, `IsTapered`, `StageSpacing`, and `StageCornerRadius` to shape each band. Use `Presentation=Flow` for connected cohort ribbons and `ShowLegend` for their key.
- **Colors and labels** Set `ShowLabels` and `ValueFormat` for stage labels. Use `ChartPalette.Colors` for defaults or assign `FunnelChartStage.Brush` and `FunnelChartSeries.Brush` for explicit colors.
- **Template parts** Keep `PART_Plot` (`Canvas`), `PART_Legend` (`Grid`) for the Flow legend, and `PART_Inspector`.
- **Gallery** In the gallery, adjust **Trial to paid subscription** in its sidebar and compare it with **Support requests by channel**.

```xml
<Style x:Key="RoundedFunnel" TargetType="charts:FunnelChart">
    <Setter Property="StageAlignment" Value="Left" />
    <Setter Property="IsTapered" Value="True" />
    <Setter Property="StageSpacing" Value="6" />
    <Setter Property="StageCornerRadius" Value="6" />
</Style>
```

### TreemapChart

- **Appearance** `TileSpacing` separates neighbors, `BranchPadding` insets nested tiles, and `TileCornerRadius` rounds tiles. Use `MaxDepth` and `Layout` to control the level of detail and packing algorithm.
- **Colors and labels** Set `ShowValueLabels` and `ValueLabelOpacity` for secondary values. `ValueFormat` formats them, `Foreground` sets label color, and `ChartPalette.Colors` supplies branch hues.
- **Template parts** Keep `PART_Plot` (`Canvas`) and `PART_Inspector`.
- **Gallery** In the gallery, use the layout, depth, tile spacing, and label options on the folder-size sample.

```xml
<Style x:Key="NestedTiles" TargetType="charts:TreemapChart">
    <Setter Property="MaxDepth" Value="3" />
    <Setter Property="TileSpacing" Value="2" />
    <Setter Property="BranchPadding" Value="4" />
    <Setter Property="TileCornerRadius" Value="4" />
    <Setter Property="ShowValueLabels" Value="True" />
    <Setter Property="ValueLabelOpacity" Value="0.65" />
</Style>
```

### IcicleChart

- **Appearance** `Orientation` selects the depth direction and `MaxDepth` sets the number of visible levels. Use `Padding` to inset the whole chart.
- **Colors and labels** `ChartPalette.Colors` supplies branch hues. `Foreground` colors labels. `ShowValueLabels` toggles the values inside tiles and `ValueFormat` formats them.
- **Template parts** Keep `PART_Plot` (`Canvas`) and `PART_Inspector`.
- **Gallery** In **Workspace folder sizes**, switch **Level direction** and **Visible levels**. Use **Options** to compare the palettes.

```xml
<Style x:Key="HorizontalHierarchy" TargetType="charts:IcicleChart">
    <Setter Property="Orientation" Value="Horizontal" />
    <Setter Property="MaxDepth" Value="3" />
    <Setter Property="Padding" Value="8" />
</Style>
```

### SunburstChart

- **Appearance** `StartAngle` rotates the ring layout and `MaxDepth` limits the visible hierarchy. Labels follow the arc where they fit.
- **Colors and labels** Use `ChartPalette.Colors` for branch hues, `Foreground` for text, and `ValueFormat` for center and inspected values.
- **Template parts** Keep `PART_Plot` (`Canvas`) and `PART_Inspector`.
- **Gallery** In the gallery, rotate the folder-size Sunburst and try showing fewer levels before choosing colors under **Options**.

```xml
<Style x:Key="RotatedHierarchy" TargetType="charts:SunburstChart">
    <Setter Property="StartAngle" Value="0" />
    <Setter Property="MaxDepth" Value="3" />
    <Setter Property="Padding" Value="8" />
</Style>
```

### LiveAreaGraph

- **Appearance** Use `IsSparkline` for a minimal plot. Set `ShowAreaFill`, `ShowLatestValueMarker`, and `Interpolation` independently. The standard plot exposes `GridMode` and `GridSpacing`.
- **Colors and labels** Supply stroke and fill colors, edge thickness, and edge style with `LiveGraphSeries`. Set `Background`, `BorderBrush`, `BorderThickness`, and `CornerRadius` for the frame. `TooltipValueFormat` and `TooltipValueSuffix` format the inspector.
- **Template parts** Keep `PART_CanvasHost` (`Grid`) for the Win2D surface. Retain `PART_Tooltip` (`Border`), `PART_TooltipTime` (`TextBlock`), and `PART_TooltipValues` (`StackPanel`) for the live inspection card. The `PresentationStates` group switches between `Standard` and `Sparkline`.
- **Gallery** In the gallery, compare **Solid, dashed, and fill-only series** with the **CPU dashboard tile** sparkline.

```xml
<Style x:Key="TileSparkline" TargetType="charts:LiveAreaGraph">
    <Setter Property="Height" Value="72" />
    <Setter Property="IsSparkline" Value="True" />
    <Setter Property="ShowAreaFill" Value="True" />
    <Setter Property="ShowLatestValueMarker" Value="True" />
    <Setter Property="Background" Value="Transparent" />
    <Setter Property="BorderThickness" Value="0" />
</Style>
```

### VerticalUsageBar

- **Appearance** Use `IsCompact` for solid segments and `TrackThickness` for the cross-axis size. Set `SegmentGap` for separation and `CornerRadius` for the track ends. In the standard presentation, `FillOpacity`, `EdgeLineThickness`, `ShowGrid`, and `GridBrush` control the fill and guides.
- **Colors and labels** `IndicatorBrush` colors a single-value meter. Assign `VerticalUsageBarSeries.IndicatorBrush` for individual segments. Use `ShowLegend`, `LegendPlacement`, `LegendOrientation`, `ShowLegendValues`, and the shared `ChartLegend` settings for the key.
- **Template parts** Keep `PART_Track` (`FrameworkElement`) and `PART_IndicatorHost` (`Canvas`) for the meter. Wrap the track in `PART_TrackFrame` (`Border`) to retain `TrackThickness` and compact end rounding. The default `MeterRoot` (`Grid`) receives orientation-specific minimum sizes. Retain `PART_Grid` (`Canvas`), `PART_ValueText` (`TextBlock`), `PART_SegmentHighlight` (`Border`), `PART_Legend` (`ContentControl`), and `PART_Inspector` to preserve those features. Keep `Vertical` and `Horizontal` in `OrientationStates` and `Standard` and `Compact` in `PresentationStates`.
- **Gallery** In the gallery, compare the compact and thick storage samples, which use the same data.

```xml
<Style x:Key="UsageSegments" TargetType="charts:VerticalUsageBar">
    <Setter Property="Orientation" Value="Horizontal" />
    <Setter Property="IsCompact" Value="True" />
    <Setter Property="TrackThickness" Value="22" />
    <Setter Property="SegmentGap" Value="2" />
    <Setter Property="CornerRadius" Value="6" />
    <Setter Property="ShowLegend" Value="True" />
</Style>
```

### RadialGauge

- **Appearance** `StartAngle` and `SweepAngle` define the arc. Set `ArcThickness` to a positive size in DIPs to override automatic thickness. `IsDirectionReversed` reverses the value progression and `IsMirrored` mirrors the arc horizontally. `IsValueLabelCentered` anchors the value and status to the circle center.
- **Colors and labels** Set `TrackBrush` and `IndicatorBrush`, or supply `RadialGaugeBand` colors through `BandsSource`. `UseActiveBandColor` applies the matching threshold color to the indicator. Format the value with `ValueFormat`.
- **Template parts** Keep `PART_Plot` (`Canvas`), `PART_Labels` (`FrameworkElement`), `PART_ValueLabel` (`TextBlock`), `PART_StatusLabel` (`TextBlock`), and `PART_Inspector`. Keep a `TranslateTransform` on `PART_Labels` for centering. `ArcStates` contains `SemicircleArc`, `CustomArc`, and `CenteredArc`.
- **Gallery** In **Battery charge**, change the arc and toggle the center label, threshold bands, and matching threshold color.

```xml
<Style x:Key="StatusGauge" TargetType="charts:RadialGauge">
    <Setter Property="StartAngle" Value="135" />
    <Setter Property="SweepAngle" Value="270" />
    <Setter Property="IsValueLabelCentered" Value="True" />
    <Setter Property="UseActiveBandColor" Value="True" />
    <Setter Property="ValueFormat" Value="0'%'" />
</Style>
```

### DoughnutGraph

- **Appearance** `RingThickness` and `GapAngle` apply to a doughnut. Set `Mode=Pie` for a solid pie with adjoining slices. Use `CenterLabel` and `CenterValue` for the ring's center text.
- **Colors and labels** Pass slice colors through `DoughnutGraphSeries`. Use `ShowLegend` and `ChartLegend` to control placement, values, and hover highlighting. `TransitionDuration` sets how quickly changing slice values settle.
- **Template parts** Keep `PART_CanvasHost` (`Grid`), `PART_Legend` (`ScrollViewer`), and `PART_Inspector`. `ShapeStates` switches between `Doughnut` and `Pie`. Keep `LegendBelow`, `LegendAbove`, `LegendStart`, and `LegendBeside` in `LegendStates` to support placement.
- **Gallery** In the gallery, compare **Storage composition as a doughnut** with the pie, then use **Customize ring thickness and slice gaps**.

```xml
<Style x:Key="RingSummary" TargetType="charts:DoughnutGraph">
    <Setter Property="RingThickness" Value="18" />
    <Setter Property="GapAngle" Value="2" />
    <Setter Property="ShowLegend" Value="True" />
    <Setter Property="charts:ChartLegend.ItemSpacing" Value="8" />
    <Setter Property="charts:ChartLegend.HighlightOnHover" Value="True" />
</Style>
```

## License

The squarified layout is adapted from DevDrive Manager. Its MIT notice ships in
`licenses/DevDrive.LICENSE.txt` in the NuGet package. [Clint Rutkas's layout source](https://github.com/crutkas/devdrive/blob/d08533898463055ae0a22f8210dd68fbef19ef81/DevDriveStorage/TreemapLayout.cs)

Apache-2.0. See the included LICENSE file.
