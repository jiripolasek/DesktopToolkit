// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using VirtualKey = Windows.System.VirtualKey;

namespace JPSoftworks.DesktopToolkit.Controls;

/// <summary>Displays a searchable, virtualized menu of data items.</summary>
/// <remarks>Populate <see cref="Items"/> in XAML or bind <see cref="ItemsSource"/>. Each submenu filters its own items.</remarks>
[ContentProperty(Name = nameof(Items))]
public sealed partial class SearchableItemsFlyout : Flyout
{
    /// <summary>Occurs when a leaf item is invoked, including in a submenu. The event follows execution of its <see cref="SearchableFlyoutItem.Command"/>.</summary>
    public event TypedEventHandler<SearchableItemsFlyout, FlyoutItemInvokedEventArgs>? ItemInvoked;

    /// <summary>Occurs after the root list applies its filter and updates the result count.</summary>
    public event TypedEventHandler<SearchableItemsFlyout, object?>? FilterChanged;

    private readonly HashSet<SearchableFlyoutItem> _observedItems = [];
    private readonly HashSet<ICommand> _observedCommands = [];
    private readonly DispatcherQueueTimer _filterTimer;
    private readonly DispatcherQueueTimer _hoverTimer;
    private readonly Style _menuItemStyle;
    private INotifyCollectionChanged? _observableSource;
    private SearchableFlyoutItem[] _sourceItems = [];
    private List<SearchableFlyoutItem> _filteredItems = [];
    private FlyoutItemEntry[] _entries = [];
    private bool _entriesDirty;
    private SearchableFlyoutItem? _hoveredItem;
    private bool _isOpen;
    private SearchableItemsFlyout? _parentMenu;
    private SearchableItemsFlyout? _submenu;
    private SearchableFlyoutItem? _submenuItem;
    private int _depth;
    private FilterBoxPlacement _effectiveFilterPlacement = FilterBoxPlacement.Top;
    private FlyoutPresenter? _presenter;
    private bool _focusFilterOnOpen = true;

    /// <summary>Identifies the <see cref="ItemsSource"/> dependency property.</summary>
    public static DependencyProperty ItemsSourceProperty { get; } = DependencyProperty.Register(
        nameof(ItemsSource), typeof(IEnumerable<SearchableFlyoutItem>), typeof(SearchableItemsFlyout),
        new PropertyMetadata(null, OnSourceChanged));

    /// <summary>Identifies the <see cref="FilterBoxPlacement"/> dependency property.</summary>
    public static DependencyProperty FilterBoxPlacementProperty { get; } = DependencyProperty.Register(
        nameof(FilterBoxPlacement), typeof(FilterBoxPlacement), typeof(SearchableItemsFlyout),
        new PropertyMetadata(FilterBoxPlacement.Auto, OnOptionsChanged));

    /// <summary>Identifies the <see cref="Strings"/> dependency property.</summary>
    public static DependencyProperty StringsProperty { get; } = DependencyProperty.Register(
        nameof(Strings), typeof(SearchableFlyoutStrings), typeof(SearchableItemsFlyout), new PropertyMetadata(null, OnOptionsChanged));

    /// <summary>Identifies the <see cref="FilterText"/> dependency property.</summary>
    public static DependencyProperty FilterTextProperty { get; } = DependencyProperty.Register(
        nameof(FilterText), typeof(string), typeof(SearchableItemsFlyout), new PropertyMetadata(string.Empty, OnFilterChanged));

    /// <summary>Identifies the <see cref="FlyoutWidth"/> dependency property.</summary>
    public static DependencyProperty FlyoutWidthProperty { get; } = DependencyProperty.Register(
        nameof(FlyoutWidth), typeof(double), typeof(SearchableItemsFlyout), new PropertyMetadata(280d, OnOptionsChanged));

    /// <summary>Identifies the <see cref="MaxListHeight"/> dependency property.</summary>
    public static DependencyProperty MaxListHeightProperty { get; } = DependencyProperty.Register(
        nameof(MaxListHeight), typeof(double), typeof(SearchableItemsFlyout), new PropertyMetadata(320d, OnOptionsChanged));

    /// <summary>Identifies the <see cref="UseAcrylicBackdrop"/> dependency property.</summary>
    public static DependencyProperty UseAcrylicBackdropProperty { get; } = DependencyProperty.Register(
        nameof(UseAcrylicBackdrop), typeof(bool), typeof(SearchableItemsFlyout),
        new PropertyMetadata(true, (sender, _) => ((SearchableItemsFlyout)sender).ApplyBackdrop()));

    /// <summary>Identifies the <see cref="ResetFilterOnOpen"/> dependency property.</summary>
    public static DependencyProperty ResetFilterOnOpenProperty { get; } = DependencyProperty.Register(
        nameof(ResetFilterOnOpen), typeof(bool), typeof(SearchableItemsFlyout), new PropertyMetadata(true));

    /// <summary>Identifies the <see cref="FilterBoxAutomationName"/> dependency property.</summary>
    public static DependencyProperty FilterBoxAutomationNameProperty { get; } = DependencyProperty.Register(
        nameof(FilterBoxAutomationName), typeof(string), typeof(SearchableItemsFlyout),
        new PropertyMetadata(null, OnOptionsChanged));

