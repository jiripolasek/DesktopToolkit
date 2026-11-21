// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Graphics;

namespace JPSoftworks.DesktopToolkit.Gallery;

public sealed partial class MainWindow : Window
{
    private const string NoSearchResults = "No matching controls";
    private const double CompactTitleBarWidth = 720;
    private const double TitleBarDragWidth = 24;
    private readonly ILogger<MainWindow> _logger;
    private readonly Dictionary<string, NavigationViewItem> _destinationItems = new();
    private InputNonClientPointerSource? _titleBarInput;
    private XamlRoot? _titleBarRoot;
    private bool _titleBarUpdateQueued;
    private bool _searchInCompactRow;

    private AutoSuggestBox ActiveSearch => this._searchInCompactRow ? this.CompactGallerySearch : this.GallerySearch;

    public MainWindow(ILogger<MainWindow> logger)
    {
        this._logger = logger;
        this.InitializeComponent();
        this.AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico"));
        this.ExtendsContentIntoTitleBar = true;
        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            this.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        }

        this._titleBarInput = InputNonClientPointerSource.GetForWindowId(this.AppWindow.Id);
        this.AppWindow.Changed += this.OnAppWindowChanged;
        this.ApplyTheme(GalleryPreferences.Theme);
        GalleryPreferences.ThemeChanged += this.ApplyTheme;
        this.Closed += this.OnWindowClosed;

        foreach (var section in GalleryCatalog.Sections)
        {
            this.Navigation.MenuItems.Add(new NavigationViewItemHeader { Content = section.Title.ToUpperInvariant() });
            this.AddNavigationItems(section.Destinations);
        }

