// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using VirtualKey = Windows.System.VirtualKey;

namespace JPSoftworks.DesktopToolkit.Controls;

/// <summary>Presents a single selection from a searchable dropdown list.</summary>
/// <remarks>Typing filters the open list. Choosing a result commits the original source object. Escape dismisses the list and keeps the prior selection.</remarks>
[ContentProperty(Name = nameof(Items))]
public sealed partial class SearchableDropDown : DropDownButton
{
    /// <summary>Occurs when the committed selection changes. <see cref="SelectionChangedEventArgs.AddedItems"/> and <see cref="SelectionChangedEventArgs.RemovedItems"/> contain the original source objects.</summary>
    public event SelectionChangedEventHandler? SelectionChanged;

    private readonly SearchableItemsFlyout _menu;
    private readonly DispatcherQueueTimer _refreshTimer;

    private readonly HashSet<INotifyPropertyChanged> _observedValues = new(ReferenceEqualityComparer.Instance);

    // Avoid WinUI failures when an IconSourceElement switches through null.
    private readonly BitmapIconSource _emptyIconSource = new();
    private string _displayedText = string.Empty;
    private List<SearchableFlyoutItem> _options = [];
    private INotifyCollectionChanged? _observableSource;
    private bool _syncingSelection;
    private bool _syncingOpen;
    private Func<object, string>? _itemTextSelector;

    /// <summary>Identifies the <see cref="Strings"/> dependency property.</summary>
    public static DependencyProperty StringsProperty { get; } = DependencyProperty.Register(
        nameof(Strings), typeof(SearchableFlyoutStrings), typeof(SearchableDropDown), new PropertyMetadata(null, OnDisplayChanged));

    /// <summary>Identifies the <see cref="ItemsSource"/> dependency property.</summary>
    public static DependencyProperty ItemsSourceProperty { get; } = DependencyProperty.Register(
        nameof(ItemsSource), typeof(IEnumerable), typeof(SearchableDropDown), new PropertyMetadata(null, OnSourceChanged));

    /// <summary>Identifies the <see cref="ItemTemplate"/> dependency property.</summary>
    public static DependencyProperty ItemTemplateProperty { get; } = DependencyProperty.Register(
        nameof(ItemTemplate), typeof(DataTemplate), typeof(SearchableDropDown),
        new PropertyMetadata(null, (sender, _) => ((SearchableDropDown)sender)._menu?.UpdateChoiceTemplate()));

    /// <summary>Identifies the <see cref="SelectedItem"/> dependency property.</summary>
    public static DependencyProperty SelectedItemProperty { get; } = DependencyProperty.Register(
        nameof(SelectedItem), typeof(object), typeof(SearchableDropDown), new PropertyMetadata(null, OnSelectionChanged));

    /// <summary>Identifies the <see cref="SelectedIndex"/> dependency property.</summary>
    public static DependencyProperty SelectedIndexProperty { get; } = DependencyProperty.Register(
        nameof(SelectedIndex), typeof(int), typeof(SearchableDropDown), new PropertyMetadata(-1, OnIndexChanged));

    /// <summary>Identifies the <see cref="PlaceholderText"/> dependency property.</summary>
    public static DependencyProperty PlaceholderTextProperty { get; } = DependencyProperty.Register(
        nameof(PlaceholderText), typeof(string), typeof(SearchableDropDown), new PropertyMetadata("Select an item", OnDisplayChanged));

    /// <summary>Identifies the <see cref="FilterPlaceholderText"/> dependency property.</summary>
    public static DependencyProperty FilterPlaceholderTextProperty { get; } = DependencyProperty.Register(
        nameof(FilterPlaceholderText), typeof(string), typeof(SearchableDropDown), new PropertyMetadata("Search...", OnDisplayChanged));

    /// <summary>Identifies the <see cref="UseAcrylicBackdrop"/> dependency property.</summary>
    public static DependencyProperty UseAcrylicBackdropProperty { get; } = DependencyProperty.Register(
        nameof(UseAcrylicBackdrop), typeof(bool), typeof(SearchableDropDown), new PropertyMetadata(false, OnBackdropChanged));

    /// <summary>Identifies the <see cref="IsDropDownOpen"/> dependency property.</summary>
    public static DependencyProperty IsDropDownOpenProperty { get; } = DependencyProperty.Register(
        nameof(IsDropDownOpen), typeof(bool), typeof(SearchableDropDown), new PropertyMetadata(false, OnOpenChanged));

    /// <summary>Gets or sets the localized labels and format strings. The default is <see cref="SearchableFlyoutStrings.Default"/>.</summary>
    public SearchableFlyoutStrings Strings
    {
        get => (SearchableFlyoutStrings?)this.GetValue(StringsProperty) ?? SearchableFlyoutStrings.Default;
        set => this.SetValue(StringsProperty, value);
    }

