// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Diagnostics;
using JPSoftworks.DesktopToolkit.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.ApplicationModel.WindowsAppRuntime;

namespace JPSoftworks.DesktopToolkit.Controls.SearchableFlyouts.ControlTests;

public partial class App : Application
{
    private readonly StackPanel _host = new();
    private Window _window = null!;

    public App()
    {
        this.UnhandledException += (_, args) => Console.Error.WriteLine(args.Exception);
        this.InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        this._window = new Window { Content = this._host, Title = "Searchable flyout control tests" };
        this._host.Loaded += this.RunTests;
        this._window.AppWindow.IsShownInSwitchers = false;
        this._window.AppWindow.Show(false);
    }

    private async void RunTests(object sender, RoutedEventArgs args)
    {
        this._host.Loaded -= this.RunTests;
        (string Name, Func<Task> Run)[] cases =
        [
            (nameof(this.EqualChoicesKeepTheirSourceIndex), this.EqualChoicesKeepTheirSourceIndex),
            (nameof(this.NullChoicesPreserveSourceIndices), this.NullChoicesPreserveSourceIndices),
            (nameof(this.RefreshPreservesHighlightedChoice), this.RefreshPreservesHighlightedChoice),
            (nameof(this.ChoiceTemplateUsesOriginalItem), this.ChoiceTemplateUsesOriginalItem),
            (nameof(this.ChildChangesKeepSubmenuOpen), this.ChildChangesKeepSubmenuOpen),
            (nameof(this.AppendingChoicesScalesLinearly), this.AppendingChoicesScalesLinearly),
            (nameof(this.OpeningCanRebuildNativeMenu), this.OpeningCanRebuildNativeMenu),
            (nameof(this.ItemsFlyoutUsesAcrylicByDefault), this.ItemsFlyoutUsesAcrylicByDefault),
            (nameof(this.DropDownBackdropIsOptIn), this.DropDownBackdropIsOptIn),
            (nameof(this.NativeFilterBackdropIsOptIn), this.NativeFilterBackdropIsOptIn),
        ];
        var failed = 0;
        foreach (var (name, test) in cases)
        {
            try
            {
                await test();
                Console.WriteLine($"PASS {name}");
            }
            catch (Exception error)
            {
                failed++;
                Console.Error.WriteLine($"FAIL {name}: {error}");
            }
            finally
            {
                this._host.Children.Clear();
            }
        }

        Console.WriteLine($"Passed: {cases.Length - failed}, Failed: {failed}, Skipped: 0");
        Environment.ExitCode = failed == 0 ? 0 : 1;
        this._window.Close();
    }

    private async Task EqualChoicesKeepTheirSourceIndex()
    {
        var picker = await this.AddControl(new SearchableDropDown());
        var first = new Choice(1);
        var second = new Choice(1);
        var source = new ObservableCollection<Choice> { first, second };
        picker.ItemsSource = source;
        var changes = new List<SelectionChangedEventArgs>();
        picker.SelectionChanged += (_, args) => changes.Add(args);
        picker.SelectedIndex = 1;
        Check(picker.SelectedIndex == 1 && ReferenceEquals(picker.SelectedItem, second), "Lost the second equal entry.");
        source.Move(1, 0);
        Check(picker.SelectedIndex == 0 && ReferenceEquals(picker.SelectedItem, second), "Moving the entry changed selection.");
        var replacement = new Choice(1);
        picker.ItemsSource = new[] { replacement };
        Check(picker.SelectedIndex == 0 && ReferenceEquals(picker.SelectedItem, replacement), "Retained an item from the old source.");
        Check(
            changes.Count == 2
            && ReferenceEquals(changes[1].RemovedItems.Single(), second)
            && ReferenceEquals(changes[1].AddedItems.Single(), replacement), "Unexpected original objects in SelectionChanged.");
        picker.SelectedIndex = -1;
        Check(picker.SelectedItem is null && changes.Count == 3, "Clearing the selection failed.");
    }

