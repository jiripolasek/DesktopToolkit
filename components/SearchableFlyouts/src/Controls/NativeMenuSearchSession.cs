// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Numerics;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using VirtualKey = Windows.System.VirtualKey;

namespace JPSoftworks.DesktopToolkit.Controls;

internal sealed partial class NativeMenuSearchSession : IDisposable
{
    private readonly SearchableMenuFlyout _owner;
    private readonly MenuFlyoutPresenter _presenter;
    private readonly FrameworkElement _root;
    private readonly TextBox _filter;
    private readonly Border _filterHost;
    private readonly Border? _filterBackdrop;
    private readonly Border? _filterThemedBackground;
    private readonly FrameworkElement? _surface;
    private readonly ScrollViewer _scroller;
    private readonly MenuSearchItem _header;
    private readonly MenuSearchItem _empty;
    private readonly TextBlock _count;
    private readonly MenuFlyoutItemBase? _parent;
    private readonly DispatcherQueueTimer _timer;
    private readonly TypedEventHandler<UIElement, CharacterReceivedRoutedEventArgs> _characterHandler;
    private readonly IObservableVector<object>? _itemsVector;
    private readonly Dictionary<MenuFlyoutItemBase, Storyboard> _hidden = [];
    private readonly List<(DependencyObject Item, DependencyProperty Property, long Token)> _listeners = [];
    private MenuFlyoutItemBase[] _items = [];
    private Popup? _popup;

    private (Border Chrome, Border Backdrop, SystemBackdrop? PopupBackdrop, UIElement ShadowCaster, Shadow? Shadow, Vector3 Translation,
        bool DefaultShadowEnabled)? _boundedSurface;

    private (UIElement Caster, Vector3 Translation)? _submenuShadow;
    private double? _pinnedTop;
    private bool _initialized;
    private bool _applying;
    private bool _updatingPosition;
    private bool _disposed;
    private FilterBoxPlacement _placement;

    internal float? UpwardMenuShadowElevation =>
        this._boundedSurface is { Shadow: not null } surface ? surface.Translation.Z : null;

    private UIElement ShadowCaster => this._presenter.Shadow is not null ? this._presenter : this._root;

    internal bool IsRoot => this._parent is null;

    internal bool IsDisposed => this._disposed;

    internal int FilteredItemCount { get; private set; }

