// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Runtime.CompilerServices;
using JPSoftworks.DesktopToolkit.Controls.Primitives;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Windows.Foundation;

namespace JPSoftworks.DesktopToolkit.Controls;

/// <summary>Displays a searchable menu of native WinUI menu items.</summary>
/// <remarks>Populate or rebuild <see cref="MenuFlyout.Items"/> in the Opening event. Each submenu filters its own items. While open, the menu adds <see cref="MenuSearchItem"/> containers for the filter and empty message to its native item collections.</remarks>
[ContentProperty(Name = nameof(Items))]
public sealed partial class SearchableMenuFlyout : MenuFlyout
{
    /// <summary>Occurs after the root list applies its filter and updates the result count.</summary>
    public event TypedEventHandler<SearchableMenuFlyout, object?>? FilterChanged;

    private static readonly ConditionalWeakTable<DependencyObject, WeakReference<SearchableMenuFlyout>> Owners = new();
    private readonly HashSet<NativeMenuSearchSession> _sessions = [];

    private readonly List<(IList<MenuFlyoutItemBase> Items, MenuFlyoutItemBase? Parent, MenuSearchItem Header, MenuSearchItem Empty)>
        _chrome = [];

    private readonly Style _headerStyle;
    private readonly Style _emptyStyle;
    private readonly Dictionary<MenuFlyoutItemBase, MenuFlyoutItemBase?> _parents = [];
    private MenuFlyoutPresenter? _rootPresenter;
    private bool _registering;
    private bool _processingAccelerators;

    /// <summary>Identifies the <see cref="Strings"/> dependency property.</summary>
    public static DependencyProperty StringsProperty { get; } = DependencyProperty.Register(
        nameof(Strings), typeof(SearchableFlyoutStrings), typeof(SearchableMenuFlyout), new PropertyMetadata(null, OnOptionsChanged));

    /// <summary>Identifies the <see cref="FilterText"/> dependency property.</summary>
    public static DependencyProperty FilterTextProperty { get; } = DependencyProperty.Register(
        nameof(FilterText), typeof(string), typeof(SearchableMenuFlyout), new PropertyMetadata(string.Empty, OnFilterTextChanged));

    /// <summary>Identifies the <see cref="FilterBoxPlacement"/> dependency property.</summary>
    public static DependencyProperty FilterBoxPlacementProperty { get; } = DependencyProperty.Register(
        nameof(FilterBoxPlacement), typeof(FilterBoxPlacement), typeof(SearchableMenuFlyout),
        new PropertyMetadata(FilterBoxPlacement.Auto, OnOptionsChanged));

    /// <summary>Identifies the <see cref="FilterPlaceholderText"/> dependency property.</summary>
    public static DependencyProperty FilterPlaceholderTextProperty { get; } = DependencyProperty.Register(
        nameof(FilterPlaceholderText), typeof(string), typeof(SearchableMenuFlyout),
        new PropertyMetadata("Search commands...", OnOptionsChanged));

    /// <summary>Identifies the <see cref="FilterBoxAutomationName"/> dependency property.</summary>
    public static DependencyProperty FilterBoxAutomationNameProperty { get; } = DependencyProperty.Register(
        nameof(FilterBoxAutomationName), typeof(string), typeof(SearchableMenuFlyout),
        new PropertyMetadata("Filter commands", OnOptionsChanged));

    /// <summary>Identifies the <see cref="NoResultsText"/> dependency property.</summary>
    public static DependencyProperty NoResultsTextProperty { get; } = DependencyProperty.Register(
        nameof(NoResultsText), typeof(string), typeof(SearchableMenuFlyout),
        new PropertyMetadata("No matching commands", OnOptionsChanged));

    /// <summary>Identifies the <see cref="FlyoutWidth"/> dependency property.</summary>
    public static DependencyProperty FlyoutWidthProperty { get; } = DependencyProperty.Register(
        nameof(FlyoutWidth), typeof(double), typeof(SearchableMenuFlyout), new PropertyMetadata(280d, OnOptionsChanged));

    /// <summary>Identifies the <see cref="MaxListHeight"/> dependency property.</summary>
    public static DependencyProperty MaxListHeightProperty { get; } = DependencyProperty.Register(
        nameof(MaxListHeight), typeof(double), typeof(SearchableMenuFlyout), new PropertyMetadata(320d, OnOptionsChanged));