    /// <summary>Identifies the <see cref="FilterPlaceholderText"/> dependency property.</summary>
    public static DependencyProperty FilterPlaceholderTextProperty { get; } = DependencyProperty.Register(
        nameof(FilterPlaceholderText), typeof(string), typeof(SearchableItemsFlyout),
        new PropertyMetadata(null, OnOptionsChanged));

    /// <summary>Identifies the <see cref="IsResultCountVisible"/> dependency property.</summary>
    public static DependencyProperty IsResultCountVisibleProperty { get; } = DependencyProperty.Register(
        nameof(IsResultCountVisible), typeof(bool), typeof(SearchableItemsFlyout), new PropertyMetadata(false, OnOptionsChanged));

    /// <summary>Identifies the <see cref="NoResultsText"/> dependency property.</summary>
    public static DependencyProperty NoResultsTextProperty { get; } = DependencyProperty.Register(
        nameof(NoResultsText), typeof(string), typeof(SearchableItemsFlyout),
        new PropertyMetadata(null, OnOptionsChanged));

    internal SearchableDropDown? DropDown { get; init; }

    internal bool IsSelectionOnly => this.DropDown is not null;

    /// <summary>Gets the items displayed when <see cref="ItemsSource"/> is null. This is the default XAML content property.</summary>
    public ObservableCollection<SearchableFlyoutItem> Items { get; } = [];

    /// <summary>Gets or sets the sequence of item models to display in place of <see cref="Items"/>.</summary>
    public IEnumerable<SearchableFlyoutItem>? ItemsSource
    {
        get => (IEnumerable<SearchableFlyoutItem>?)this.GetValue(ItemsSourceProperty);
        set => this.SetValue(ItemsSourceProperty, value);
    }

    /// <summary>Gets or sets the position of the filter field. The default is <see cref="JPSoftworks.DesktopToolkit.Controls.FilterBoxPlacement.Auto"/>.</summary>
    public FilterBoxPlacement FilterBoxPlacement
    {
        get => (FilterBoxPlacement)this.GetValue(FilterBoxPlacementProperty);
        set => this.SetValue(FilterBoxPlacementProperty, value);
    }

    /// <summary>Gets or sets the localized labels and format strings. The default is <see cref="SearchableFlyoutStrings.Default"/>.</summary>
    public SearchableFlyoutStrings Strings
    {
        get => (SearchableFlyoutStrings?)this.GetValue(StringsProperty) ?? this._parentMenu?.Strings ?? SearchableFlyoutStrings.Default;
        set => this.SetValue(StringsProperty, value);
    }

    /// <summary>Gets or sets the filter text for the root level. Each word matches against item text or keywords, ignoring case.</summary>
    /// <remarks>A null value is treated as an empty string.</remarks>
    [AllowNull]
    public string FilterText
    {
        get => (string?)this.GetValue(FilterTextProperty) ?? string.Empty;
        set => this.SetValue(FilterTextProperty, value ?? string.Empty);
    }

    /// <summary>Gets or sets the requested width of the flyout in device-independent pixels. The default is 280.</summary>
    public double FlyoutWidth
    {
        get => (double)this.GetValue(FlyoutWidthProperty);
        set => this.SetValue(FlyoutWidthProperty, value);
    }

    /// <summary>Gets or sets the maximum height of the item list in device-independent pixels. The default is 320.</summary>
    public double MaxListHeight
    {
        get => (double)this.GetValue(MaxListHeightProperty);
        set => this.SetValue(MaxListHeightProperty, value);
    }

    /// <summary>Gets or sets whether this flyout and its submenus use an acrylic backdrop. The default is true.</summary>
    public bool UseAcrylicBackdrop
    {
        get => (bool)this.GetValue(UseAcrylicBackdropProperty);
        set => this.SetValue(UseAcrylicBackdropProperty, value);
    }

    /// <summary>Gets or sets whether opening the flyout clears its root filter. The default is true.</summary>
    public bool ResetFilterOnOpen
    {
        get => (bool)this.GetValue(ResetFilterOnOpenProperty);
        set => this.SetValue(ResetFilterOnOpenProperty, value);
    }