    [WinRT.DynamicWindowsRuntimeCast(typeof(TextBox))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(Border))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(ScrollViewer))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(TextBlock))]
    internal NativeMenuSearchSession(
        SearchableMenuFlyout owner,
        MenuFlyoutPresenter presenter,
        FrameworkElement root,
        MenuSearchItem header,
        MenuSearchItem empty)
    {
        this._owner = owner;
        this._presenter = presenter;
        this._root = root;
        this._header = header;
        this._header.ApplyTemplate();
        this._filter = (TextBox)Find(this._header, "SearchFilterBox");
        this._filterHost = (Border)Find(this._header, "SearchFilterHost");
        this._filterBackdrop = FindOrDefault(this._header, "SearchFilterBackdrop") as Border;
        this._filterThemedBackground = FindOrDefault(this._header, "SearchFilterThemedBackground") as Border;
        this._surface = FindOrDefault(root, "SearchMenuSurface");
        this.ApplyBackdrop();
        Canvas.SetZIndex(this._header, 1);
        this._scroller = (ScrollViewer)Find(presenter, "MenuFlyoutPresenterScrollViewer");
        this._empty = empty;
        this._count = (TextBlock)Find(root, "SearchResultCount");
        this._parent = this._header.MenuParent;
        var depth = 0;
        for (var ancestor = this._parent; ancestor is not null; ancestor = owner.ParentOf(ancestor))
        {
            depth++;
        }

        var suffix = depth == 0 ? string.Empty : $"Depth{depth}";
        AutomationProperties.SetAutomationId(this._filter, $"NativeSearchFilterBox{suffix}");
        AutomationProperties.SetAutomationId(this._presenter, $"NativeSearchPresenter{suffix}");
        AutomationProperties.SetAutomationId(this._empty, $"NativeSearchEmptyMessage{suffix}");
        AutomationProperties.SetAutomationId(this._count, $"NativeSearchResultCount{suffix}");
        this._timer = root.DispatcherQueue.CreateTimer();
        this._timer.Interval = TimeSpan.FromMilliseconds(70);
        this._timer.IsRepeating = false;
        this._timer.Tick += this.OnTimerTick;
        this._filter.TextChanged += this.OnTextChanged;
        this._filter.ProcessKeyboardAccelerators += this.OnProcessKeyboardAccelerators;
        this._filter.PreviewKeyDown += this.OnFilterPreviewKeyDown;
        this._filter.KeyDown += OnFilterKey;
        this._filter.KeyUp += OnFilterKey;
        this._root.PreviewKeyDown += this.OnPreviewKeyDown;
        this._characterHandler = this.OnCharacterReceived;
        this._root.AddHandler(UIElement.CharacterReceivedEvent, this._characterHandler, true);
        this._root.LayoutUpdated += this.OnLayoutUpdated;
        this._scroller.ViewChanged += this.OnViewChanged;
        this._root.GotFocus += this.OnGotFocus;
        this._itemsVector = presenter.Items as IObservableVector<object>;
        if (this._itemsVector is not null)
        {
            this._itemsVector.VectorChanged += this.OnItemsChanged;
        }

        owner.AddSession(this);
        this.ApplyOptions();
        this.Refresh();
        this.QueueInitialize();
    }

    public void Dispose()
    {
        if (this._disposed)
        {
            return;
        }

        this._disposed = true;
        this._timer.Stop();
        this._timer.Tick -= this.OnTimerTick;
        this._filter.CancelAnnouncements();
        this._filter.TextChanged -= this.OnTextChanged;
        this._filter.ProcessKeyboardAccelerators -= this.OnProcessKeyboardAccelerators;
        this._filter.PreviewKeyDown -= this.OnFilterPreviewKeyDown;
        this._filter.KeyDown -= OnFilterKey;
        this._filter.KeyUp -= OnFilterKey;
        this._root.PreviewKeyDown -= this.OnPreviewKeyDown;
        this._root.RemoveHandler(UIElement.CharacterReceivedEvent, this._characterHandler);
        this._root.LayoutUpdated -= this.OnLayoutUpdated;
        this._scroller.ViewChanged -= this.OnViewChanged;
        this._root.GotFocus -= this.OnGotFocus;
        if (this._itemsVector is not null)
        {
            this._itemsVector.VectorChanged -= this.OnItemsChanged;
        }

        foreach (var (item, property, token) in this._listeners)
        {
            item.UnregisterPropertyChangedCallback(property, token);
        }

        this._listeners.Clear();
        foreach (var storyboard in this._hidden.Values)
        {
            storyboard.Stop();
        }

        this._hidden.Clear();
        if (this._boundedSurface is { } surface)
        {
            this._root.PointerPressed -= this.OnSlotPointerPressed;
            this._root.ClearValue(FrameworkElement.HeightProperty);
            this._surface!.ClearValue(FrameworkElement.VerticalAlignmentProperty);
            surface.Chrome.Shadow = null;
            surface.Chrome.Translation = default;
            MenuSearch.SetAcrylicBackdrop(surface.Backdrop, null, enabled: false);
            this._presenter.IsDefaultShadowEnabled = surface.DefaultShadowEnabled;
            surface.ShadowCaster.Translation = surface.Translation;
            surface.ShadowCaster.Shadow = surface.Shadow;
            if (this._popup is not null)
            {
                this._popup.SystemBackdrop = surface.PopupBackdrop;
            }
        }

        if (this._submenuShadow is { } submenuShadow)
        {
            submenuShadow.Caster.Translation = submenuShadow.Translation;
        }

        this._owner.RemoveSession(this);
    }

    internal void QueueInitialize()
    {
        this._root.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, this.Initialize);
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(DependencyObject))]
    internal void Initialize()
    {
        if (this._disposed || this._initialized || !this._root.IsLoaded || !this._owner.IsOpen || this._root.ActualHeight <= 0)
        {
            return;
        }

        this._popup = MenuSearch.FindPopup(this._presenter);
        if (this._popup is not null)
        {
            this._popup.RequestedTheme = this._presenter.ActualTheme;
        }

        var anchor = this._parent as FrameworkElement ?? this._owner.Target;
        this._placement = this._owner.FilterBoxPlacement == FilterBoxPlacement.Auto && anchor is not null
            ? MenuSearch.ResolvePlacement(this._popup, this._root, anchor)
            : this._owner.FilterBoxPlacement;
        this.SetFilterPlacement();
        this._root.UpdateLayout();
        this.UpdateStickyHeader();
        this._root.UpdateLayout();
        if (this.IsRoot
            && this._placement == FilterBoxPlacement.Bottom
            && anchor is not null
            && MenuSearch.ResolvePlacement(this._popup, this._root, anchor) == FilterBoxPlacement.Bottom)
        {
            this.StabilizeUpwardPopup();
        }
        else if (!this.IsRoot)
        {
            this.MatchUpwardMenuShadow();
        }

        this._initialized = true;
        this._pinnedTop = this.FilterTop();
        this.ApplyFilter();
        if (this._disposed)
        {
            return;
        }

        var focused = FocusManager.GetFocusedElement(this._root.XamlRoot) as DependencyObject;
        if (this.IsRoot || PresenterOf(focused) == this._presenter)
        {
            this._filter.Focus(FocusState.Programmatic);
            this._filter.SelectAll();
        }
    }

    internal void ApplyOptions()
    {
        if (this._disposed)
        {
            return;
        }

        this._presenter.Width = double.IsFinite(this._owner.FlyoutWidth) ? Math.Max(160, this._owner.FlyoutWidth) : 280;
        this.UpdateStickyHeader();
        if (this._owner.Target is { } target)
        {
            this._presenter.RequestedTheme = target.ActualTheme;
            this._presenter.FlowDirection = target.FlowDirection;
        }

        var menuName = AutomationProperties.GetName(this._owner);
        AutomationProperties.SetName(this._presenter, SearchableFlyoutStrings.WithFallback(menuName, this._owner.Strings.ListName()));
        this._filter.PlaceholderText = this._owner.FilterPlaceholderText;
        var filterName = this._owner.FilterBoxAutomationName;
        var label = this._parent is { } parent
            ? this._owner.Strings.Submenu(
                SearchableFlyoutStrings.WithFallback(SearchableMenuFlyout.ItemText(parent), AutomationProperties.GetName(parent)), filterName)
            : filterName;
        AutomationProperties.SetName(this._header, label);
        AutomationProperties.SetName(this._filter, label);
        this._filter.SetKeyboardHelp(this._owner.Strings.KeyboardHelp);
        this._empty.Text = this._owner.NoResultsText;
        this._count.Visibility = this._owner.IsResultCountVisible ? Visibility.Visible : Visibility.Collapsed;
        this.SyncFilterText();
        if (this._owner.FilterBoxPlacement != FilterBoxPlacement.Auto && this._placement != this._owner.FilterBoxPlacement)
        {
            this._placement = this._owner.FilterBoxPlacement;
            this._pinnedTop = null;
            this.SetFilterPlacement();
        }

        this.ScheduleFilter();
    }

    internal void SyncFilterText()
    {
        if (this.IsRoot && this._filter.Text != this._owner.FilterText)
        {
            this._filter.Text = this._owner.FilterText;
        }
    }

    internal void ApplyBackdrop()
    {
        if (!this._disposed && this._filterBackdrop is not null)
        {
            MenuSearch.SetAcrylicBackdrop(this._filterBackdrop, this._filterThemedBackground, this._owner.UseAcrylicBackdrop);
        }
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(MenuFlyoutItem))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(MenuFlyoutSubItem))]
    internal void Refresh()
    {
        if (this._disposed)
        {
            return;
        }

        foreach (var (item, property, token) in this._listeners)
        {
            item.UnregisterPropertyChangedCallback(property, token);
        }

        this._listeners.Clear();
        this._items = this._presenter.Items.OfType<MenuFlyoutItemBase>().Where(item => item is not MenuSearchItem).ToArray();
        foreach (var removed in this._hidden.Keys.Except(this._items).ToArray())
        {
            this._hidden[removed].Stop();
            this._hidden.Remove(removed);
        }

        foreach (var item in this._items)
        {
            this.Observe(item, UIElement.VisibilityProperty);
            if (item is MenuFlyoutItem)
            {
                this.Observe(item, MenuFlyoutItem.TextProperty);
            }
            else if (item is MenuFlyoutSubItem)
            {
                this.Observe(item, MenuFlyoutSubItem.TextProperty);
            }
        }

        this.ApplyFilter();
    }

    private void Observe(DependencyObject item, DependencyProperty property)
    {
        var token = item.RegisterPropertyChangedCallback(
            property, (_, _) =>
            {
                if (!this._applying)
                {
                    this.ScheduleFilter();
                }
            });
        this._listeners.Add((item, property, token));
    }

    private void OnItemsChanged(IObservableVector<object> sender, IVectorChangedEventArgs args)
    {
        this._owner.Refresh();
    }

    private void OnTimerTick(DispatcherQueueTimer sender, object args)
    {
        this.ApplyFilter();
    }

    private void OnProcessKeyboardAccelerators(UIElement sender, ProcessKeyboardAcceleratorEventArgs args)
    {
        this._owner.ProcessSearchAccelerators(args);
    }

    private void OnTextChanged(object sender, TextChangedEventArgs args)
    {
        if (this.IsRoot && this._owner.FilterText != this._filter.Text)
        {
            this._owner.FilterText = this._filter.Text;
        }

        this.ScheduleFilter();
    }

    private void ScheduleFilter()
    {
        if (this._disposed)
        {
            return;
        }

        this._timer.Stop();
        this._timer.Start();
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(MenuFlyoutSeparator))]
    private void ApplyFilter()
    {
        if (this._disposed || !this._initialized || this._applying)
        {
            return;
        }

        this._timer.Stop();
        this._applying = true;
        try
        {
            var tokens = MenuSearch.Tokenize(this._filter.Text);
            var visible = MenuSearch.Filter(
                this._items, IsOriginallyVisible, item => item is MenuFlyoutSeparator,
                item => MenuSearch.Matches(SearchableMenuFlyout.ItemText(item), SearchableMenuFlyout.GetKeywords(item), tokens));
            var matches = visible.ToHashSet();
            foreach (var item in this._items)
            {
                if (matches.Contains(item) || !IsOriginallyVisible(item))
                {
                    if (this._hidden.Remove(item, out var hidden))
                    {
                        hidden.Stop();
                    }
                }
                else if (!this._hidden.ContainsKey(item))
                {
                    CollapseSubmenu(item);
                    var animation = new ObjectAnimationUsingKeyFrames { Duration = TimeSpan.Zero, EnableDependentAnimation = true };
                    animation.KeyFrames.Add(new DiscreteObjectKeyFrame { KeyTime = TimeSpan.Zero, Value = Visibility.Collapsed });
                    Storyboard.SetTarget(animation, item);
                    Storyboard.SetTargetProperty(animation, "Visibility");
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(animation);
                    this._hidden.Add(item, storyboard);
                    // An animation layer preserves the caller's Visibility binding and local value.
                    storyboard.Begin();
                }
            }

            this.FilteredItemCount = visible.Count(item => item is not MenuFlyoutSeparator);
            this._empty.Visibility = this.FilteredItemCount == 0 ? Visibility.Visible : Visibility.Collapsed;

            this._count.Text = this._owner.Strings.Count(
                this.FilteredItemCount, this._items.Count(item => item is not MenuFlyoutSeparator && IsOriginallyVisible(item)));
            var status = this._owner.Strings.Matches(this.FilteredItemCount);
            AutomationProperties.SetHelpText(this._presenter, status);
            this._filter.SetResultStatus(this.FilteredItemCount == 0 ? this._owner.NoResultsText : status);
            if (this.IsRoot)
            {
                this._owner.NotifyFilterChanged();
            }
        }
        finally
        {
            this._applying = false;
        }
    }

    private static bool IsOriginallyVisible(MenuFlyoutItemBase item)
    {
        return item.GetAnimationBaseValue(UIElement.VisibilityProperty) is Visibility.Visible;
    }

    private void SetFilterPlacement()
    {
        var bottom = this._placement == FilterBoxPlacement.Bottom;
        this._filterHost.BorderThickness = bottom ? new Thickness(0, 1, 0, 0) : new Thickness(0, 0, 0, 1);
        this.UpdateStickyHeader();
    }

    private double FilterTop()
    {
        return this._filter.TransformToVisual(this._root.XamlRoot.Content).TransformPoint(new Point()).Y;
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(Border))]
    private void StabilizeUpwardPopup()
    {
        if (this._surface is null
            || this._popup is not { IsOpen: true } popup
            || FindOrDefault(this._root, "SearchMenuChrome") is not Border chrome
            || FindOrDefault(this._root, "SearchMenuBackdrop") is not Border backdrop)
        {
            return;
        }

        // Keep the popup window still while the visible menu grows upward inside it.
        var shadowCaster = this.ShadowCaster;
        var shadow = shadowCaster.Shadow;
        var translation = shadowCaster.Translation;
        this._boundedSurface = (chrome, backdrop, popup.SystemBackdrop, shadowCaster, shadow, translation,
            this._presenter.IsDefaultShadowEnabled);
        this._empty.Visibility = Visibility.Visible;
        this._empty.Measure(new Size(this._root.ActualWidth, double.PositiveInfinity));
        var emptyHeight = this._empty.DesiredSize.Height;
        this._empty.Visibility = Visibility.Collapsed;
        var chromeHeight = chrome.BorderThickness.Top + chrome.BorderThickness.Bottom + this._count.ActualHeight;
        var minimumHeight = this._header.ActualHeight
                            + Math.Min(emptyHeight, this._scroller.MaxHeight - this._header.ActualHeight)
                            + chromeHeight;
        this._root.Height = Math.Max(this._root.ActualHeight, minimumHeight);
        this._surface.VerticalAlignment = VerticalAlignment.Bottom;
        this._presenter.IsDefaultShadowEnabled = false;
        // Native submenus inherit the backdrop from the root presenter.
        popup.SystemBackdrop = null;
        if (shadow is not null)
        {
            shadowCaster.Shadow = null;
            shadowCaster.Translation = new(translation.X, translation.Y, 0);
            chrome.Translation = new(0, 0, translation.Z);
            chrome.Shadow = shadow;
        }

        MenuSearch.SetAcrylicBackdrop(backdrop, null, enabled: true, rounded: true);
        this._root.PointerPressed += this.OnSlotPointerPressed;
        this._root.UpdateLayout();
    }

    private void MatchUpwardMenuShadow()
    {
        if (!this._presenter.IsDefaultShadowEnabled || this._owner.UpwardMenuShadowElevation is not { } elevation)
        {
            return;
        }

        var caster = this.ShadowCaster;
        var translation = caster.Translation;
        if (caster.Shadow is not null && translation.Z > elevation)
        {
            this._submenuShadow = (caster, translation);
            caster.Translation = new(translation.X, translation.Y, elevation);
        }
    }

    private void OnSlotPointerPressed(object sender, PointerRoutedEventArgs args)
    {
        if (this._surface is not { } surface)
        {
            return;
        }

        var point = args.GetCurrentPoint(surface).Position;
        if (point.X < 0 || point.Y < 0 || point.X > surface.ActualWidth || point.Y > surface.ActualHeight)
        {
            args.Handled = true;
            this._owner.Hide();
        }
    }

    private void OnLayoutUpdated(object? sender, object args)
    {
        if (this._disposed || this._updatingPosition)
        {
            return;
        }

        if (!this._initialized)
        {
            return;
        }

        // Use the arranged field position after a viewport change.
        if (this.UpdateStickyHeader() || this._boundedSurface is not null || this._popup is not { IsOpen: true })
        {
            return;
        }

        this._pinnedTop ??= this.FilterTop();
        var delta = this._pinnedTop.Value - this.FilterTop();
        if (!double.IsFinite(delta) || Math.Abs(delta) < 0.25)
        {
            return;
        }

        this._updatingPosition = true;
        try
        {
            this._popup.VerticalOffset += delta;
        }
        finally
        {
            this._updatingPosition = false;
        }
    }

    private void OnViewChanged(object? sender, ScrollViewerViewChangedEventArgs args)
    {
        this.UpdateStickyHeader();
    }

    private bool UpdateStickyHeader()
    {
        var height = this._header.ActualHeight > 0 ? this._header.ActualHeight : 33;
        var limit = double.IsFinite(this._owner.MaxListHeight) ? Math.Max(32, this._owner.MaxListHeight) : 320;
        var maxHeight = limit + height;
        var changed = Math.Abs(this._scroller.MaxHeight - maxHeight) > 0.25;
        if (changed)
        {
            this._scroller.MaxHeight = maxHeight;
        }

        // WinUI can realize or replace the panel after the presenter is loaded.
        if (this._presenter.ItemsPanelRoot is NativeMenuItemsPanel panel)
        {
            var viewport = this._scroller.ViewportHeight;
            // A content-sized viewport can grow on the next arrange.
            var viewportLimit = viewport > 0 && this._scroller.ExtentHeight - viewport > 0.25 ? viewport : maxHeight;
            changed |= panel.UpdateViewport(this._placement == FilterBoxPlacement.Bottom, this._scroller.VerticalOffset, viewportLimit);
        }

        return changed;
    }

    private void OnGotFocus(object sender, RoutedEventArgs args)
    {
        this._root.DispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low, () =>
            {
                if (this._disposed || this.FocusedItem() is not { } item)
                {
                    return;
                }

                var bounds = item.TransformToVisual(this._scroller).TransformBounds(new Rect(0, 0, item.ActualWidth, item.ActualHeight));
                var top = this._placement == FilterBoxPlacement.Top ? this._header.ActualHeight : 0;
                var bottom = this._scroller.ViewportHeight - (this._placement == FilterBoxPlacement.Bottom ? this._header.ActualHeight : 0);
                if (bounds.Y < top)
                {
                    this._scroller.ChangeView(null, Math.Max(0, this._scroller.VerticalOffset + bounds.Y - top), null, true);
                }
                else if (bounds.Bottom > bottom)
                {
                    this._scroller.ChangeView(null, this._scroller.VerticalOffset + bounds.Bottom - bottom, null, true);
                }
            });
    }

    private void OnFilterPreviewKeyDown(object sender, KeyRoutedEventArgs args)
    {
        if (this._disposed)
        {
            return;
        }

        if (args.Key is VirtualKey.Up or VirtualKey.Down)
        {
            this.ApplyFilter();
            this.FocusBoundary(args.Key == VirtualKey.Down);
            args.Handled = true;
        }
        else if (args.Key == VirtualKey.Enter)
        {
            this.ApplyFilter();
            var first = this.FocusableItems().FirstOrDefault();
            if (first is not null)
            {
                InvokeNative(first);
            }

            args.Handled = true;
        }
    }

    private static void OnFilterKey(object sender, KeyRoutedEventArgs args)
    {
        // Keep Space from activating the native menu item that hosts the editor.
        if (args.Key == VirtualKey.Space)
        {
            args.Handled = true;
        }
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(DependencyObject))]
    private void OnPreviewKeyDown(object sender, KeyRoutedEventArgs args)
    {
        if (this._disposed || this._filter.FocusState != FocusState.Unfocused)
        {
            return;
        }

        if (PresenterOf(FocusManager.GetFocusedElement(this._root.XamlRoot) as DependencyObject) != this._presenter)
        {
            return;
        }

        if (this._filter.HandleListEditingKey(args))
        {
            return;
        }

        if (args.Key is VirtualKey.Up or VirtualKey.Down)
        {
            var items = this.FocusableItems().ToArray();
            var focused = this.FocusedItem();
            if (items.Length > 0
                && (args.Key == VirtualKey.Up && focused == items[0] || args.Key == VirtualKey.Down && focused == items[^1]))
            {
                this._filter.FocusForEditing();
                args.Handled = true;
            }
        }
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(DependencyObject))]
    private void OnCharacterReceived(UIElement sender, CharacterReceivedRoutedEventArgs args)
    {
        if (this._disposed || this._filter.FocusState != FocusState.Unfocused || char.IsControl(args.Character))
        {
            return;
        }

        var control = MenuFilterBox.IsKeyDown(VirtualKey.Control);
        var alt = MenuFilterBox.IsKeyDown(VirtualKey.Menu);
        if (control != alt)
        {
            return;
        }

        if (PresenterOf(FocusManager.GetFocusedElement(this._root.XamlRoot) as DependencyObject) != this._presenter)
        {
            return;
        }

        this._filter.InsertCharacter(args.Character);
        args.Handled = true;
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(MenuFlyoutSeparator))]
    private IEnumerable<MenuFlyoutItemBase> FocusableItems()
    {
        return this._items.Where(item =>
            item is not MenuFlyoutSeparator && !this._hidden.ContainsKey(item) && item.Visibility == Visibility.Visible && item.IsEnabled);
    }

    private void FocusBoundary(bool first)
    {
        var item = first ? this.FocusableItems().FirstOrDefault() : this.FocusableItems().LastOrDefault();
        if (item is null)
        {
            return;
        }

        item.StartBringIntoView();
        item.Focus(FocusState.Keyboard);
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(DependencyObject))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(MenuFlyoutItemBase))]
    private MenuFlyoutItemBase? FocusedItem()
    {
        for (var element = FocusManager.GetFocusedElement(this._root.XamlRoot) as DependencyObject;
             element is not null;
             element = VisualTreeHelper.GetParent(element))
        {
            if (element is MenuFlyoutItemBase item && this._items.Contains(item))
            {
                return item;
            }
        }

        return null;
    }

    internal static bool InvokeNative(MenuFlyoutItemBase item)
    {
        if (!item.IsEnabled || item.Visibility != Visibility.Visible)
        {
            return false;
        }

        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(item);
        if (peer?.GetPattern(PatternInterface.Invoke) is IInvokeProvider invoke)
        {
            invoke.Invoke();
            return true;
        }
        else if (peer?.GetPattern(PatternInterface.Toggle) is IToggleProvider toggle)
        {
            toggle.Toggle();
            return true;
        }
        else if (peer?.GetPattern(PatternInterface.SelectionItem) is ISelectionItemProvider selection)
        {
            selection.Select();
            return true;
        }
        else if (peer?.GetPattern(PatternInterface.ExpandCollapse) is IExpandCollapseProvider expand)
        {
            if (expand.ExpandCollapseState == ExpandCollapseState.Expanded)
            {
                expand.Collapse();
            }
            else
            {
                expand.Expand();
            }

            return true;
        }

        return false;
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(MenuFlyoutSubItem))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(SplitMenuFlyoutItem))]
    private static void CollapseSubmenu(MenuFlyoutItemBase item)
    {
        if (item is not MenuFlyoutSubItem && item is not SplitMenuFlyoutItem)
        {
            return;
        }

        if (FrameworkElementAutomationPeer.FromElement(item)?.GetPattern(PatternInterface.ExpandCollapse) is IExpandCollapseProvider expand
            && expand.ExpandCollapseState == ExpandCollapseState.Expanded)
        {
            expand.Collapse();
        }
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(MenuFlyoutPresenter))]
    private static MenuFlyoutPresenter? PresenterOf(DependencyObject? element)
    {
        for (; element is not null; element = VisualTreeHelper.GetParent(element))
        {
            if (element is MenuFlyoutPresenter presenter)
            {
                return presenter;
            }
        }

        return null;
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
    private static FrameworkElement Find(DependencyObject root, string name)
    {
        if (root is FrameworkElement element && element.Name == name)
        {
            return element;
        }

        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            if (FindOrDefault(VisualTreeHelper.GetChild(root, index), name) is { } found)
            {
                return found;
            }
        }

        throw new InvalidOperationException($"Missing searchable menu template part: {name}.");
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
    private static FrameworkElement? FindOrDefault(DependencyObject root, string name)
    {
        if (root is FrameworkElement element && element.Name == name)
        {
            return element;
        }

        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            if (FindOrDefault(VisualTreeHelper.GetChild(root, index), name) is { } found)
            {
                return found;
            }
        }

        return null;
    }
}