    /// <summary>Gets the items displayed when <see cref="ItemsSource"/> is null. This is the default XAML content property.</summary>
    public ObservableCollection<object> Items { get; } = [];

    /// <summary>Gets or sets the sequence of choices displayed in place of <see cref="Items"/>.</summary>
    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)this.GetValue(ItemsSourceProperty);
        set => this.SetValue(ItemsSourceProperty, value);
    }

    /// <summary>Gets or sets the template used to display each choice in the open dropdown. The default is null.</summary>
    /// <remarks>The template receives the original item from <see cref="Items"/> or <see cref="ItemsSource"/>.
    /// <see cref="ItemTextSelector"/> supplies the closed selection text, search label, and default automation name.</remarks>
    public DataTemplate? ItemTemplate
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(DataTemplate))]
        get => (DataTemplate?)this.GetValue(ItemTemplateProperty);
        set => this.SetValue(ItemTemplateProperty, value);
    }

    /// <summary>Gets or sets the selected source object. The default is null.</summary>
    public object? SelectedItem
    {
        get => this.GetValue(SelectedItemProperty);
        set => this.SetValue(SelectedItemProperty, value);
    }

    /// <summary>Gets or sets the zero-based source index. A value of -1 or the index of a null entry clears the selection.</summary>
    public int SelectedIndex
    {
        get => (int)this.GetValue(SelectedIndexProperty);
        set => this.SetValue(SelectedIndexProperty, value);
    }

    /// <summary>Gets or sets the prompt shown on the closed control when no item is selected.</summary>
    public string PlaceholderText
    {
        get => (string)this.GetValue(PlaceholderTextProperty);
        set => this.SetValue(PlaceholderTextProperty, value);
    }

    /// <summary>Gets or sets the prompt shown in the filter field when its text is empty.</summary>
    public string FilterPlaceholderText
    {
        get => (string)this.GetValue(FilterPlaceholderTextProperty);
        set => this.SetValue(FilterPlaceholderTextProperty, value);
    }

    /// <summary>Gets or sets whether the open dropdown uses an acrylic backdrop. The default is false.</summary>
    public bool UseAcrylicBackdrop
    {
        get => (bool)this.GetValue(UseAcrylicBackdropProperty);
        set => this.SetValue(UseAcrylicBackdropProperty, value);
    }

    /// <summary>Gets or sets whether the dropdown is open.</summary>
    public bool IsDropDownOpen
    {
        get => (bool)this.GetValue(IsDropDownOpenProperty);
        set => this.SetValue(IsDropDownOpenProperty, value);
    }

    /// <summary>Gets or sets the function that supplies display and filter text. By default the control uses <see cref="SearchableFlyoutItem.Text"/> or the object's string representation.</summary>
    public Func<object, string>? ItemTextSelector
    {
        get => this._itemTextSelector;
        set
        {
            this._itemTextSelector = value;
            this.Refresh();
        }
    }

    /// <summary>Gets the flyout that displays the choices. Use it to configure filter placement, result text, and the list height.</summary>
    public SearchableItemsFlyout DropDownFlyout => this._menu;

    internal string SelectedText => this.SelectedItem is null ? string.Empty : this.GetItemText(this.SelectedItem);

    /// <summary>Initializes a new instance of the <see cref="SearchableDropDown"/> class.</summary>
    public SearchableDropDown()
    {
        this.InitializeComponent();
        this._refreshTimer = this.DispatcherQueue.CreateTimer();
        this._refreshTimer.Interval = TimeSpan.FromMilliseconds(70);
        this._refreshTimer.IsRepeating = false;
        this._refreshTimer.Tick += (_, _) => this.Refresh();
        this._menu = new SearchableItemsFlyout
        {
            DropDown = this,
            FilterPlaceholderText = this.FilterPlaceholderText,
            FilterBoxAutomationName = "Filter choices",
            NoResultsText = "No matching choices",
            UseAcrylicBackdrop = this.UseAcrylicBackdrop,
        };
        this.Flyout = this._menu;
        this._menu.Opening += (_, _) => this._menu.FlyoutWidth = Math.Max(this.ActualWidth, this.MinWidth);
        this._menu.Opened += (_, _) => this.SetOpenState(true);
        this._menu.Closed += (_, _) => this.SetOpenState(false);
        this._menu.ItemInvoked += (_, args) =>
        {
            var index = this._options.IndexOf(args.InvokedItem);
            if (index >= 0)
            {
                this.SelectedIndex = index;
            }
        };
        this.Items.CollectionChanged += this.OnCollectionChanged;
        this.UpdateDisplay();
    }

    private static void OnBackdropChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (((SearchableDropDown)sender)._menu is { } menu)
        {
            menu.UseAcrylicBackdrop = (bool)args.NewValue;
        }
    }

    /// <summary>Rereads the choices and display text and synchronizes the selection. Call this after changes to a non-observable source.</summary>
    public void Refresh()
    {
        this._refreshTimer.Stop();
        this.StopObservingValues();
        this._options = (this.ItemsSource ?? this.Items).Cast<object?>().Select(this.CreateOption).ToList();
        this.ReconcileSelection();
        this._menu.ItemsSource = this._options;
    }

    private SearchableFlyoutItem CreateOption(object? value)
    {
        if (value is null)
        {
            return new SearchableFlyoutItem { IsVisible = false };
        }

        if (this.IsLoaded && value is INotifyPropertyChanged observable && this._observedValues.Add(observable))
        {
            observable.PropertyChanged += this.OnValuePropertyChanged;
        }

        var item = value as SearchableFlyoutItem;
        return new SearchableFlyoutItem
        {
            Text = this.GetItemText(value),
            Keywords = item?.Keywords ?? string.Empty,
            Glyph = item?.Glyph ?? string.Empty,
            IconSource = item?.IconSource,
            IsVisible = item?.IsVisible ?? true,
            IsSeparator = item?.IsSeparator ?? false,
            IsEnabled = item?.IsEnabled ?? true,
            AutomationName = item?.AutomationName ?? string.Empty,
            AutomationHelpText = item?.AutomationHelpText ?? string.Empty,
            AutomationId = item?.AutomationId ?? string.Empty,
            Tag = value,
        };
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new SearchableDropDownAutomationPeer(this);
    }

    private string GetItemText(object value)
    {
        return this.ItemTextSelector?.Invoke(value) ?? (value is SearchableFlyoutItem item ? item.Text : value.ToString()) ?? string.Empty;
    }

    private static void OnSourceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var picker = (SearchableDropDown)sender;
        picker.ObserveSource();
        picker.Refresh();
    }

    private static void OnSelectionChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var picker = (SearchableDropDown)sender;
        if (picker._syncingSelection)
        {
            return;
        }

        var index = MenuSearch.FindChoiceIndex(picker._options, args.NewValue, picker.SelectedIndex);
        var item = index >= 0 ? picker._options[index].Tag : picker.IsLoaded ? null : args.NewValue;
        picker.SetSelection(index, item, args.OldValue);
    }

    private static void OnIndexChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var picker = (SearchableDropDown)sender;
        var index = picker.SelectedIndex;
        if (picker._syncingSelection || (!picker.IsLoaded && index >= picker._options.Count))
        {
            return;
        }

        picker.SelectIndex(index);
    }

    private void ReconcileSelection()
    {
        var index = this.SelectedItem is { } item
            ? MenuSearch.FindChoiceIndex(this._options, item, this.SelectedIndex)
            : this.SelectedIndex;
        if (!this.IsLoaded && (index >= this._options.Count || (index < 0 && this.SelectedItem is not null)))
        {
            this._menu.SelectedItem = null;
            this.UpdateDisplay();
            return;
        }

        this.SelectIndex(index);
    }

    private void SelectIndex(int index)
    {
        if (index < 0 || index >= this._options.Count || this._options[index].Tag is null)
        {
            index = -1;
        }

        this.SetSelection(index, index >= 0 ? this._options[index].Tag : null, this.SelectedItem);
    }

    private void SetSelection(int index, object? item, object? previousItem)
    {
        this._syncingSelection = true;
        try
        {
            if (this.SelectedIndex != index)
            {
                this.SelectedIndex = index;
            }

            if (!SameItem(this.SelectedItem, item))
            {
                this.SelectedItem = item;
            }
        }
        finally
        {
            this._syncingSelection = false;
        }

        this._menu.SelectedItem = index >= 0 ? this._options[index] : null;
        this.UpdateDisplay();
        if (!SameItem(previousItem, item))
        {
            this.SelectionChanged?.Invoke(
                this, new SelectionChangedEventArgs(previousItem is null ? [] : [previousItem], item is null ? [] : [item]));
        }
    }

    private static bool SameItem(object? left, object? right)
    {
        return ReferenceEquals(left, right) || (left is ValueType or string && Equals(left, right));
    }

    private static void OnDisplayChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((SearchableDropDown)sender).UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (this.SelectedTextBlock is null)
        {
            return;
        }

        var oldText = this._displayedText;
        this._displayedText = this.SelectedText;
        this.SelectedTextBlock.Text = this._displayedText;
        if (oldText != this._displayedText && FrameworkElementAutomationPeer.FromElement(this) is { } peer)
        {
            peer.RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty, oldText, this._displayedText);
        }

        this.SelectedTextBlock.Visibility = this.SelectedItem is null ? Visibility.Collapsed : Visibility.Visible;
        var iconSource = (this.SelectedItem as SearchableFlyoutItem)?.IconSource;
        this.SelectedSourceIcon.IconSource = iconSource ?? this._emptyIconSource;
        this.SelectedSourceIconHost.Visibility = iconSource is null ? Visibility.Collapsed : Visibility.Visible;
        this.SelectedGlyphIcon.Glyph = this.SelectedItem is SearchableFlyoutItem { IconSource: null } item ? item.Glyph : string.Empty;
        this.SelectedGlyphIcon.Visibility = FlyoutItemEntry.TextVisibility(this.SelectedGlyphIcon.Glyph);
        this.PlaceholderTextBlock.Text = this.PlaceholderText;
        this.PlaceholderTextBlock.Visibility = this.SelectedItem is null ? Visibility.Visible : Visibility.Collapsed;
        if (this._menu is not null)
        {
            this._menu.FilterPlaceholderText = this.FilterPlaceholderText;
            this._menu.Strings = this.Strings;
        }
    }

    private static void OnOpenChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var picker = (SearchableDropDown)sender;
        if (picker._syncingOpen || !picker.IsLoaded)
        {
            return;
        }

        if (picker.IsDropDownOpen && picker.IsEnabled)
        {
            picker._menu.ShowAt(picker);
        }
        else
        {
            picker._menu.Hide();
        }
    }

    private void SetOpenState(bool isOpen)
    {
        this._syncingOpen = true;
        this.IsDropDownOpen = isOpen;
        this._syncingOpen = false;
    }

    private void ObserveSource()
    {
        if (this._observableSource is not null)
        {
            this._observableSource.CollectionChanged -= this.OnCollectionChanged;
            this._observableSource = null;
        }

        if (this.IsLoaded && this.ItemsSource is INotifyCollectionChanged source && !ReferenceEquals(source, this.Items))
        {
            this._observableSource = source;
            source.CollectionChanged += this.OnCollectionChanged;
        }
    }

    private void StopObservingValues()
    {
        foreach (var value in this._observedValues)
        {
            value.PropertyChanged -= this.OnValuePropertyChanged;
        }

        this._observedValues.Clear();
    }

    private void OnValuePropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (!this.DispatcherQueue.HasThreadAccess)
        {
            this.DispatcherQueue.TryEnqueue(() => this.OnValuePropertyChanged(sender, args));
            return;
        }

        if (!this.IsLoaded)
        {
            return;
        }

        this._refreshTimer.Stop();
        this._refreshTimer.Start();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        if (!ReferenceEquals(sender, this.ItemsSource ?? this.Items))
        {
            return;
        }

        if (args.Action == NotifyCollectionChangedAction.Add
            && args.NewItems is { } added
            && sender is IList list
            && args.NewStartingIndex == list.Count - added.Count)
        {
            var oldCount = this._options.Count;
            foreach (var value in added)
            {
                this._options.Add(this.CreateOption(value));
            }

            if ((this.SelectedIndex >= oldCount && this.SelectedIndex < this._options.Count)
                || (this.SelectedIndex == -1 && this.SelectedItem is { } pending && added.Contains(pending)))
            {
                this.ReconcileSelection();
            }

            if (this._menu.IsOpen && !this._refreshTimer.IsRunning)
            {
                this._refreshTimer.Start();
            }

            return;
        }

        this.Refresh();
    }

    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        this.ObserveSource();
        this.Refresh();
        if (this.IsDropDownOpen)
        {
            this._menu.ShowAt(this);
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        this.ObserveSource();
        this._refreshTimer.Stop();
        this.StopObservingValues();
        this._menu.Hide();
    }

    private void OnPreviewKeyDown(object sender, KeyRoutedEventArgs args)
    {
        var isArrowKey = args.Key is VirtualKey.Down or VirtualKey.Up;
        if (args.Key == VirtualKey.F4 || (isArrowKey && MenuFilterBox.IsKeyDown(VirtualKey.Menu)))
        {
            this.IsDropDownOpen = !this.IsDropDownOpen;
        }
        else if (isArrowKey && !this.IsDropDownOpen)
        {
            this.CycleSelection(args.Key == VirtualKey.Down ? 1 : -1);
        }
        else
        {
            return;
        }

        args.Handled = true;
    }

    private void CycleSelection(int direction)
    {
        if (this._refreshTimer.IsRunning)
        {
            this.Refresh();
        }

        var currentIndex = this.SelectedIndex;
        var start = direction > 0 ? 0 : this._options.Count - 1;
        if (currentIndex >= 0 && currentIndex < this._options.Count)
        {
            start = currentIndex + direction;
        }

        var nextIndex = MenuSearch.FindInvokableIndex(this._options, start, direction);
        if (nextIndex >= 0)
        {
            this.SelectedIndex = nextIndex;
        }
    }
}
