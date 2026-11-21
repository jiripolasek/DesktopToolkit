// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using JPSoftworks.DesktopToolkit.Controls.Demo.Controls;
using JPSoftworks.DesktopToolkit.Controls.Demo.Pages;
using JPSoftworks.DesktopToolkit.Controls.SearchableFlyouts.Samples;
using JPSoftworks.DesktopToolkit.Controls.Samples;
using JPSoftworks.DesktopToolkit.Gallery.UI;
using JPSoftworks.DesktopToolkit.Styles.Samples;
using JPSoftworks.DesktopToolkit.ToastWindows.Samples;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace JPSoftworks.DesktopToolkit.Gallery;

internal sealed record GalleryComponent(
    string Id,
    string Title,
    string Description,
    string Glyph,
    string SearchTerms,
    string ApiNamespace,
    string ApiNotes,
    GalleryLink[] Links,
    GalleryExample[] Examples)
{
    public GalleryDestination Destination => new(
        this.Title, typeof(ControlPage), this.Description, this.SearchTerms, ComponentId: this.Id, Glyph: this.Glyph);
}

internal sealed record GalleryDestination(
    string Title,
    Type PageType,
    string Description,
    string SearchTerms,
    string? ComponentId = null,
    string? Glyph = null,
    string? IconResource = null)
{
    public string Id => this.ComponentId ?? this.PageType.Name;

    [WinRT.DynamicWindowsRuntimeCast(typeof(DataTemplate))]
    public IconElement CreateIcon()
    {
        if (this.IconResource is { } resource
            && Application.Current.Resources.TryGetValue(resource, out var value)
            && value is DataTemplate template
            && GalleryIcon.CreateIcon(template, 16) is { } icon)
        {
            return icon;
        }

        return new FontIcon { Glyph = this.Glyph ?? "\uE9D9", FontSize = 16 };
    }
}

internal sealed record GallerySection(string Title, GalleryDestination[] Destinations);

internal sealed record GalleryExample(
    string Title,
    string Description,
    Func<Page> CreatePreview,
    Func<Page, FrameworkElement>? TakeSampleControls = null);

internal static class GalleryCatalog
{
    public const string RepositoryUrl = GalleryLink.RepositoryUrl;

