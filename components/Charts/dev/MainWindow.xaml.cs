// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using JPSoftworks.DesktopToolkit.Controls.Demo.Pages;
using JPSoftworks.DesktopToolkit.Controls.Demo.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Windows.Graphics;

namespace JPSoftworks.DesktopToolkit.Controls.Demo;

public sealed partial class MainWindow : Window
{
    private static readonly (string Tag, string Name, Type Page)[] Destinations =
    [
        ("overview", "Overview", typeof(OverviewPage)),
        ("live", "Live area graph", typeof(LiveAreaGraphPage)),
        ("line-chart", "Line chart", typeof(LineChartPage)),
        ("scatter-chart", "Scatter chart", typeof(ScatterChartPage)),
        ("spider-chart", "Spider chart", typeof(SpiderChartPage)),
        ("heatmap", "Heatmap", typeof(HeatmapChartPage)),
        ("sankey", "Sankey chart", typeof(SankeyChartPage)),
        ("funnel", "Funnel chart", typeof(FunnelChartPage)),
        ("treemap", "Treemap", typeof(TreemapChartPage)),
        ("icicle", "Icicle", typeof(IcicleChartPage)),
        ("sunburst", "Sunburst", typeof(SunburstChartPage)),
        ("bar-chart", "Bar chart", typeof(BarChartPage)),
        ("bar", "Usage bar", typeof(VerticalUsageBarPage)),
        ("gauge", "Radial gauge", typeof(RadialGaugePage)),
        ("doughnut", "Doughnut and pie", typeof(DoughnutGraphPage)),
        ("legends", "Chart legends", typeof(ChartLegendsPage)),
        ("dashboard-widgets", "Dashboard widgets", typeof(DashboardWidgetsPage)),
        ("monitor", "System monitor", typeof(SystemMonitorPage)),
        ("memory-analysis", "Memory analysis", typeof(MemoryAnalysisPage)),
        ("sales-performance", "Sales performance", typeof(SalesPerformancePage)),
        ("web-analytics", "Web analytics", typeof(WebAnalyticsPage)),
        ("settings", "Settings", typeof(SettingsPage)),
    ];

    public MainWindow()
    {
        this.InitializeComponent();
        this.ExtendsContentIntoTitleBar = true;
        this.SetTitleBar(this.AppTitleBar);
        this.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        this.Root.ActualThemeChanged += (_, _) => TitleBarHelper.ApplySystemThemeToCaptionButtons(this, this.Root.ActualTheme);
        TitleBarHelper.ApplySystemThemeToCaptionButtons(this, this.Root.ActualTheme);
        this.AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico"));
        this.Root.Loaded += this.OnRootLoaded;
        this.Navigation.SelectedItem = this.Navigation.MenuItems[0];
    }

    private void OnRootLoaded(object sender, RoutedEventArgs args)
    {
        this.Root.Loaded -= this.OnRootLoaded;
        var scale = this.Root.XamlRoot.RasterizationScale;
        var workArea = DisplayArea.GetFromWindowId(this.AppWindow.Id, DisplayAreaFallback.Primary).WorkArea;
        var width = Math.Min((int)Math.Round(1440 * scale), workArea.Width);
        var height = Math.Min((int)Math.Round(900 * scale), workArea.Height);
        this.AppWindow.MoveAndResize(
            new RectInt32(workArea.X + (workArea.Width - width) / 2, workArea.Y + (workArea.Height - height) / 2, width, height));
    }

    internal void NavigateTo(string tag)
    {
        var destination = Destinations.Single(item => item.Tag == tag);
        if (this.ContentFrame.CurrentSourcePageType != destination.Page)
        {
            this.ContentFrame.Navigate(destination.Page);
        }
    }

    private void OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            this.NavigateTo("settings");
        }
        else if (args.SelectedItem is NavigationViewItem { Tag: string tag })
        {
            this.NavigateTo(tag);
        }
    }

    private void OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (sender.DisplayMode != NavigationViewDisplayMode.Expanded)
        {
            sender.IsPaneOpen = false;
        }
    }

    private void OnTitleBarBackRequested(TitleBar sender, object args)
    {
        if (this.ContentFrame.CanGoBack)
        {
            this.ContentFrame.GoBack();
        }
    }

    private void OnPaneToggleRequested(TitleBar sender, object args)
    {
        this.Navigation.IsPaneOpen = !this.Navigation.IsPaneOpen;
    }

    private void OnSearchAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        this.SearchBox.Focus(FocusState.Keyboard);
        args.Handled = true;
    }

    private void OnNavigated(object sender, NavigationEventArgs args)
    {
        var destination = Destinations.Single(item => item.Page == args.SourcePageType);
        this.Navigation.SelectedItem = destination.Tag == "settings"
            ? this.Navigation.SettingsItem
            : this.Navigation.MenuItems.OfType<NavigationViewItem>().Single(item => (string)item.Tag == destination.Tag);
    }

    private void OnContentSizeChanged(object sender, SizeChangedEventArgs args)
    {
        var margin = this.Navigation.ActualWidth < 720 ? new Thickness(16, 16, 16, 0) : new Thickness(36, 32, 36, 0);
        if (this.ContentFrame.Margin != margin)
        {
            this.ContentFrame.Margin = margin;
        }
    }

    private void OnSearchTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            sender.ItemsSource = Destinations.Where(item => item.Name.Contains(sender.Text, StringComparison.CurrentCultureIgnoreCase))
                .Select(item => item.Name).ToArray();
        }
    }

    private void OnSearchSubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var query = args.ChosenSuggestion as string ?? args.QueryText;
        var matches = Destinations.Where(item => item.Name.Equals(query, StringComparison.CurrentCultureIgnoreCase)).ToArray();
        if (matches.Length == 1)
        {
            this.NavigateTo(matches[0].Tag);
            sender.Text = string.Empty;
        }
    }
}