    /// <summary>Identifies the <see cref="UseAcrylicBackdrop"/> dependency property.</summary>
    public static DependencyProperty UseAcrylicBackdropProperty { get; } = DependencyProperty.Register(
        nameof(UseAcrylicBackdrop), typeof(bool), typeof(SearchableMenuFlyout), new PropertyMetadata(false, OnBackdropChanged));

    /// <summary>Identifies the <see cref="ResetFilterOnOpen"/> dependency property.</summary>
    public static DependencyProperty ResetFilterOnOpenProperty { get; } = DependencyProperty.Register(
        nameof(ResetFilterOnOpen), typeof(bool), typeof(SearchableMenuFlyout), new PropertyMetadata(true));

    /// <summary>Identifies the <see cref="IsResultCountVisible"/> dependency property.</summary>
    public static DependencyProperty IsResultCountVisibleProperty { get; } = DependencyProperty.Register(
        nameof(IsResultCountVisible), typeof(bool), typeof(SearchableMenuFlyout), new PropertyMetadata(false, OnOptionsChanged));

    /// <summary>Identifies the Keywords attached property.</summary>
    public static DependencyProperty KeywordsProperty { get; } = DependencyProperty.RegisterAttached(
        "Keywords", typeof(string), typeof(SearchableMenuFlyout), new PropertyMetadata(string.Empty, OnKeywordsChanged));

    /// <summary>Gets or sets the localized labels and format strings. The default is <see cref="SearchableFlyoutStrings.Default"/>.</summary>
    public SearchableFlyoutStrings Strings
    {
        get => (SearchableFlyoutStrings?)this.GetValue(StringsProperty) ?? SearchableFlyoutStrings.Default;
        set => this.SetValue(StringsProperty, value);
    }

    /// <summary>Gets or sets the filter text for the root level. Each word matches against item text or keywords, ignoring case.</summary>
    public string FilterText
    {
        get => (string)this.GetValue(FilterTextProperty);
        set => this.SetValue(FilterTextProperty, value ?? string.Empty);
    }

    /// <summary>Gets or sets the position of the filter field. The default is <see cref="JPSoftworks.DesktopToolkit.Controls.FilterBoxPlacement.Auto"/>.</summary>
    public FilterBoxPlacement FilterBoxPlacement
    {
        get => (FilterBoxPlacement)this.GetValue(FilterBoxPlacementProperty);
        set => this.SetValue(FilterBoxPlacementProperty, value);
    }

    /// <summary>Gets or sets the prompt shown in the filter field when its text is empty.</summary>
    public string FilterPlaceholderText
    {
        get => (string)this.GetValue(FilterPlaceholderTextProperty);
        set => this.SetValue(FilterPlaceholderTextProperty, value);
    }

    /// <summary>Gets or sets the accessible name of the root filter field.</summary>
    public string FilterBoxAutomationName
    {
        get => (string)this.GetValue(FilterBoxAutomationNameProperty);
        set => this.SetValue(FilterBoxAutomationNameProperty, value);
    }