    public static GalleryComponent[] Components { get; } =
    [
        new(
            "date-range-picker", "Date range picker", "Commit inclusive dates with presets and an optional previous-period comparison.",
            "\uE787", "DateRangePicker calendar date ranges reporting filter presets",
            "JPSoftworks.DesktopToolkit.Controls",
            "Bind Selection in two-way mode to receive the selected dates and optional comparison together.", [
                GalleryLink.ForComponentDocumentation("date-range-picker"),
                GalleryLink.ForComponentSource("DateRangePicker"),
                GalleryLink.ForNuGetPackage("JPSoftworks.DesktopToolkit.Controls.DateRangePicker"),
            ], [
                new("Reporting dates", "Choose dates or a preset, then apply the selection.", () => new DateRangePickerExample()),
            ]),
        new(
            "info-label", "Help Tooltips", "Add a tooltip icon beside a label or other content.", "\uE946",
            "InfoLabel info label information help tooltip caption field", "JPSoftworks.DesktopToolkit.Controls",
            "Set InfoLabel.Description to the help text and place the control beside the field or label it describes.", [
                GalleryLink.ForComponentDocumentation("info-label"),
                GalleryLink.ForComponentSource("InfoLabel"),
                GalleryLink.ForNuGetPackage("JPSoftworks.DesktopToolkit.Controls.InfoLabel"),
            ], [
                new("Help beside a field", "Hover over the icon or reach it with Tab.", () => new InfoLabelExample()),
            ]),
        new(
            "key-visual", "Keyboard Shortcuts", "Display themed keycaps and let users edit keyboard shortcuts.", "\uE765",
            "KeyVisual keyboard keycap shortcut hotkey glyph modifier editor capture", "JPSoftworks.DesktopToolkit.Controls",
            "Use ShortcutPresenter to display a shortcut and ShortcutEditor to capture and validate a new one.", [
                GalleryLink.ForComponentDocumentation("key-visual"),
                GalleryLink.ForComponentSource("KeyVisual"),
                GalleryLink.ForNuGetPackage("JPSoftworks.DesktopToolkit.Controls.KeyVisual"),
            ], [
                new(
                    "Keycap styles", "Compare keycap sizes, text, and validation states.", () => new KeyVisualExample(),
                    preview => ((KeyVisualExample)preview).TakeSampleControls()),
                new(
                    "Shortcut settings", "Record the other action's shortcut to try replacing it. Ctrl+P is reserved for printing.",
                    () => new ShortcutEditorExample(), preview => ((ShortcutEditorExample)preview).TakeSampleControls()),
                new("Command list", "Align compact keycaps at the trailing edge of command rows.", () => new ShortcutCommandListExample()),
                new("Shortcut help", "Align compact keycaps in a reference list.", () => new ShortcutHelpExample()),
                new(
                    "Async shortcut checks",
                    "In Open launcher, record Ctrl+P for a delayed error or Ctrl+K for a conflict. Switch to Ctrl+J while a check is pending to discard its result.",
                    () => new AsyncShortcutEditorExample()),
            ]),
        new(
            "searchable-flyouts", "Menus & Dropdowns",
            "Add search to command menus and single-selection dropdowns, including lists with thousands of items.", "\uE721",
            "SearchableMenuFlyout SearchableItemsFlyout SearchableDropDown searchable flyouts combo box filter input",
            "JPSoftworks.DesktopToolkit.Controls",
            "Use SearchableMenuFlyout for searchable commands. Bind SearchableDropDown.ItemsSource for a selection list and set ItemTemplate to customize its rows.",
            [
                GalleryLink.ForComponentDocumentation("searchable-flyouts"),
                GalleryLink.ForComponentSource("SearchableFlyouts"),
                GalleryLink.ForNuGetPackage("JPSoftworks.DesktopToolkit.Controls.SearchableFlyouts"),
            ], [
                new(
                    "SearchableMenuFlyout", "Filter native commands, toggles, and nested menus. Try duplicate or bookmark.",
                    () => new SearchableMenuFlyoutExample()),
                new(
                    "SearchableItemsFlyout", "Search document commands and 10,000 recent files. Try location or recent 042.",
                    () => new SearchableItemsFlyoutExample()),
                new(
                    "Right-click context menu", "Right-click Project notes.docx and search for location to find Copy path.",
                    () => new SearchableContextMenuExample()),
                new(
                    "Search a submenu", "Open Document formats and search for portable. Each submenu has its own filter.",
                    () => new SearchableSubmenuExample()),
                new(
                    "Replace a file menu",
                    "Compare the built-in and searchable versions of the same file menu. Both share commands and favorite state.",
                    () => new MenuFlyoutReplacementExample()),
                new(
                    "SearchableDropDown", "Filter a list and commit a single selection with the keyboard or pointer.",
                    () => new SearchableDropDownExample()),
                new(
                    "SearchableDropDown with icons", "Filter save locations by name. The selected location keeps its icon.",
                    () => new SearchableDropDownIconsExample()),
            ]),
        new(
            "toasts", "Toast Windows", "Show acrylic toast windows that follow the Windows on-screen indicator position.", "\uE91C",
            "Toasts ToastWindow toast notification position acrylic banner overlay", "JPSoftworks.DesktopToolkit.ToastWindows",
            "Create a ToastWindow for the calling window. Set Position to an explicit placement or UseSystemSettings to follow Windows.", [
                GalleryLink.ForComponentDocumentation("toasts"),
                GalleryLink.ForComponentSource("Toasts"),
                GalleryLink.ForNuGetPackage("JPSoftworks.DesktopToolkit.ToastWindows"),
            ], [
                new(
                    "Toast preview", "Show a toast with or without an action using the preview options.", () => new ToastWindowExample(),
                    preview => ((ToastWindowExample)preview).TakeSampleControls()),
                new(
                    "Toast position setting", "Save a position or follow the Windows on-screen indicator setting.",
                    () => new ToastPositionSettingsExample()),
            ]),
    ];

    private static GalleryDestination[] Controls { get; } = Components.Select(component => component.Destination).ToArray();

    private static GalleryDestination[] Styles { get; } =
    [
        new(
            "Search box", typeof(SettingsSearchBoxStylePage), "Style a WinUI search field for a settings page or title bar.",
            "styles PowerToys Command Palette settings search box AutoSuggestBox", Glyph: "\uE721"),
    ];