    private async Task NullChoicesPreserveSourceIndices()
    {
        var picker = await this.AddControl(new SearchableDropDown());
        var source = new ObservableCollection<string?> { "first", null, "third" };
        picker.ItemsSource = source;
        picker.SelectedItem = "third";
        Check(picker.SelectedIndex == 2, "SelectedItem lost its source index after a null.");
        picker.SelectedIndex = 1;
        Check(picker.SelectedIndex == -1 && picker.SelectedItem is null, "A null source entry remained selected.");
        source.Add(null);
        source.Add("fifth");
        picker.SelectedIndex = 4;
        Check(Equals(picker.SelectedItem, "fifth"), "Appended nulls shifted selection.");
        source.Insert(0, null);
        Check(picker.SelectedIndex == 5 && Equals(picker.SelectedItem, "fifth"), "An inserted null lost the selected item.");

        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        picker.DropDownFlyout.Closed += (_, _) => closed.TrySetResult();
        picker.IsDropDownOpen = true;
        try
        {
            await Until(() => GetItemsList(picker.DropDownFlyout) is { IsLoaded: true, Items.Count: 3 });
            var rows = GetItemsList(picker.DropDownFlyout);
            Check(rows.SelectedIndex == 2, "The open flyout highlighted the wrong visible choice.");
        }
        finally
        {
            picker.DropDownFlyout.Hide();
            await closed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    private async Task RefreshPreservesHighlightedChoice()
    {
        var picker = await this.AddControl(new SearchableDropDown());
        var first = new SearchableFlyoutItem { Text = "A" };
        var pending = new SearchableFlyoutItem { Text = "B" };
        var other = new SearchableFlyoutItem { Text = "C" };
        var source = new ObservableCollection<SearchableFlyoutItem> { first, pending, other };
        picker.ItemsSource = source;
        picker.SelectedItem = first;
        picker.IsDropDownOpen = true;
        try
        {
            var rows = GetItemsList(picker.DropDownFlyout);
            await Until(() => rows.Items.Count == 3 && rows.IsLoaded);
            rows.SelectedIndex = 1;
            var refreshed = false;
            picker.DropDownFlyout.FilterChanged += (_, _) => refreshed = true;
            other.Text = "Renamed";
            await Until(() => refreshed);
            Check(rows.SelectedIndex == 1 && ReferenceEquals(picker.SelectedItem, first), "An unrelated edit reset the pending choice.");
            source.Insert(0, new SearchableFlyoutItem { Text = "Inserted" });
            await Until(() => rows.Items.Count == 4);
            Check(rows.SelectedIndex == 2 && ReferenceEquals(picker.SelectedItem, first), "An insertion lost the pending source entry.");
        }
        finally
        {
            picker.DropDownFlyout.Hide();
        }
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(DataTemplate))]
    private async Task ChoiceTemplateUsesOriginalItem()
    {
        var first = new XamlUICommand { Label = "Alpha", Description = "First detail" };
        var second = new XamlUICommand { Label = "Beta", Description = "Second detail" };
        var picker = await this.AddControl(
            new SearchableDropDown
            {
                ItemTextSelector = value => value is XamlUICommand choice ? choice.Label : string.Empty,
                ItemsSource = new object[] { first, new SearchableFlyoutItem { IsSeparator = true }, second },
                ItemTemplate = (DataTemplate)this.Resources["ChoiceDetail"],
                SelectedItem = first,
            });
        picker.IsDropDownOpen = true;
        try
        {
            var rows = GetItemsList(picker.DropDownFlyout);
            await Until(() => FindRowText(rows.ContainerFromIndex(2), second.Description) is not null);
            Check(
                ReferenceEquals(FindRowText(rows.ContainerFromIndex(2), second.Description)!.DataContext, second),
                "The row template did not receive the original item.");
            picker.DropDownFlyout.FilterText = second.Label;
            await Until(() => rows.Items.Count == 1 && FindRowText(rows.ContainerFromIndex(0), second.Description) is not null);
            Check(GetRowPeer().GetName() == second.Label, "The accessible name must come from the text selector.");

            picker.ItemTemplate = (DataTemplate)this.Resources["ChoiceLabel"];
            await Until(() => FindRowText(rows.ContainerFromIndex(0), second.Label) is not null);
            picker.ClearValue(SearchableDropDown.ItemTemplateProperty);
            await Until(() => FindRowText(rows.ContainerFromIndex(0), second.Label) is { } label
                              && !ReferenceEquals(label.DataContext, second));
            picker.ItemTemplate = (DataTemplate)this.Resources["ChoiceDetail"];
            await Until(() => FindRowText(rows.ContainerFromIndex(0), second.Description) is not null);
            Check(
                ReferenceEquals(picker.SelectedItem, first) && picker.DropDownFlyout.FilterText == second.Label,
                "Changing the template committed the pending choice or reset the query.");
            ((IInvokeProvider)GetRowPeer().GetPattern(PatternInterface.Invoke)).Invoke();
            await Until(() => !picker.IsDropDownOpen);
            Check(
                ReferenceEquals(picker.SelectedItem, second) && picker.SelectedIndex == 2,
                "Invoking a templated choice did not commit the original source item.");

            AutomationPeer GetRowPeer()
            {
                return FrameworkElementAutomationPeer.CreatePeerForElement(rows).GetChildren()
                    .Single(peer => peer.GetAutomationControlType() == AutomationControlType.ListItem);
            }
        }
        finally
        {
            picker.DropDownFlyout.Hide();
        }
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(TextBlock))]
    private static TextBlock? FindRowText(DependencyObject? root, string value)
    {
        if (root is null)
        {
            return null;
        }

        if (root is TextBlock text && text.Text == value)
        {
            return text;
        }

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            if (FindRowText(VisualTreeHelper.GetChild(root, i), value) is { } match)
            {
                return match;
            }
        }