    /// <summary>Gets or sets the message shown when filtering leaves no matching items.</summary>
    public string NoResultsText
    {
        get => (string)this.GetValue(NoResultsTextProperty);
        set => this.SetValue(NoResultsTextProperty, value);
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

    /// <summary>Gets or sets whether the search fields use an additional acrylic backdrop. The default is false.</summary>
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

    /// <summary>Gets or sets whether the result count is displayed below the list. The default is false.</summary>
    public bool IsResultCountVisible
    {
        get => (bool)this.GetValue(IsResultCountVisibleProperty);
        set => this.SetValue(IsResultCountVisibleProperty, value);
    }

    /// <summary>Gets the number of matching items at the root level, excluding separators. The count is zero when closed.</summary>
    public int FilteredItemCount => this._sessions.FirstOrDefault(session => session.IsRoot)?.FilteredItemCount ?? 0;

    internal float? UpwardMenuShadowElevation =>
        this._sessions.FirstOrDefault(session => session.IsRoot)?.UpwardMenuShadowElevation;

    /// <summary>Initializes a new instance of the <see cref="SearchableMenuFlyout"/> class.</summary>
    [WinRT.DynamicWindowsRuntimeCast(typeof(Style))]
    public SearchableMenuFlyout()
    {
        var resources = new ResourceDictionary
        {
            Source = new Uri("ms-appx:///JPSoftworks.DesktopToolkit.Controls.SearchableFlyouts/Controls/MenuStyles.xaml")
        };
        this.MenuFlyoutPresenterStyle = (Style)resources["SearchableMenuPresenterStyle"];
        this._headerStyle = (Style)resources["SearchFilterItemStyle"];
        this._emptyStyle = (Style)resources["SearchEmptyItemStyle"];
        this.AreOpenCloseAnimationsEnabled = false;
        this.Opening += this.OnOpening;
        this.Opened += (_, _) =>
        {
            this.RegisterItems();
            if (this._rootPresenter is not null)
            {
                NativeMenuSearch.EnsureAttached(this._rootPresenter);
            }

            foreach (var session in this._sessions.ToArray())
            {
                session.QueueInitialize();
            }
        };
        this.Closed += (_, _) =>
        {
            foreach (var session in this._sessions.ToArray())
            {
                session.Dispose();
            }

            foreach (var (items, parent, header, empty) in this._chrome)
            {
                items.Remove(header);
                items.Remove(empty);
            }

            this._chrome.Clear();
            this._parents.Clear();
        };
    }

    /// <summary>Gets the additional text used to match a native menu item.</summary>
    /// <param name="item">The element whose Keywords value is read.</param>
    /// <returns>The item's keywords, or an empty string if none are set.</returns>
    public static string GetKeywords(DependencyObject item)
    {
        return item.GetValue(KeywordsProperty) as string ?? string.Empty;
    }

    /// <summary>Sets the additional text used to match a native menu item.</summary>
    /// <param name="item">The element that owns the Keywords value.</param>
    /// <param name="value">The keywords to match alongside the item's label.</param>
    public static void SetKeywords(DependencyObject item, string value)
    {
        item.SetValue(KeywordsProperty, value ?? string.Empty);
    }

    /// <summary>Rereads native menu items and reapplies the filters at open menu levels.</summary>
    public void Refresh()
    {
        if (this._registering || !this.IsOpen)
        {
            return;
        }

        this.RegisterItems();
        foreach (var session in this._sessions.ToArray())
        {
            session.Refresh();
        }
    }

    /// <inheritdoc/>
    [WinRT.DynamicWindowsRuntimeCast(typeof(MenuFlyoutPresenter))]
    protected override Control CreatePresenter()
    {
        var presenter = base.CreatePresenter();
        this._rootPresenter = presenter as MenuFlyoutPresenter;
        SetOwner(presenter, this);
        return presenter;
    }

    internal static SearchableMenuFlyout? FindOwner(MenuFlyoutPresenter presenter)
    {
        if (Owners.TryGetValue(presenter, out var reference) && reference.TryGetTarget(out var owner))
        {
            return owner;
        }

        foreach (var item in presenter.Items.OfType<MenuFlyoutItemBase>())
        {
            if (Owners.TryGetValue(item, out reference) && reference.TryGetTarget(out owner))
            {
                return owner;
            }
        }

        return null;
    }

    internal MenuFlyoutItemBase? ParentOf(MenuFlyoutItemBase item)
    {
        return this._parents.GetValueOrDefault(item);
    }

    internal void AddSession(NativeMenuSearchSession session)
    {
        this._sessions.Add(session);
    }

    internal void RemoveSession(NativeMenuSearchSession session)
    {
        this._sessions.Remove(session);
    }

    internal void NotifyFilterChanged()
    {
        this.FilterChanged?.Invoke(this, null);
    }

    internal void ProcessSearchAccelerators(ProcessKeyboardAcceleratorEventArgs args)
    {
        if (this._processingAccelerators || args.Handled)
        {
            return;
        }

        this._processingAccelerators = true;
        try
        {
            foreach (var item in this._parents.Keys.ToArray())
            {
                TryInvokeAccelerator(item, args);
                if (args.Handled)
                {
                    break;
                }
            }
        }
        finally
        {
            this._processingAccelerators = false;
        }
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(MenuFlyoutItem))]
    private static void TryInvokeAccelerator(MenuFlyoutItemBase item, ProcessKeyboardAcceleratorEventArgs args)
    {
        KeyboardAcceleratorInvokedEventArgs? invocation = null;
        TypedEventHandler<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs> observe = (_, value) => invocation = value;
        var clicked = false;
        var command = item as MenuFlyoutItem;
        RoutedEventHandler observeClick = (_, _) => clicked = true;
        var accelerators = item.KeyboardAccelerators.ToArray();
        try
        {
            if (command is not null)
            {
                command.Click += observeClick;
            }

            foreach (var accelerator in accelerators)
            {
                accelerator.Invoked += observe;
            }

            item.TryInvokeKeyboardAccelerator(args);
            if (!args.Handled && invocation is not null)
            {
                // Native matching can succeed without performing the default action after filtering.
                args.Handled = invocation.Handled || clicked || NativeMenuSearchSession.InvokeNative(item);
            }
        }
        finally
        {
            foreach (var accelerator in accelerators)
            {
                accelerator.Invoked -= observe;
            }

            if (command is not null)
            {
                command.Click -= observeClick;
            }
        }
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(MenuFlyoutItem))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(MenuFlyoutSubItem))]
    internal static string ItemText(MenuFlyoutItemBase item)
    {
        return item switch
        {
            MenuFlyoutItem command => command.Text,
            MenuFlyoutSubItem submenu => submenu.Text,
            _ => Microsoft.UI.Xaml.Automation.AutomationProperties.GetName(item),
        };
    }

    private static void SetOwner(DependencyObject element, SearchableMenuFlyout owner)
    {
        Owners.Remove(element);
        Owners.Add(element, new WeakReference<SearchableMenuFlyout>(owner));
    }

    private void OnOpening(object? sender, object args)
    {
        if (this.ResetFilterOnOpen)
        {
            this.FilterText = string.Empty;
        }

        this.RegisterItems();
    }

    private void RegisterItems()
    {
        if (this._registering)
        {
            return;
        }

        this._registering = true;
        try
        {
            this._parents.Clear();
            this.RegisterLevel(this.Items, null);
        }
        finally
        {
            this._registering = false;
        }
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(MenuFlyoutSubItem))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(SplitMenuFlyoutItem))]
    private void RegisterLevel(IList<MenuFlyoutItemBase> items, MenuFlyoutItemBase? parent)
    {
        var level = this._chrome.Find(candidate => ReferenceEquals(candidate.Parent, parent));
        if (level.Header is null)
        {
            var header = new MenuSearchItem { IsFilter = true, MenuParent = parent, Style = this._headerStyle };
            var empty = new MenuSearchItem { MenuParent = parent, Style = this._emptyStyle, Visibility = Visibility.Collapsed };
            level = (items, parent, header, empty);
            this._chrome.Add(level);
            SetOwner(header, this);
            SetOwner(empty, this);
        }

        if (!items.Contains(level.Header))
        {
            items.Insert(0, level.Header);
        }

        if (!items.Contains(level.Empty))
        {
            items.Add(level.Empty);
        }

        foreach (var item in items.Where(item => item is not MenuSearchItem).ToArray())
        {
            this._parents[item] = parent;
            SetOwner(item, this);
            if (item is MenuFlyoutSubItem submenu)
            {
                this.RegisterLevel(submenu.Items, item);
            }
            else if (item is SplitMenuFlyoutItem split)
            {
                this.RegisterLevel(split.Items, item);
            }
        }
    }

    private static void OnOptionsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        foreach (var session in ((SearchableMenuFlyout)sender)._sessions.ToArray())
        {
            session.ApplyOptions();
        }
    }

    private static void OnBackdropChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        foreach (var session in ((SearchableMenuFlyout)sender)._sessions.ToArray())
        {
            session.ApplyBackdrop();
        }
    }

    private static void OnFilterTextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        foreach (var session in ((SearchableMenuFlyout)sender)._sessions)
        {
            session.SyncFilterText();
        }
    }

    private static void OnKeywordsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (Owners.TryGetValue(sender, out var reference) && reference.TryGetTarget(out var owner))
        {
            owner.Refresh();
        }
    }
}