    /// <summary>Gets or sets the accessible name of the root filter field.</summary>
    public string FilterBoxAutomationName
    {
        get
        {
            if (this.GetValue(FilterBoxAutomationNameProperty) is string name && !string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            return this._parentMenu is { _submenuItem: { } item } parent
                ? parent.Strings.Submenu(SearchableFlyoutStrings.WithFallback(item.Text, item.AutomationName), parent.FilterBoxAutomationName)
                : this.DropDown?.FilterBoxAutomationName ?? SearchableFlyoutStrings.DefaultFilterBoxAutomationName;
        }
        set => this.SetValue(FilterBoxAutomationNameProperty, value);
    }

    /// <summary>Gets or sets the prompt shown in the filter field when its text is empty.</summary>
    /// <remarks>An empty string hides the prompt. Null restores the default.</remarks>
    [AllowNull]
    public string FilterPlaceholderText
    {
        get => WinUIResourceStrings.GetPlaceholderOverride((string?)this.GetValue(FilterPlaceholderTextProperty))
            ?? this.DropDown?.FilterPlaceholderText
            ?? this._parentMenu?.FilterPlaceholderText
            ?? SearchableFlyoutStrings.DefaultFilterPlaceholderText;
        set => WinUIResourceStrings.SetTextValue(this, FilterPlaceholderTextProperty, value);
    }

    /// <summary>Gets or sets the message shown when filtering leaves no matching items.</summary>
    public string NoResultsText
    {
        get => SearchableFlyoutStrings.WithFallback(
            (string?)this.GetValue(NoResultsTextProperty),
            this.DropDown?.NoResultsText ?? this._parentMenu?.NoResultsText ?? SearchableFlyoutStrings.DefaultNoResultsText);
        set => this.SetValue(NoResultsTextProperty, value);
    }

    /// <summary>Gets the number of matching items at the root level, excluding separators. The count is zero when closed.</summary>
    public int FilteredItemCount => this._filteredItems.Count(item => !item.IsSeparator);

    /// <summary>Gets or sets whether the result count is displayed below the list. The default is false.</summary>
    public bool IsResultCountVisible
    {
        get => (bool)this.GetValue(IsResultCountVisibleProperty);
        set => this.SetValue(IsResultCountVisibleProperty, value);
    }

    internal SearchableFlyoutItem? SelectedItem { get; set; }

    /// <summary>Gets the number of item containers currently realized by the virtualizing list.</summary>
    public int RealizedContainerCount => this.ItemsList.ItemsPanelRoot?.Children.OfType<ListViewItem>()
                                             .Count(item => this.ItemsList.IndexFromContainer(item) >= 0)
                                         ?? 0;

    /// <summary>Initializes a new instance of the <see cref="SearchableItemsFlyout"/> class.</summary>
    public SearchableItemsFlyout()
    {
        this.InitializeComponent();
        this._menuItemStyle = this.ItemsList.ItemContainerStyle;
        this.ItemsList.Menu = this;
        this._filterTimer = this.DispatcherQueue.CreateTimer();
        this._filterTimer.Interval = TimeSpan.FromMilliseconds(70);
        this._filterTimer.IsRepeating = false;
        this._filterTimer.Tick += (_, _) => this.ApplyFilter();
        this._hoverTimer = this.DispatcherQueue.CreateTimer();
        this._hoverTimer.Interval = TimeSpan.FromMilliseconds(400);
        this._hoverTimer.IsRepeating = false;
        this._hoverTimer.Tick += (_, _) =>
        {
            if (this._isOpen && this._hoveredItem is { CanInvoke: true } item)
            {
                if (item.HasChildren)
                {
                    this.OpenSubmenu(item, focusFilter: false);
                }
                else
                {
                    this.CloseSubmenu();
                }
            }
        };
        this.ApplyOptions();
        WinUIResourceStrings.ObserveChanges(this, () =>
        {
            if (this._isOpen)
            {
                this.ApplyOptions();
                this.UpdateResultText();
            }
        });
    }

    /// <summary>Rereads the current source and reapplies the filter while the flyout is open. Call this after modifying a collection that does not report changes.</summary>
    public void Refresh()
    {
        if (!this._isOpen)
        {
            return;
        }

        this.StopObserving();
        var pending = this.IsSelectionOnly ? (this.ItemsList.SelectedItem as FlyoutItemEntry)?.Item : null;
        var pendingIndex = pending is null ? -1 : Array.IndexOf(this._sourceItems, pending);
        var source = this.ItemsSource ?? this.Items;
        this._sourceItems = source.ToArray();
        this._observableSource = source as INotifyCollectionChanged;
        if (this._observableSource is not null)
        {
            this._observableSource.CollectionChanged += this.OnSourceCollectionChanged;
        }

        foreach (var item in this._sourceItems)
        {
            if (this._observedItems.Add(item))
            {
                item.PropertyChanged += this.OnItemPropertyChanged;
            }

            if (item.Command is { } command && this._observedCommands.Add(command))
            {
                command.CanExecuteChanged += this.OnCanExecuteChanged;
            }
        }

        this._entriesDirty = true;
        var index = MenuSearch.FindChoiceIndex(this._sourceItems, pending?.Tag, pendingIndex);
        this.ApplyFilter(index >= 0 ? this._sourceItems[index] : null);
    }

    private static void OnSourceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((SearchableItemsFlyout)sender).Refresh();
    }