        return null;
    }

    private async Task ChildChangesKeepSubmenuOpen()
    {
        var group = new SearchableFlyoutItem { Text = "Group", Items = { new() { Text = "First" } } };
        var menu = new SearchableItemsFlyout { Items = { group } };
        var anchor = await this.AddControl(new Button { Content = "Open", Flyout = menu });
        menu.ShowAt(anchor);
        try
        {
            var list = GetItemsList(menu);
            await Until(() => list.ContainerFromIndex(0) is not null);
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(list);
            var groupPeer = peer.GetChildren().Single(child => child.GetName() == "Group");
            var expand = (IExpandCollapseProvider)groupPeer.GetPattern(PatternInterface.ExpandCollapse);
            expand.Expand();
            await Until(() => SubmenuList(anchor)?.Items.Count == 1);
            group.Items.Add(new SearchableFlyoutItem { Text = "Second" });
            await Until(() => SubmenuList(anchor)?.Items.Count == 2);
            await Task.Delay(200);
            Check(SubmenuList(anchor)?.Items.Count == 2, "Adding a child dismissed the submenu.");
            group.Items.Clear();
            await Until(() => SubmenuList(anchor) is null);
        }
        finally
        {
            menu.Hide();
        }
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(ListView))]
    private static ListView GetItemsList(SearchableItemsFlyout menu)
    {
        return (ListView)((FrameworkElement)menu.Content).FindName("ItemsList");
    }

    private static ListView? SubmenuList(FrameworkElement anchor)
    {
        foreach (var popup in VisualTreeHelper.GetOpenPopupsForXamlRoot(anchor.XamlRoot))
        {
            if (FindSubmenuList(popup.Child) is { } list)
            {
                return list;
            }
        }

        return null;
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(ListView))]
    private static ListView? FindSubmenuList(DependencyObject root)
    {
        if (root is ListView list && AutomationProperties.GetAutomationId(list) == "FilteredMenuItemsDepth1")
        {
            return list;
        }

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            if (FindSubmenuList(VisualTreeHelper.GetChild(root, i)) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    private async Task AppendingChoicesScalesLinearly()
    {
        var picker = await this.AddControl(new SearchableDropDown());
        var calls = 0;
        picker.ItemTextSelector = value =>
        {
            calls++;
            return value.ToString()!;
        };
        const int size = 300;
        for (var i = 0; i < size; i++)
        {
            picker.Items.Add(i);
        }

        picker.SelectedIndex = size - 1;
        Check(Equals(picker.SelectedItem, size - 1), "The appended choices were unavailable for immediate selection.");
        Console.WriteLine($"{size} additions and a selection made {calls} text-selector calls.");
        Check(calls <= size * 2, $"{size} additions made {calls} selector calls.");
    }

    private async Task OpeningCanRebuildNativeMenu()
    {
        var menu = new SearchableMenuFlyout { Items = { new MenuFlyoutItem { Text = "Old" } } };
        var anchor = await this.AddControl(new Button { Content = "Native", Flyout = menu });
        menu.Opening += (_, _) =>
        {
            menu.Items.Clear();
            menu.Items.Add(new MenuFlyoutItem { Text = "Current command" });
            menu.Items.Add(new MenuFlyoutSubItem { Text = "Group", Items = { new MenuFlyoutItem { Text = "Child" } } });
        };
        for (var pass = 0; pass < 2; pass++)
        {
            menu.FilterBoxPlacement = pass == 0 ? FilterBoxPlacement.Bottom : FilterBoxPlacement.Auto;
            menu.ShowAt(anchor);
            try
            {
                await Until(() => menu.FilteredItemCount == 2);
                menu.FilterText = "current";
                await Until(() => menu.FilteredItemCount == 1);
            }
            finally
            {
                menu.Hide();
            }

            await Until(() => menu.Items.Count == 2 && menu.Items.OfType<MenuFlyoutSubItem>().Single().Items.Count == 1);
        }
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(Border))]
    private async Task ItemsFlyoutUsesAcrylicByDefault()
    {
        var menu = new SearchableItemsFlyout { Items = { new SearchableFlyoutItem { Text = "Command" } } };
        var anchor = await this.AddControl(new Button { Content = "Open", Flyout = menu });
        menu.ShowAt(anchor);
        try
        {
            var root = (FrameworkElement)menu.Content;
            var backdrop = (Border)root.FindName("MenuBackdrop");
            await Until(() => backdrop.IsLoaded);
            var themed = (Border)root.FindName("MenuThemedBackground");
            Check(menu.UseAcrylicBackdrop, "A standalone command menu must default to acrylic.");
            CheckAcrylicBackground(themed, backdrop);
            menu.UseAcrylicBackdrop = false;
            CheckThemedBackground(themed, backdrop);
        }
        finally
        {
            menu.Hide();
        }
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(Border))]
    private async Task DropDownBackdropIsOptIn()
    {
        var picker = await this.AddControl(new SearchableDropDown { Items = { "Choice" } });
        var menu = picker.DropDownFlyout;
        menu.ShowAt(picker);
        try
        {
            var root = (FrameworkElement)menu.Content;
            var backdrop = (Border)root.FindName("MenuBackdrop");
            await Until(() => backdrop.IsLoaded);
            var themed = (Border)root.FindName("MenuThemedBackground");
            CheckBackdropOptIn(themed, backdrop, enabled => picker.UseAcrylicBackdrop = enabled);
        }
        finally
        {
            menu.Hide();
        }
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(Border))]
    private async Task NativeFilterBackdropIsOptIn()
    {
        var menu = new SearchableMenuFlyout { Items = { new MenuFlyoutItem { Text = "Command" } } };
        var anchor = await this.AddControl(new Button { Content = "Open", Flyout = menu });
        menu.ShowAt(anchor);
        try
        {
            await Until(() => menu.FilteredItemCount == 1);
            var header = menu.Items.OfType<MenuSearchItem>().First();
            var backdrop = (Border)FindNamedElement(header, "SearchFilterBackdrop")!;
            var themed = (Border)FindNamedElement(header, "SearchFilterThemedBackground")!;
            CheckBackdropOptIn(themed, backdrop, enabled => menu.UseAcrylicBackdrop = enabled);
        }
        finally
        {
            menu.Hide();
        }
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
    private static FrameworkElement? FindNamedElement(DependencyObject root, string name)
    {
        if (root is FrameworkElement element && element.Name == name)
        {
            return element;
        }

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            if (FindNamedElement(VisualTreeHelper.GetChild(root, i), name) is { } match)
            {
                return match;
            }
        }

        return null;
    }

    private static void CheckBackdropOptIn(Border themed, Border backdrop, Action<bool> setEnabled)
    {
        CheckThemedBackground(themed, backdrop);
        setEnabled(true);
        CheckAcrylicBackground(themed, backdrop);
        setEnabled(false);
        CheckThemedBackground(themed, backdrop);
    }

    private static void CheckAcrylicBackground(Border themed, Border backdrop)
    {
        Check(
            themed.Visibility == Visibility.Collapsed && backdrop.Visibility == Visibility.Visible,
            "Opting in did not show the acrylic layer.");
        Check(
            ReleaseInfo.Major < 2
                ? backdrop.Background is not null && backdrop.Child is null
                : backdrop.Child is not null && backdrop.Background is null, "The runtime's acrylic backdrop is missing.");
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(SolidColorBrush))]
    private static void CheckThemedBackground(Border themed, Border backdrop)
    {
        Check(
            themed.Visibility == Visibility.Visible && themed.Background is SolidColorBrush { Color.A: 255 },
            "The themed fill must be visible and opaque.");
        Check(backdrop.Visibility == Visibility.Collapsed && backdrop.Child is null, "The acrylic layer must be inactive in solid mode.");
    }

    private async Task<T> AddControl<T>(T control)
        where T : FrameworkElement
    {
        this._host.Children.Add(control);
        await Until(() => control.IsLoaded);
        return control;
    }

    private static async Task Until(Func<bool> condition)
    {
        var elapsed = Stopwatch.StartNew();
        while (!condition())
        {
            if (elapsed.Elapsed > TimeSpan.FromSeconds(5))
            {
                throw new TimeoutException("Expected UI state was not reached.");
            }

            await Task.Delay(20);
        }
    }

    private static void Check(bool passed, string message)
    {
        if (!passed)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed record Choice(int Value);
}