        this.PageFrame.NavigationFailed += this.OnNavigationFailed;
        this.Navigate(typeof(HomePage));
    }

    private void AddNavigationItems(GalleryDestination[] destinations)
    {
        foreach (var destination in destinations)
        {
            var item = new NavigationViewItem { Content = destination.Title, Icon = destination.CreateIcon(), Tag = destination, };

            this._destinationItems.Add(destination.Id, item);
            this.Navigation.MenuItems.Add(item);
        }
    }

    private void OnShellSizeChanged(object sender, SizeChangedEventArgs args)
    {
        this.UpdatePageMargin(args.NewSize.Width);
        var useCompactRow = args.NewSize.Width < CompactTitleBarWidth;
        if (useCompactRow)
        {
            this.CompactSearchHost.Width = Math.Min(this.GallerySearch.MaxWidth, Math.Max(0, args.NewSize.Width - 32));
        }

        if (this._searchInCompactRow == useCompactRow)
        {
            return;
        }

        var previousSearch = this.ActiveSearch;
        previousSearch.IsSuggestionListOpen = false;
        this._searchInCompactRow = useCompactRow;
        this.GallerySearch.Visibility = useCompactRow ? Visibility.Collapsed : Visibility.Visible;
        Grid.SetColumnSpan(this.TitleBarIdentity, useCompactRow ? 3 : 1);
        this.CompactSearchHost.Visibility = useCompactRow ? Visibility.Visible : Visibility.Collapsed;
        this.ActiveSearch.Text = previousSearch.Text;
        previousSearch.ItemsSource = null;

        if (useCompactRow)
        {
            this.TitleBarBrandColumn.Width = new GridLength(1, GridUnitType.Star);
        }
        else
        {
            this.PositionTitleBarSearch(this.TitleBarContentGrid.ActualWidth);
        }

        this.QueueTitleBarInputUpdate();
    }

    private void OnTitleBarContentSizeChanged(object sender, SizeChangedEventArgs args)
    {
        if (!this._searchInCompactRow)
        {
            this.PositionTitleBarSearch(args.NewSize.Width);
        }

        this.QueueTitleBarInputUpdate();
    }

    private void PositionTitleBarSearch(double availableWidth)
    {
        this.TitleBarIdentity.Measure(new Size(double.PositiveInfinity, this.GalleryTitleBar.Height));
        var brandWidth = this.TitleBarIdentity.DesiredSize.Width;
        var maxSearchWidth = Math.Clamp(
            availableWidth - brandWidth - TitleBarDragWidth, this.GallerySearch.MinWidth, this.GallerySearch.MaxWidth);
        var searchWidth = Math.Clamp(availableWidth * 0.65, this.GallerySearch.MinWidth, maxSearchWidth);
        var center = this.RootLayout.TransformToVisual(this.TitleBarContentGrid)
            .TransformPoint(new Point(this.RootLayout.ActualWidth / 2, 0)).X;

        var maxOffset = Math.Max(0, availableWidth - searchWidth - TitleBarDragWidth);
        var minOffset = Math.Min(brandWidth, maxOffset);
        this.TitleBarBrandColumn.Width = new GridLength(Math.Clamp(center - searchWidth / 2, minOffset, maxOffset));
        this.GallerySearch.Width = searchWidth;
    }

    private void OnTitleBarLoaded(object sender, RoutedEventArgs args)
    {
        if (this._titleBarRoot is null && this.GalleryTitleBar.XamlRoot is { } root)
        {
            this._titleBarRoot = root;
            root.Changed += this.OnTitleBarRootChanged;
        }

        this.QueueTitleBarInputUpdate();
    }

    private void OnTitleBarRootChanged(XamlRoot sender, XamlRootChangedEventArgs args)
    {
        this.QueueTitleBarInputUpdate();
    }

    private void OnAppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (args.DidSizeChange || args.DidPresenterChange)
        {
            this.QueueTitleBarInputUpdate();
        }
    }

    private void QueueTitleBarInputUpdate()
    {
        if (!this._titleBarUpdateQueued)
        {
            this._titleBarUpdateQueued = this.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, this.UpdateTitleBarInput);
        }
    }

    private void UpdateTitleBarInput()
    {
        this._titleBarUpdateQueued = false;
        if (this._titleBarInput is null || this._titleBarRoot is null)
        {
            return;
        }

        var titleBar = this.AppWindow.TitleBar;
        var scale = this._titleBarRoot.RasterizationScale;
        var clientLeft = titleBar.LeftInset;
        var rightInset = titleBar.RightInset;
        if (!double.IsFinite(scale) || scale <= 0 || clientLeft < 0 || rightInset < 0)
        {
            return;
        }

        this.TitleBarLeftInset.Width = new GridLength(clientLeft / scale);
        this.TitleBarRightInset.Width = new GridLength(rightInset / scale);

        var clientRight = (int)Math.Round(this.GalleryTitleBar.ActualWidth * scale) - rightInset;
        var regions = new List<RectInt32>(3);
        AddInteractiveControl(this.TitleBarBackButton);
        AddInteractiveControl(this.TitleBarPaneToggleButton);
        AddInteractiveControl(this.GallerySearch);
        this._titleBarInput.SetRegionRects(NonClientRegionKind.Passthrough, regions.ToArray());

        void AddInteractiveControl(FrameworkElement control)
        {
            if (control.Visibility != Visibility.Visible)
            {
                return;
            }

            var bounds = control.TransformToVisual(this.RootLayout)
                .TransformBounds(new Rect(0, 0, control.ActualWidth, control.ActualHeight));
            var left = Math.Max(clientLeft, (int)Math.Round(bounds.Left * scale));
            var right = Math.Min(clientRight, (int)Math.Round(bounds.Right * scale));
            var top = (int)Math.Round(bounds.Top * scale);
            var bottom = (int)Math.Round(bounds.Bottom * scale);
            if (right > left && bottom > top)
            {
                regions.Add(new RectInt32(left, top, right - left, bottom - top));
            }
        }
    }

    private void OnNavigationItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.IsSettingsInvoked)
        {
            this.Navigate(typeof(SettingsPage));
        }
        else if (ReferenceEquals(args.InvokedItemContainer, this.HomeItem))
        {
            this.Navigate(typeof(HomePage));
        }
        else if (args.InvokedItemContainer?.Tag is GalleryDestination destination)
        {
            this.Navigate(destination.PageType, destination.ComponentId);
        }
    }

    private void Navigate(Type pageType, string? componentId = null)
    {
        if (this.PageFrame.CurrentSourcePageType != pageType
            || this.PageFrame.Content is ControlPage page && page.Component.Id != componentId)
        {
            this.PageFrame.Navigate(pageType, componentId);
        }
    }

    private void OnNavigationFailed(object sender, NavigationFailedEventArgs args)
    {
        this._logger.LogError(args.Exception, "Failed to navigate to {Page}", args.SourcePageType.Name);
    }

    private void OnPageNavigated(object sender, NavigationEventArgs args)
    {
        this._logger.LogInformation("Navigated to {Page}", args.Parameter as string ?? args.SourcePageType.Name);
        this.Navigation.SelectedItem = args.Content switch
        {
            SettingsPage => this.Navigation.SettingsItem,
            _ => this._destinationItems.GetValueOrDefault(args.Parameter as string ?? args.SourcePageType.Name, this.HomeItem),
        };
        this.UpdatePageMargin(this.RootLayout.ActualWidth);
        this.TitleBarBackButton.Visibility = this.PageFrame.CanGoBack ? Visibility.Visible : Visibility.Collapsed;
        this.QueueTitleBarInputUpdate();

        if (this.Navigation.IsLoaded && this.Navigation.DisplayMode != NavigationViewDisplayMode.Expanded)
        {
            this.Navigation.IsPaneOpen = false;
        }
    }

    private void UpdatePageMargin(double width)
    {
        var sampleMargin = width < CompactTitleBarWidth ? new Thickness(16, 16, 16, 0) : new Thickness(32, 20, 32, 0);
        var margin = this.Navigation.SelectedItem is NavigationViewItem { Tag: GalleryDestination { ComponentId: null } }
            ? sampleMargin
            : default;
        if (this.PageFrame.Margin != margin)
        {
            this.PageFrame.Margin = margin;
        }
    }

    private void OnBackRequested(object sender, RoutedEventArgs args)
    {
        this.GoBack();
    }

    private void OnPaneToggleRequested(object sender, RoutedEventArgs args)
    {
        this.Navigation.IsPaneOpen = !this.Navigation.IsPaneOpen;
    }

    private void OnBackShortcut(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = this.GoBack();
    }

    private bool GoBack()
    {
        if (!this.PageFrame.CanGoBack)
        {
            return false;
        }

        this.PageFrame.GoBack();
        return true;
    }

    private void OnSearchShortcut(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = this.ActiveSearch.Focus(FocusState.Keyboard);
    }

    private void ApplyTheme(ElementTheme theme)
    {
        this.RootLayout.RequestedTheme = theme;
        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            this.AppWindow.TitleBar.PreferredTheme = theme switch
            {
                ElementTheme.Light => TitleBarTheme.Light,
                ElementTheme.Dark => TitleBarTheme.Dark,
                _ => TitleBarTheme.UseDefaultAppMode,
            };
        }
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        this.AppWindow.Changed -= this.OnAppWindowChanged;
        if (this._titleBarRoot is not null)
        {
            this._titleBarRoot.Changed -= this.OnTitleBarRootChanged;
        }

        this._titleBarInput = null;
        GalleryPreferences.ThemeChanged -= this.ApplyTheme;
    }

    private void OnSearchTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
        {
            return;
        }

        string[] suggestions = string.IsNullOrWhiteSpace(sender.Text)
            ? []
            : GalleryCatalog.Search(sender.Text).Select(destination => destination.Title).ToArray();

        sender.ItemsSource = suggestions.Length == 0 && !string.IsNullOrWhiteSpace(sender.Text) ? new[] { NoSearchResults } : suggestions;
    }

    private void OnSearchSubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var query = args.ChosenSuggestion as string ?? args.QueryText;
        var destination = GalleryCatalog.Search(query).FirstOrDefault();
        if (destination is not null)
        {
            this.Navigate(destination.PageType, destination.ComponentId);
            sender.Text = string.Empty;
            sender.ItemsSource = null;
        }
    }
}