    private static void OnOptionsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var updateResults = args.Property == StringsProperty || args.Property == NoResultsTextProperty;
        var inherited = updateResults || args.Property == FilterPlaceholderTextProperty || args.Property == FilterBoxAutomationNameProperty;
        for (SearchableItemsFlyout? menu = (SearchableItemsFlyout)sender; menu is not null; menu = inherited ? menu._submenu : null)
        {
            menu.ApplyOptions();
            if (menu._isOpen && updateResults)
            {
                menu.UpdateResultText();
            }
        }
    }

    private void ApplyBackdrop()
    {
        if (this.MenuBackdrop is not null)
        {
            MenuSearch.SetAcrylicBackdrop(this.MenuBackdrop, this.MenuThemedBackground, this.UseAcrylicBackdrop, rounded: true);
        }

        if (this._submenu is not null)
        {
            this._submenu.UseAcrylicBackdrop = this.UseAcrylicBackdrop;
        }
    }

    private static void OnFilterChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var menu = (SearchableItemsFlyout)sender;
        if (menu.FilterBox is null)
        {
            return;
        }

        var text = args.NewValue as string ?? string.Empty;
        if (menu.FilterBox.Text != text)
        {
            menu.FilterBox.Text = text;
        }

        if (menu._isOpen)
        {
            menu._filterTimer.Stop();
            menu._filterTimer.Start();
        }
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(Style))]
    private void ApplyOptions()
    {
        if (this.LayoutRoot is null)
        {
            return;
        }

        this.LayoutRoot.Width = double.IsFinite(this.FlyoutWidth) ? Math.Max(160, this.FlyoutWidth) : 280;
        this.ItemsList.ItemContainerStyle
            = this.IsSelectionOnly ? (Style)this.LayoutRoot.Resources["ChoiceItemStyle"] : this._menuItemStyle;
        if (this.IsSelectionOnly)
        {
            // Add to the 2-DIP row margin to match the 5-DIP side inset.
            this.ItemsList.Padding = new Thickness(0, 3, 0, 3);
        }

        this.ItemsList.MaxHeight = double.IsFinite(this.MaxListHeight) ? Math.Max(32, this.MaxListHeight) : 320;
        if (this.FilterBoxPlacement != FilterBoxPlacement.Auto)
        {
            this._effectiveFilterPlacement = this.FilterBoxPlacement;
        }

        this.ApplyFilterPlacement();
        this.FilterBox.PlaceholderText = this.FilterPlaceholderText;
        AutomationProperties.SetName(this.FilterBox, this.FilterBoxAutomationName);
        this.FilterBox.SetKeyboardHelp(this.Strings.KeyboardHelp);
        this.EmptyMessage.Text = this.NoResultsText;
        this.ResultCount.Visibility = this.IsResultCountVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ApplyFilterPlacement()
    {
        var atBottom = this._effectiveFilterPlacement == FilterBoxPlacement.Bottom;
        Grid.SetRow(this.FilterHost, atBottom ? 2 : 0);
        this.FilterHost.BorderThickness = atBottom ? new Thickness(0, 1, 0, 0) : new Thickness(0, 0, 0, 1);
        this.MenuSurface.VerticalAlignment = atBottom ? VerticalAlignment.Bottom : VerticalAlignment.Top;
    }

    private void OnOpening(object? sender, object args)
    {
        this.SystemBackdrop = null;
        this._isOpen = true;
        this.ApplyBackdrop();
        this.LayoutRoot.FlowDirection = this.Target?.FlowDirection ?? FlowDirection.LeftToRight;
        this.ApplyOptions();
        if (this.ResetFilterOnOpen)
        {
            this.FilterText = string.Empty;
        }

        this.Refresh();
        this.SetPopupHeight();
    }

    private void SetPopupHeight()
    {
        // Keep the native popup slot fixed; the visible surface shrinks away from its anchor edge.
        var listHeight = this.DropDown?.ItemTemplate is not null
            ? this.ItemsList.MaxHeight
            : Math.Min(
                this.ItemsList.MaxHeight,
                Math.Max(
                    40,
                    this._sourceItems.Where(item => item.IsVisible).Sum(item => item.IsSeparator ? 9 : this.IsSelectionOnly ? 37 : 32)));
        this.LayoutRoot.Height = listHeight + 48 + (this.IsResultCountVisible ? 24 : 0);
        this.MenuSurface.MaxHeight = this.LayoutRoot.Height;
    }

    private void OnOpened(object? sender, object args)
    {
        this._presenter = this.FindPresenter();
        if (this._presenter is not null && this.Target is not null)
        {
            this._presenter.RequestedTheme = this.Target.ActualTheme;
        }

        var popup = this._presenter is null ? null : MenuSearch.FindPopup(this._presenter);
        if (popup is not null)
        {
            popup.SystemBackdrop = null;
        }

        this.ConstrainSurface();
        if (this._presenter is not null)
        {
            this._presenter.SizeChanged += this.OnPresenterSizeChanged;
        }

        if (this.FilterBoxPlacement == FilterBoxPlacement.Auto && this.Target is not null)
        {
            this._effectiveFilterPlacement = MenuSearch.ResolvePlacement(popup, this.LayoutRoot, this.Target);
            this.ApplyFilterPlacement();
            this.LayoutRoot.UpdateLayout();
        }

        if (this.ItemsList.SelectedItem is FlyoutItemEntry selected)
        {
            this.ItemsList.ScrollIntoView(selected);
        }

        if (this._focusFilterOnOpen)
        {
            this.FocusFilter();
        }
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(FlyoutPresenter))]
    private FlyoutPresenter? FindPresenter()
    {
        for (DependencyObject? parent = this.LayoutRoot; parent is not null; parent = VisualTreeHelper.GetParent(parent))
        {
            if (parent is FlyoutPresenter presenter)
            {
                return presenter;
            }
        }

        return null;
    }

    private void OnPresenterSizeChanged(object sender, SizeChangedEventArgs args)
    {
        this.ConstrainSurface();
    }

    private void ConstrainSurface()
    {
        if (this._presenter is { ActualHeight: > 0 } && this._presenter.ActualHeight < this.LayoutRoot.Height)
        {
            this.LayoutRoot.Height = this._presenter.ActualHeight;
        }

        this.MenuSurface.MaxHeight = this.LayoutRoot.Height;
    }

    private void OnSlotPointerPressed(object sender, PointerRoutedEventArgs args)
    {
        var point = args.GetCurrentPoint(this.MenuSurface).Position;
        if (point.X < 0 || point.Y < 0 || point.X > this.MenuSurface.ActualWidth || point.Y > this.MenuSurface.ActualHeight)
        {
            this.Hide();
            args.Handled = true;
        }
    }

    private void OnClosed(object? sender, object args)
    {
        this._isOpen = false;
        this._hoverTimer.Stop();
        this._hoveredItem = null;
        if (this._presenter is not null)
        {
            this._presenter.SizeChanged -= this.OnPresenterSizeChanged;
            this._presenter = null;
        }

        this.CloseSubmenu();
        this._filterTimer.Stop();
        this.FilterBox.CancelAnnouncements();
        this.StopObserving();
        this._sourceItems = [];
        this._filteredItems = [];
        this._entries = [];
        this.ItemsList.ItemsSource = null;
        this.LayoutRoot.Height = double.NaN;
        this.MenuSurface.MaxHeight = double.PositiveInfinity;
    }

    private void StopObserving()
    {
        if (this._observableSource is not null)
        {
            this._observableSource.CollectionChanged -= this.OnSourceCollectionChanged;
            this._observableSource = null;
        }

        foreach (var item in this._observedItems)
        {
            item.PropertyChanged -= this.OnItemPropertyChanged;
        }

        foreach (var command in this._observedCommands)
        {
            command.CanExecuteChanged -= this.OnCanExecuteChanged;
        }

        this._observedItems.Clear();
        this._observedCommands.Clear();
    }

    private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        this.Refresh();
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (sender is SearchableFlyoutItem changed)
        {
            this.NotifyAutomationState(changed);
        }

        if (args.PropertyName == nameof(SearchableFlyoutItem.Command))
        {
            this.Refresh();
        }
        else if (args.PropertyName is nameof(SearchableFlyoutItem.Text) or nameof(SearchableFlyoutItem.Keywords)
                 or nameof(SearchableFlyoutItem.IsVisible) or nameof(SearchableFlyoutItem.IsSeparator)
                 or nameof(SearchableFlyoutItem.IsCheckable) or nameof(SearchableFlyoutItem.Glyph)
                 or nameof(SearchableFlyoutItem.IconSource) or nameof(SearchableFlyoutItem.HasChildren))
        {
            this._entriesDirty = true;
            this._filterTimer.Stop();
            this._filterTimer.Start();
        }
        else if (args.PropertyName == nameof(SearchableFlyoutItem.CanInvoke))
        {
            this.UpdateRealizedContainers();
        }
    }

    private void OnCanExecuteChanged(object? sender, EventArgs args)
    {
        if (!this.DispatcherQueue.HasThreadAccess)
        {
            this.DispatcherQueue.TryEnqueue(() => this.OnCanExecuteChanged(sender, args));
            return;
        }

        this.UpdateRealizedContainers();
    }

    private void OnFilterTextChanged(object sender, TextChangedEventArgs args)
    {
        this.FilterText = this.FilterBox.Text;
    }

    private void ApplyFilter(SearchableFlyoutItem? preferredItem = null)
    {
        this._filterTimer.Stop();
        this._hoverTimer.Stop();
        this._hoveredItem = null;
        this.CloseSubmenu();
        var current = (this.ItemsList.SelectedItem as FlyoutItemEntry)?.Item;
        var selected = preferredItem ?? (current is not null && this._sourceItems.Contains(current) ? current : this.SelectedItem);
        var tokens = MenuSearch.Tokenize(this.FilterText);
        var matches = MenuSearch.Filter(
            this._sourceItems, item => item.IsVisible, item => item.IsSeparator,
            item => MenuSearch.Matches(item.Text, item.Keywords, tokens));

        if (!this.IsSelectionOnly || this._entriesDirty || !this._filteredItems.SequenceEqual(matches))
        {
            this._filteredItems = matches;
            var position = 0;
            var resultCount = this.FilteredItemCount;
            var hasChecks = !this.IsSelectionOnly && this._sourceItems.Any(item => item.IsVisible && item.IsCheckable);
            var hasIcons = this._sourceItems.Any(item =>
                item.IsVisible && (!string.IsNullOrEmpty(item.Glyph) || item.IconSource is not null));
            var choiceTemplate = this.DropDown?.ItemTemplate;
            this._entries = this._filteredItems.Select(item =>
                new FlyoutItemEntry(item, hasChecks, hasIcons, this.LayoutRoot.FlowDirection == FlowDirection.RightToLeft)
                {
                    ChoiceTemplate = choiceTemplate, PositionInSet = item.IsSeparator ? 0 : ++position, SizeOfSet = resultCount
                }).ToArray();

            // Replace the view in one update when results change.
            this._entriesDirty = false;
            this.ItemsList.ItemsSource = this._entries;
        }

        var selectedEntry
            = this._entries.FirstOrDefault(entry => selected is not null && ReferenceEquals(entry.Item, selected) && selected.CanInvoke)
              ?? this._entries.FirstOrDefault(entry => entry.Item.CanInvoke);
        if (!ReferenceEquals(this.ItemsList.SelectedItem, selectedEntry))
        {
            this.ItemsList.SelectedItem = selectedEntry;
        }

        this.EmptyMessage.Visibility = this._filteredItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        this.ItemsList.Visibility = this._filteredItems.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        this.UpdateResultText();
        this.FilterChanged?.Invoke(this, null);
    }

    private void UpdateResultText()
    {
        var resultCount = this.FilteredItemCount;
        this.ResultCount.Text = this.Strings.Count(
            resultCount, this._sourceItems.Count(item => item.IsVisible && !item.IsSeparator), this.IsSelectionOnly);
        var menuName = AutomationProperties.GetName(this);
        AutomationProperties.SetName(
            this.ItemsList,
            SearchableFlyoutStrings.WithFallback(menuName, this.Strings.ListName(this.IsSelectionOnly)));
        var status = this.Strings.Matches(resultCount, this.IsSelectionOnly);
        AutomationProperties.SetHelpText(this.ItemsList, status);
        this.FilterBox.SetResultStatus(resultCount == 0 ? this.NoResultsText : status);
    }

    private void OnListSizeChanged(object sender, SizeChangedEventArgs args)
    {
        if (this.IsSelectionOnly && args.NewSize.Width != args.PreviousSize.Width && this.ItemsList.ItemsPanelRoot is { } panel)
        {
            foreach (var container in panel.Children.OfType<ListViewItem>())
            {
                container.MinWidth = args.NewSize.Width;
            }
        }
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(ListViewItem))]
    private void OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (!args.InRecycleQueue && args.Item is FlyoutItemEntry entry && args.ItemContainer is ListViewItem container)
        {
            this.UpdateContainer(container, entry.Item);
            if (this.IsSelectionOnly)
            {
                // Fill the viewport when WinUI measures a narrower row slot.
                container.MinWidth = this.ItemsList.ActualWidth;
            }

            AutomationProperties.SetPositionInSet(container, entry.PositionInSet);
            AutomationProperties.SetSizeOfSet(container, entry.SizeOfSet);
        }
    }

    private void UpdateContainer(ListViewItem container, SearchableFlyoutItem item)
    {
        container.IsEnabled = item.CanInvoke;
        container.IsTabStop = !item.IsSeparator;
        container.IsHitTestVisible = !item.IsSeparator;
        if (item.IsSeparator)
        {
            container.MinHeight = 0;
        }
        else
        {
            container.ClearValue(FrameworkElement.MinHeightProperty);
        }

        container.Height = item.IsSeparator ? 9 : double.NaN;
        if (item.IsSeparator)
        {
            container.Padding = new Thickness(0);
        }
        else
        {
            container.ClearValue(Control.PaddingProperty);
        }

        container.PointerEntered -= this.OnItemPointerEntered;
        container.PointerExited -= this.OnItemPointerExited;
        if (!this.IsSelectionOnly)
        {
            container.PointerEntered += this.OnItemPointerEntered;
            container.PointerExited += this.OnItemPointerExited;
        }

        AutomationProperties.SetName(
            container, item.IsSeparator ? string.Empty : string.IsNullOrEmpty(item.AutomationName) ? item.Text : item.AutomationName);
        AutomationProperties.SetHelpText(container, item.AutomationHelpText);
        AutomationProperties.SetAutomationId(container, item.AutomationId);
    }

    internal void UpdateChoiceTemplate()
    {
        if (!this._isOpen)
        {
            return;
        }

        this._entriesDirty = true;
        this.ApplyFilter();
        this.SetPopupHeight();
    }

    private void UpdateRealizedContainers()
    {
        if (this.ItemsList.ItemsPanelRoot is not { } panel)
        {
            return;
        }

        foreach (var container in panel.Children.OfType<ListViewItem>())
        {
            if (this.ItemsList.ItemFromContainer(container) is FlyoutItemEntry entry)
            {
                this.UpdateContainer(container, entry.Item);
            }
        }
    }

    private void OnItemClick(object sender, ItemClickEventArgs args)
    {
        this.InvokeItem(((FlyoutItemEntry)args.ClickedItem).Item);
    }

    internal void InvokeItem(SearchableFlyoutItem? item)
    {
        if (item is null || !item.CanInvoke)
        {
            return;
        }

        if (item.HasChildren)
        {
            this.OpenSubmenu(item);
            return;
        }

        if (item.IsCheckable && !this.IsSelectionOnly)
        {
            item.IsChecked = !item.IsChecked;
        }

        var root = this;
        while (root._parentMenu is not null)
        {
            root = root._parentMenu;
        }

        root.CloseSubmenu();
        root.Hide();
        if (!this.IsSelectionOnly)
        {
            item.Command?.Execute(item.CommandParameter);
        }

        this.ItemInvoked?.Invoke(this, new FlyoutItemInvokedEventArgs(item));
    }

    internal bool IsSubmenuOpen(SearchableFlyoutItem item)
    {
        return ReferenceEquals(this._submenuItem, item) && this._submenu is { _isOpen: true };
    }

    internal void CollapseSubmenu(SearchableFlyoutItem item)
    {
        if (ReferenceEquals(this._submenuItem, item))
        {
            this.CloseSubmenu();
        }
    }

    private void NotifyAutomationState(SearchableFlyoutItem item)
    {
        if (FrameworkElementAutomationPeer.FromElement(this.ItemsList) is FlyoutItemsListAutomationPeer peer)
        {
            peer.NotifyItemChanged(item);
        }
    }

    private void FocusFilter()
    {
        this.FilterBox.Focus(FocusState.Programmatic);
        this.FilterBox.SelectAll();
    }

    private void FocusFilterForEditing()
    {
        this.FilterBox.FocusForEditing();
    }

    private void OnListCharacterReceived(UIElement sender, CharacterReceivedRoutedEventArgs args)
    {
        if (!this._isOpen || char.IsControl(args.Character))
        {
            return;
        }

        var control = MenuFilterBox.IsKeyDown(VirtualKey.Control);
        var alt = MenuFilterBox.IsKeyDown(VirtualKey.Menu);
        if (control != alt)
        {
            return;
        }

        this.FilterBox.InsertCharacter(args.Character);
        args.Handled = true;
    }

    private void MoveFromList(int start, int direction)
    {
        if (this.IsSelectionOnly)
        {
            this.FocusItem(start, direction);
            return;
        }

        for (var index = start; index >= 0 && index < this._filteredItems.Count; index += direction)
        {
            if (this._filteredItems[index].CanInvoke)
            {
                this.FocusItem(index, direction);
                return;
            }
        }

        this.FocusFilterForEditing();
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(ListViewItem))]
    private void OnItemPointerEntered(object sender, PointerRoutedEventArgs args)
    {
        if (args.Pointer.PointerDeviceType == PointerDeviceType.Mouse
            && sender is ListViewItem container
            && this.ItemsList.ItemFromContainer(container) is FlyoutItemEntry { Item.CanInvoke: true } entry)
        {
            this._hoveredItem = entry.Item;
            this._hoverTimer.Stop();
            this._hoverTimer.Start();
        }
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(ListViewItem))]
    private void OnItemPointerExited(object sender, PointerRoutedEventArgs args)
    {
        if (sender is ListViewItem container
            && this.ItemsList.ItemFromContainer(container) is FlyoutItemEntry entry
            && ReferenceEquals(this._hoveredItem, entry.Item))
        {
            this._hoveredItem = null;
            this._hoverTimer.Stop();
        }
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(ListViewItem))]
    internal void OpenSubmenu(SearchableFlyoutItem item, bool focusFilter = true)
    {
        if (!item.HasChildren || !item.CanInvoke)
        {
            return;
        }

        if (ReferenceEquals(this._submenuItem, item) && this._submenu is { _isOpen: true })
        {
            if (focusFilter)
            {
                this._submenu.FocusFilter();
            }

            return;
        }

        this.CloseSubmenu();
        var entry = this._entries.FirstOrDefault(entry => ReferenceEquals(entry.Item, item));
        if (entry is null)
        {
            return;
        }

        this.ItemsList.SelectedItem = entry;
        this.ItemsList.ScrollIntoView(entry);
        this.ItemsList.UpdateLayout();
        if (this.ItemsList.ContainerFromItem(entry) is not ListViewItem anchor)
        {
            return;
        }

        var submenu = new SearchableItemsFlyout
        {
            ItemsSource = item.Items,
            FilterBoxPlacement = this.FilterBoxPlacement,
            FlyoutWidth = this.FlyoutWidth,
            MaxListHeight = this.MaxListHeight,
            UseAcrylicBackdrop = this.UseAcrylicBackdrop,
            IsResultCountVisible = this.IsResultCountVisible,
            Placement = FlyoutPlacementMode.RightEdgeAlignedTop,
            ShowMode = focusFilter ? FlyoutShowMode.Standard : FlyoutShowMode.Transient,
            _focusFilterOnOpen = focusFilter,
            _parentMenu = this,
            _depth = this._depth + 1,
        };
        var suffix = $"Depth{submenu._depth}";
        AutomationProperties.SetAutomationId(submenu.FilterBox, $"MenuFilterBox{suffix}");
        AutomationProperties.SetAutomationId(submenu.ItemsList, $"FilteredMenuItems{suffix}");
        AutomationProperties.SetAutomationId(submenu.ResultCount, $"MenuResultCount{suffix}");
        AutomationProperties.SetAutomationId(submenu.EmptyMessage, $"MenuEmptyMessage{suffix}");
        AutomationProperties.SetAutomationId(submenu.MenuSurface, $"MenuSurface{suffix}");
        submenu.ItemInvoked += (_, args) => this.ItemInvoked?.Invoke(this, args);
        submenu.Closed += (_, _) =>
        {
            if (ReferenceEquals(this._submenu, submenu))
            {
                this._submenu = null;
                this._submenuItem = null;
                this.NotifyAutomationState(item);
            }
        };
        this._submenu = submenu;
        this._submenuItem = item;
        // Inset the exclusion area to allow the native 5-DIP submenu overlap.
        const double overlap = 5;
        var menuBounds = this.MenuSurface.TransformToVisual(anchor).TransformBounds(
            new Rect(0, 0, this.MenuSurface.ActualWidth, this.MenuSurface.ActualHeight));
        var exclusionRect = new Rect(menuBounds.X + overlap, 0, Math.Max(0, menuBounds.Width - 2 * overlap), anchor.ActualHeight);
        submenu.ShowAt(
            anchor,
            new FlyoutShowOptions
            {
                Position = new Point(exclusionRect.Right, 0),
                ExclusionRect = exclusionRect,
                Placement = submenu.Placement,
                ShowMode = submenu.ShowMode,
            });
        this.NotifyAutomationState(item);
    }

    private void CloseSubmenu()
    {
        var submenu = this._submenu;
        var item = this._submenuItem;
        this._submenu = null;
        this._submenuItem = null;
        submenu?.Hide();
        if (item is not null)
        {
            this.NotifyAutomationState(item);
        }
    }

    private void OnFilterKeyDown(object sender, KeyRoutedEventArgs args)
    {
        if (args.Key is VirtualKey.Down or VirtualKey.Up)
        {
            this.FlushFilter();
            var direction = args.Key == VirtualKey.Down ? 1 : -1;
            if (this.IsSelectionOnly
                && this.ItemsList.SelectedItem is FlyoutItemEntry { Item.CanInvoke: true } entry
                && ReferenceEquals(entry.Item, this.SelectedItem))
            {
                this.MoveFromList(this.ItemsList.SelectedIndex + direction, direction);
            }
            else
            {
                this.FocusItem(direction > 0 ? 0 : this._filteredItems.Count - 1, direction);
            }

            args.Handled = true;
        }
        else if (args.Key == VirtualKey.Enter)
        {
            this.FlushFilter();
            this.InvokeItem((this.ItemsList.SelectedItem as FlyoutItemEntry)?.Item);
            args.Handled = true;
        }
    }

    private void OnListKeyDown(object sender, KeyRoutedEventArgs args)
    {
        if (this.FilterBox.HandleListEditingKey(args))
        {
            return;
        }

        this.FlushFilter();
        var index = this.ItemsList.SelectedIndex;
        var openKey = this.LayoutRoot.FlowDirection == FlowDirection.RightToLeft ? VirtualKey.Left : VirtualKey.Right;
        var closeKey = this.LayoutRoot.FlowDirection == FlowDirection.RightToLeft ? VirtualKey.Right : VirtualKey.Left;
        if (args.Key == openKey && this.ItemsList.SelectedItem is FlyoutItemEntry { Item.HasChildren: true } entry)
        {
            this.OpenSubmenu(entry.Item);
            args.Handled = true;
            return;
        }

        if (args.Key == closeKey && this._parentMenu is not null)
        {
            this.HideAndRestoreParentFocus();
            args.Handled = true;
            return;
        }

        switch (args.Key)
        {
            case VirtualKey.Down:
                this.MoveFromList(index + 1, 1);
                break;
            case VirtualKey.Up:
                this.MoveFromList(index - 1, -1);
                break;
            case VirtualKey.Home:
                this.FocusItem(0, 1);
                break;
            case VirtualKey.End:
                this.FocusItem(this._filteredItems.Count - 1, -1);
                break;
            case VirtualKey.Enter:
            case VirtualKey.Space:
                this.InvokeItem((this.ItemsList.SelectedItem as FlyoutItemEntry)?.Item);
                break;
            default:
                return;
        }

        args.Handled = true;
    }

    private void FlushFilter()
    {
        if (this._filterTimer.IsRunning)
        {
            this.ApplyFilter();
        }
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(ListViewItem))]
    private void FocusItem(int start, int direction)
    {
        var index = MenuSearch.FindInvokableIndex(this._filteredItems, start, direction);
        if (index < 0)
        {
            return;
        }

        this.ItemsList.SelectedIndex = index;
        var entry = this._entries[index];
        this.ItemsList.ScrollIntoView(entry);
        this.ItemsList.UpdateLayout();
        if (this.ItemsList.ContainerFromIndex(index) is not ListViewItem container || !container.Focus(FocusState.Keyboard))
        {
            // A freshly filtered container may not be loaded until the next layout pass.
            this.DispatcherQueue.TryEnqueue(
                DispatcherQueuePriority.Low, [WinRT.DynamicWindowsRuntimeCast(typeof(ListViewItem))]() =>
                {
                    if (this._isOpen
                        && ReferenceEquals(this.ItemsList.SelectedItem, entry)
                        && this.ItemsList.ContainerFromItem(entry) is ListViewItem readyContainer)
                    {
                        readyContainer.Focus(FocusState.Keyboard);
                    }
                });
        }
    }

    private void HideAndRestoreParentFocus()
    {
        var parent = this._parentMenu;
        var item = parent?._submenuItem;
        this.Hide();
        if (parent is null || item is null)
        {
            return;
        }

        parent.DispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low, () =>
            {
                if (!parent._isOpen || parent._submenu is not null && !ReferenceEquals(parent._submenu, this))
                {
                    return;
                }

                var index = parent._filteredItems.FindIndex(candidate => ReferenceEquals(candidate, item));
                if (index >= 0)
                {
                    parent.FocusItem(index, 1);
                }
            });
    }

    private void OnRootKeyDown(object sender, KeyRoutedEventArgs args)
    {
        if (args.Key == VirtualKey.Escape)
        {
            this.HideAndRestoreParentFocus();
            args.Handled = true;
        }
        else if (args.Key == VirtualKey.F
                 && InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down)
                 && !InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
        {
            this.FilterBox.Focus(FocusState.Keyboard);
            this.FilterBox.SelectAll();
            args.Handled = true;
        }
    }
}