    private static GalleryDestination[] Charts { get; } =
    [
        new(
            "Live area graph", typeof(LiveAreaGraphPage), "Show live readings or a compact sparkline.", "live streaming sparkline",
            IconResource: "LiveAreaGraphIcon"),
        new(
            "Line chart", typeof(LineChartPage), "Plot numeric or time-based series with zoom and selection.",
            "timeline time series stacked area", IconResource: "LineChartIcon"),
        new(
            "Scatter chart", typeof(ScatterChartPage), "Compare paired measurements and spot outliers.", "points numeric",
            IconResource: "ScatterChartIcon"),
        new(
            "Spider chart", typeof(SpiderChartPage), "Compare several metrics on shared radial axes.", "radar",
            IconResource: "SpiderChartIcon"),
        new(
            "Heatmap", typeof(HeatmapChartPage), "Show a matrix of values or a calendar of activity.", "matrix calendar contributions",
            IconResource: "HeatmapChartIcon"),
        new(
            "Sankey chart", typeof(SankeyChartPage), "Follow weighted flows between sources and destinations.", "flow network ribbons",
            IconResource: "SankeyChartIcon"),
        new(
            "Funnel chart", typeof(FunnelChartPage), "Compare counts and conversion through ordered stages.", "conversion stages pipeline",
            IconResource: "FunnelChartIcon"),
        new(
            "Treemap", typeof(TreemapChartPage), "Compare folder sizes and drill into nested groups.", "tree hierarchy folder size",
            IconResource: "TreemapIcon"),
        new(
            "Icicle", typeof(IcicleChartPage), "Explore a hierarchy in horizontal or vertical bands.", "tree hierarchy folder size",
            IconResource: "IcicleIcon"),
        new(
            "Sunburst", typeof(SunburstChartPage), "Explore nested groups in concentric rings.", "tree hierarchy radial folder size",
            IconResource: "SunburstIcon"),
        new(
            "Bar chart", typeof(BarChartPage), "Compare categories with grouped, stacked, or 100% bars.", "stacked grouped columns",
            IconResource: "BarChartIcon"),
        new(
            "Usage bar", typeof(VerticalUsageBarPage), "Show capacity or composition in a compact meter.",
            "segmented meter storage composition", IconResource: "UsageBarIcon"),
        new(
            "Radial gauge", typeof(RadialGaugePage), "Show a value along an arc with threshold bands.", "meter dial threshold",
            IconResource: "RadialGaugeIcon"),
        new(
            "Doughnut and pie", typeof(DoughnutGraphPage), "Show parts of a whole as a ring or a filled pie.", "donut pie slices",
            IconResource: "DoughnutIcon"),
        new(
            "Chart legends", typeof(ChartLegendsPage), "Move a chart legend or place it in a separate panel.",
            "legend placement detached sidebar series key", Glyph: "\uE8FD"),
    ];

    private static GalleryDestination[] Scenarios { get; } =
    [
        new(
            "Dashboard widgets", typeof(DashboardWidgetsPage), "Build compact tiles for memory and service metrics.", "tiles sparkline",
            Glyph: "\uE8A9"),
        new(
            "System monitor", typeof(SystemMonitorPage), "Chart live CPU, memory, and network activity.", "CPU memory performance",
            IconResource: "SystemMonitorIcon"),
        new(
            "Memory analysis", typeof(MemoryAnalysisPage), "Explore linked heap timelines and inspect a selected range.",
            "heap timeline zoom selection", IconResource: "MemoryAnalysisIcon"),
        new(
            "Sales performance", typeof(SalesPerformancePage), "Compare revenue, monthly variance, and return rates.",
            "business revenue dashboard", IconResource: "SalesPerformanceIcon"),
        new(
            "Web analytics", typeof(WebAnalyticsPage), "Explore traffic, acquisition, and checkout for a fictional shop.",
            "google analytics sessions ecommerce conversion engagement", IconResource: "LineChartIcon"),
    ];

    public static GallerySection[] Sections { get; } =
    [
        new("Controls", Controls),
        new("Styles", Styles),
        new("Charts", Charts),
        new("Scenarios", Scenarios),
    ];

    private static GalleryDestination[] SearchDestinations { get; } =
    [
        .. Controls,
        .. Styles,
        Charts.Single(chart => chart.PageType == typeof(LineChartPage)) with { Title = "Charts", SearchTerms = "charts graphs" },
        .. Charts,
        .. Scenarios,
    ];

    public static IEnumerable<GalleryDestination> Search(string query)
    {
        string[] words = query.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return [];
        }

        return SearchDestinations
            .Where(destination => words.All(word =>
                destination.Title.Contains(word, StringComparison.OrdinalIgnoreCase)
                || destination.SearchTerms.Contains(word, StringComparison.OrdinalIgnoreCase))).OrderByDescending(destination =>
                destination.Title.Equals(query.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
