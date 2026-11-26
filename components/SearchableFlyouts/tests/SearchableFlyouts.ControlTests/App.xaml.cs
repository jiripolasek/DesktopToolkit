// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using JPSoftworks.DesktopToolkit.Controls;
using JPSoftworks.DesktopToolkit.Localization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.XamlTypeInfo;
using Microsoft.Windows.Globalization;

namespace JPSoftworks.DesktopToolkit.Controls.SearchableFlyouts.ControlTests;

public partial class App : Application
{
    private readonly StackPanel _host = new();
    private Window _window = null!;

    public App()
    {
        ApplicationLanguages.PrimaryLanguageOverride = "en-US";
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
            (nameof(ThrowingLoaderFallsThroughPerKey), ThrowingLoaderFallsThroughPerKey),
            (nameof(RepeatedLoaderFailuresAreThrottled), RepeatedLoaderFailuresAreThrottled),
            (nameof(this.ExistingControlsRefreshLocalizedDefaults), this.ExistingControlsRefreshLocalizedDefaults),
            (nameof(ResolvedStringsAreCachedUntilInvalidated), ResolvedStringsAreCachedUntilInvalidated),
            (nameof(ResolvedStringsFollowCurrentUICulture), ResolvedStringsFollowCurrentUICulture),
            (nameof(ReswStringsFollowAppLanguage), ReswStringsFollowAppLanguage),
            (nameof(FilterTextNormalizesNull), FilterTextNormalizesNull),
            (nameof(EnglishTextIsAvailableByDefault), EnglishTextIsAvailableByDefault),
            (nameof(this.EmptyFlyoutPlaceholdersCanBeRestored), this.EmptyFlyoutPlaceholdersCanBeRestored),
            (nameof(this.DropDownTextUpdatesOpenFlyout), this.DropDownTextUpdatesOpenFlyout),
            (nameof(this.EqualChoicesKeepTheirSourceIndex), this.EqualChoicesKeepTheirSourceIndex),
            (nameof(this.NullChoicesPreserveSourceIndices), this.NullChoicesPreserveSourceIndices),
            (nameof(this.RefreshPreservesHighlightedChoice), this.RefreshPreservesHighlightedChoice),
            (nameof(this.ChoiceTemplateUsesOriginalItem), this.ChoiceTemplateUsesOriginalItem),
            (nameof(this.ChildChangesKeepSubmenuOpen), this.ChildChangesKeepSubmenuOpen),
            (nameof(this.OpenSubmenusRefreshInheritedText), this.OpenSubmenusRefreshInheritedText),
            (nameof(this.AppendingChoicesScalesLinearly), this.AppendingChoicesScalesLinearly),
            (nameof(this.OpeningCanRebuildNativeMenu), this.OpeningCanRebuildNativeMenu),
            (nameof(this.UnnamedNativeSubmenuUsesParentFilterName), this.UnnamedNativeSubmenuUsesParentFilterName),
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

    private static Task ThrowingLoaderFallsThroughPerKey()
    {
        var previous = ToolkitLocalization.StringResourceLoader;
        var attempts = 0;
        ToolkitLocalization.StringResourceLoader = new TestStringLoader((catalog, key) => (catalog, key) switch
        {
            ("JPSoftworks.DesktopToolkit.SearchableFlyouts", "DropDownDefaultPlaceholderText") => throw new InvalidOperationException(),
            ("JPSoftworks.DesktopToolkit.SearchableFlyouts", "DropDownDefaultNoResultsText") => " ",
            ("JPSoftworks.DesktopToolkit.SearchableFlyouts", "MatchingCommandCountFormat") when ++attempts == 1 => throw new InvalidOperationException(),
            ("JPSoftworks.DesktopToolkit.SearchableFlyouts", "MatchingCommandCountFormat") => "Results: {0:N0}",
            _ => null,
        });
        try
        {
            var picker = new SearchableDropDown();
            Check(picker.PlaceholderText == "Select an item", "A throwing lookup prevented the dropdown from using its default prompt.");
            Check(picker.NoResultsText == "No matching choices", "The loader's blank result replaced the default.");
            Check(picker.Strings.MatchingCommandCountFormat == "Matching commands: {0:N0}", "A transient error skipped the default format.");
            Check(picker.Strings.MatchingCommandCountFormat == "Results: {0:N0}", "A transient loader error prevented a later lookup.");
        }
        finally
        {
            ToolkitLocalization.StringResourceLoader = previous;
        }

        return Task.CompletedTask;
    }

    private static async Task RepeatedLoaderFailuresAreThrottled()
    {
        var previous = ToolkitLocalization.StringResourceLoader;
        var attempts = 0;
        var available = false;
        ToolkitLocalization.StringResourceLoader = new TestStringLoader((_, key) =>
        {
            if (key != "CommandsName")
            {
                return null;
            }

            attempts++;
            return available ? "Recovered" : throw new InvalidOperationException();
        });
        try
        {
            var strings = SearchableFlyoutStrings.Default;
            for (var index = 0; index < 1000; index++)
            {
                Check(strings.CommandsName == "Commands", "A missing translation lost the fallback.");
            }

            Check(attempts == 2, $"A failing source was called {attempts} times.");
            available = true;
            await Until(() => strings.CommandsName == "Recovered");
            Check(attempts == 3, "The source did not recover on the first retry after the cooldown.");
        }
        finally
        {
            ToolkitLocalization.StringResourceLoader = previous;
        }
    }

    private async Task ExistingControlsRefreshLocalizedDefaults()
    {
        var previous = ToolkitLocalization.StringResourceLoader;
        var language = "First";
        ToolkitLocalization.StringResourceLoader = new TestStringLoader((_, key) => language + " " + key);
        try
        {
            var strings = new SearchableFlyoutStrings { ChoiceCountFormat = "Fixed {0}" };
            var picker = await this.AddControl(new SearchableDropDown { Strings = strings, NoResultsText = "Fixed empty text" });
            var items = new SearchableItemsFlyout();
            var native = new SearchableMenuFlyout();
            var placeholder = (TextBlock)picker.FindName("PlaceholderTextBlock");
            Check(placeholder.Text == "First DropDownDefaultPlaceholderText" && strings.CommandCountFormat == "First CommandCountFormat",
                "The controls did not resolve the first loader values.");

            language = "Second";
            await Task.Run(ToolkitLocalization.InvalidateStringResources);
            await Until(() => placeholder.Text == "Second DropDownDefaultPlaceholderText");
            Check(picker.DropDownFlyout.FilterPlaceholderText == "Second DropDownDefaultFilterPlaceholderText"
                && picker.DropDownFlyout.FilterBoxAutomationName == "Second DropDownDefaultFilterBoxAutomationName"
                && picker.DropDownFlyout.NoResultsText == "Fixed empty text", "The inner flyout lost its inherited or explicit text.");
            Check(strings.CommandCountFormat == "Second CommandCountFormat" && strings.ChoiceCountFormat == "Fixed {0}"
                && SearchableFlyoutStrings.Default.ChoiceCountFormat == "Second ChoiceCountFormat"
                && strings.FilterSubmenuFormat == "Second FilterSubmenuFormat", "A flyout format ignored invalidation or lost its override.");
            Check(items.FilterPlaceholderText == "Second DefaultFilterPlaceholderText" && native.NoResultsText == "Second DefaultNoResultsText",
                "The flyout dependency properties retained old defaults.");
        }
        finally
        {
            ToolkitLocalization.StringResourceLoader = previous;
        }
    }

    private static Task ResolvedStringsAreCachedUntilInvalidated()
    {
        var previous = ToolkitLocalization.StringResourceLoader;
        var calls = 0;
        var label = "Original commands";
        var loader = new TestStringLoader((catalog, key) =>
        {
            if (catalog != "JPSoftworks.DesktopToolkit.SearchableFlyouts" || key != "CommandsName")
            {
                return null;
            }

            calls++;
            return label;
        });
        ToolkitLocalization.StringResourceLoader = loader;
        try
        {
            var strings = new SearchableFlyoutStrings();
            for (var index = 0; index < 20; index++)
            {
                Check(strings.CommandsName == label, "The cached label differed from the resolved text.");
            }

            Check(calls == 1, "Repeated reads called the loader again.");

            label = "Updated commands";
            ToolkitLocalization.StringResourceLoader = loader;
            Check(strings.CommandsName == label && calls == 2, "Reassigning the same loader did not refresh the cached label.");

            label = "Refreshed commands";
            ToolkitLocalization.InvalidateStringResources();
            Check(strings.CommandsName == label && calls == 3, "Explicit invalidation did not refresh the cached label.");

            ToolkitLocalization.StringResourceLoader = null;
            Check(strings.CommandsName == "Commands", "Clearing the loader did not refresh the cached label.");
        }
        finally
        {
            ToolkitLocalization.StringResourceLoader = previous;
        }

        return Task.CompletedTask;
    }

    private static Task ResolvedStringsFollowCurrentUICulture()
    {
        var previousLoader = ToolkitLocalization.StringResourceLoader;
        var previousCulture = CultureInfo.CurrentUICulture;
        ToolkitLocalization.StringResourceLoader = new TestStringLoader((catalog, key) =>
            catalog == "JPSoftworks.DesktopToolkit.SearchableFlyouts" && key == "CommandsName" ? CultureInfo.CurrentUICulture.Name : null);
        try
        {
            var strings = new SearchableFlyoutStrings();
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            Check(strings.CommandsName == "en-US", "The initial UI culture did not select the expected string.");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("cs-CZ");
            Check(strings.CommandsName == "cs-CZ", "Switching the managed UI culture reused the previous string.");
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousCulture;
            ToolkitLocalization.StringResourceLoader = previousLoader;
        }

        return Task.CompletedTask;
    }

    private static Task ReswStringsFollowAppLanguage()
    {
        var previousLanguage = ApplicationLanguages.PrimaryLanguageOverride;
        var previousLoader = ToolkitLocalization.StringResourceLoader;
        try
        {
            ToolkitLocalization.StringResourceLoader = null;
            var strings = SearchableFlyoutStrings.Default;
            foreach (var (language, expected) in new[]
                     {
                         ("en-US", "Type to search the test menu."),
                         ("fr-FR", "Saisissez du texte pour rechercher dans le menu."),
                         ("en-US", "Type to search the test menu."),
                     })
            {
                ApplicationLanguages.PrimaryLanguageOverride = language;
                ToolkitLocalization.InvalidateStringResources();
                Check(strings.KeyboardHelp == expected, $"RESW lookup for {language} returned '{strings.KeyboardHelp}'.");
            }
        }
        finally
        {
            ApplicationLanguages.PrimaryLanguageOverride = previousLanguage;
            ToolkitLocalization.StringResourceLoader = previousLoader;
        }

        return Task.CompletedTask;
    }

    private static Task FilterTextNormalizesNull()
    {
        var itemsFlyout = new SearchableItemsFlyout { FilterText = "query" };
        itemsFlyout.FilterText = null;
        Check(itemsFlyout.FilterText == string.Empty, "The items flyout setter must clear a null filter.");
        itemsFlyout.SetValue(SearchableItemsFlyout.FilterTextProperty, null);
        Check(itemsFlyout.FilterText == string.Empty, "The items flyout getter must normalize a stored null filter.");

        var menuFlyout = new SearchableMenuFlyout { FilterText = "query" };
        menuFlyout.FilterText = null;
        Check(menuFlyout.FilterText == string.Empty, "The menu flyout setter must clear a null filter.");
        menuFlyout.SetValue(SearchableMenuFlyout.FilterTextProperty, null);
        Check(menuFlyout.FilterText == string.Empty, "The menu flyout getter must normalize a stored null filter.");

        return Task.CompletedTask;
    }

    private static Task EnglishTextIsAvailableByDefault()
    {
        var picker = new SearchableDropDown();
        Check(
            picker.PlaceholderText == "Select an item"
            && picker.FilterPlaceholderText == "Search..."
            && picker.DropDownFlyout.FilterBoxAutomationName == "Filter choices"
            && picker.DropDownFlyout.NoResultsText == "No matching choices", "The dropdown lost its default prompt or status text.");

        var items = new SearchableItemsFlyout();
        Check(
            items.FilterPlaceholderText == "Search commands..."
            && items.FilterBoxAutomationName == "Filter commands"
            && items.NoResultsText == "No matching commands", "The items flyout lost its default prompt or status text.");

        var menu = new SearchableMenuFlyout();
        Check(
            menu.FilterPlaceholderText == "Search commands..."
            && menu.FilterBoxAutomationName == "Filter commands"
            && menu.NoResultsText == "No matching commands", "The menu flyout lost its default prompt or status text.");

        Check(
            menu.Strings.CommandsName == "Commands"
            && picker.Strings.MatchingChoiceCountFormat == "Matching choices: {0:N0}"
            && items.Strings.FilterSubmenuFormat == "Filter {0}"
            && items.Strings.KeyboardHelp.Length > 0, "The shared flyout strings lost default labels or announcements.");

        picker.PlaceholderText = " ";
        items.FilterPlaceholderText = " ";
        menu.NoResultsText = " ";
        Check(
            picker.PlaceholderText == "Select an item"
            && items.FilterPlaceholderText == "Search commands..."
            && menu.NoResultsText == "No matching commands", "Blank control labels hid the default text.");

        return Task.CompletedTask;
    }

    private async Task EmptyFlyoutPlaceholdersCanBeRestored()
    {
        var dropdown = await this.AddControl(new SearchableDropDown { Items = { "Desktop" } });
        var hint = (TextBlock)dropdown.FindName("PlaceholderTextBlock");
        var selected = (TextBlock)dropdown.FindName("SelectedTextBlock");
        var items = new SearchableItemsFlyout { FilterPlaceholderText = string.Empty };
        var native = new SearchableMenuFlyout { FilterPlaceholderText = string.Empty };
        dropdown.PlaceholderText = string.Empty;
        dropdown.FilterPlaceholderText = string.Empty;
        ToolkitLocalization.InvalidateStringResources();
        Check(hint.Text == string.Empty && hint.Visibility == Visibility.Collapsed, "The closed dropdown displayed a suppressed hint.");
        Check(dropdown.DropDownFlyout.FilterPlaceholderText == string.Empty && items.FilterPlaceholderText == string.Empty
            && native.FilterPlaceholderText == string.Empty, "A flyout replaced an explicit empty filter hint.");
        dropdown.SelectedIndex = 0;
        Check(selected.Text == "Desktop" && selected.Visibility == Visibility.Visible, "A hidden hint also hid the selected item.");
        dropdown.SelectedIndex = -1;
        Check(hint.Visibility == Visibility.Collapsed, "Clearing the selection restored a suppressed hint.");

        dropdown.PlaceholderText = null;
        dropdown.ClearValue(SearchableDropDown.FilterPlaceholderTextProperty);
        items.FilterPlaceholderText = null;
        native.ClearValue(SearchableMenuFlyout.FilterPlaceholderTextProperty);
        Check(hint.Visibility == Visibility.Visible && hint.Text == "Select an item"
            && dropdown.DropDownFlyout.FilterPlaceholderText == "Search...",
            "Resetting the dropdown hints did not restore their defaults.");
        Check(items.FilterPlaceholderText == "Search commands..." && native.FilterPlaceholderText == "Search commands...",
            "Resetting a standalone flyout hint did not restore its default.");
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(TextBox))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(TextBlock))]
    private async Task DropDownTextUpdatesOpenFlyout()
    {
        var picker = await this.AddControl(new SearchableDropDown
        {
            FilterBoxAutomationName = "Find a destination",
            FilterPlaceholderText = "Search destinations...",
            NoResultsText = "No matching destinations",
            Strings = new SearchableFlyoutStrings
            {
                ChoiceCountFormat = string.Empty,
                MatchingChoiceCountFormat = "{1}",
                ChoicesName = " ",
                KeyboardHelp = " ",
            },
            Items = { "Desktop" },
        });
        var menu = picker.DropDownFlyout;
        menu.IsResultCountVisible = true;
        picker.IsDropDownOpen = true;
        try
        {
            var root = (FrameworkElement)menu.Content;
            var filter = (TextBox)root.FindName("FilterBox");
            var empty = (TextBlock)root.FindName("EmptyMessage");
            var resultCount = (TextBlock)root.FindName("ResultCount");
            await Until(() => menu.FilteredItemCount == 1);
            Check(
                AutomationProperties.GetName(filter) == "Find a destination"
                && filter.PlaceholderText == "Search destinations..."
                && AutomationProperties.GetName(GetItemsList(menu)) == "Choices", "The dropdown did not present its configured filter text.");
            Check(
                resultCount.Text == "Choices: 1 of 1" && AutomationProperties.GetItemStatus(filter) == "Matching choices: 1",
                "Blank or invalid formats removed the result count or accessible status.");
            menu.FilterText = "missing";
            await Until(() => empty.Visibility == Visibility.Visible);
            Check(empty.Text == "No matching destinations", "The dropdown did not present its configured no-results text.");

            picker.FilterBoxAutomationName = string.Empty;
            picker.NoResultsText = "No destinations";
            Check(
                AutomationProperties.GetName(filter) == "Filter choices" && empty.Text == "No destinations",
                "The open dropdown did not update its filter name or empty state.");

            var previous = ToolkitLocalization.StringResourceLoader;
            try
            {
                var language = "First";
                ToolkitLocalization.StringResourceLoader = new TestStringLoader((_, key) => key switch
                {
                    "DropDownDefaultFilterBoxAutomationName" => language + " filter",
                    "ChoiceCountFormat" => language + " {0}/{1}",
                    _ => null,
                });
                language = "Second";
                ToolkitLocalization.InvalidateStringResources();
                Check(AutomationProperties.GetName(filter) == "Second filter" && resultCount.Text == "Second 0/1"
                    && empty.Text == "No destinations", "The open dropdown retained an old label or lost its explicit empty text.");
            }
            finally
            {
                ToolkitLocalization.StringResourceLoader = previous;
            }

            picker.FilterPlaceholderText = string.Empty;
            Check(filter.PlaceholderText == string.Empty && AutomationProperties.GetName(filter) == "Filter choices",
                "Removing the open filter hint changed its accessible name or left stale text.");
            picker.FilterPlaceholderText = null;
            Check(filter.PlaceholderText == "Search...", "Resetting an open filter did not restore its default hint.");

            menu.FilterBoxAutomationName = "Legacy filter name";
            picker.SelectedIndex = 0;
            Check(AutomationProperties.GetName(filter) == "Legacy filter name", "Selection overwrote the flyout's configured name.");
        }
        finally
        {
            menu.Hide();
        }
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

    [WinRT.DynamicWindowsRuntimeCast(typeof(TextBox))]
    private async Task ChildChangesKeepSubmenuOpen()
    {
        var group = new SearchableFlyoutItem { AutomationName = "Group", Items = { new() { Text = "First" } } };
        var menu = new SearchableItemsFlyout
        {
            Items = { group },
            Strings = new SearchableFlyoutStrings { FilterSubmenuFormat = "Find in {0}" },
        };
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
            var submenu = VisualTreeHelper.GetOpenPopupsForXamlRoot(anchor.XamlRoot).Select(popup => popup.Child)
                .Single(child => FindSubmenuList(child) is not null);
            var filter = (TextBox)FindNamedElement(submenu, "FilterBox")!;
            Check(AutomationProperties.GetName(filter) == "Find in Group", "The submenu filter ignored the text-less item's accessible name.");
            menu.FilterPlaceholderText = string.Empty;
            Check(filter.PlaceholderText == string.Empty, "The open submenu retained its parent's old filter hint.");
            menu.ClearValue(SearchableItemsFlyout.FilterPlaceholderTextProperty);
            Check(filter.PlaceholderText == "Search commands...", "The open submenu did not restore its inherited filter hint.");
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

    [WinRT.DynamicWindowsRuntimeCast(typeof(TextBox))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(TextBlock))]
    private async Task OpenSubmenusRefreshInheritedText()
    {
        var inner = new SearchableFlyoutItem { Text = "Inner", Items = { new() { Text = "Save" } } };
        var menu = new SearchableItemsFlyout { Items = { new() { Items = { inner } } } };
        var anchor = await this.AddControl(new Button { Content = "Open", Flyout = menu });
        menu.ShowAt(anchor);
        try
        {
            await ExpandOnlyGroup(() => GetItemsList(menu));
            await ExpandOnlyGroup(() => SubmenuList(anchor));
            await Until(() => SubmenuRoot(2) is not null);
            var first = SubmenuRoot(1)!;
            var second = SubmenuRoot(2)!;
            var parentFilter = (TextBox)FindNamedElement(first, "FilterBox")!;
            var filter = (TextBox)FindNamedElement(second, "FilterBox")!;
            var empty = (TextBlock)FindNamedElement(second, "EmptyMessage")!;
            var count = (TextBlock)FindNamedElement(second, "ResultCount")!;
            var items = FindSubmenuList(second, 2)!;
            filter.Text = "missing";
            await Until(() => items.Items.Count == 0);

            menu.FilterBoxAutomationName = "Commands search";
            menu.Strings = new() { FilterSubmenuFormat = "Search {0}", CommandCountFormat = "Hits {0}/{1}", MatchingCommandCountFormat = "Matches {0}" };
            menu.NoResultsText = "No commands here";
            Check(AutomationProperties.GetName(parentFilter) == "Commands search" && AutomationProperties.GetName(filter) == "Search Inner",
                "Open submenu filters kept their previous accessible names.");
            Check(count.Text == "Hits 0/1" && AutomationProperties.GetHelpText(items) == "Matches 0" && empty.Text == "No commands here",
                "The open nested submenu kept its previous count, status or empty message.");
            Check(filter.Text == "missing" && ReferenceEquals(SubmenuRoot(2), second), "Updating inherited text reset or closed the filtered submenu.");
        }
        finally
        {
            menu.Hide();
        }

        DependencyObject? SubmenuRoot(int depth) => VisualTreeHelper.GetOpenPopupsForXamlRoot(anchor.XamlRoot)
            .Select(popup => popup.Child).SingleOrDefault(child => FindSubmenuList(child, depth) is not null);

        static async Task ExpandOnlyGroup(Func<ListView?> findList)
        {
            ListView? list = null;
            await Until(() => (list = findList())?.ContainerFromIndex(0) is not null);
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(list!);
            peer.GetChildren().Select(child => child.GetPattern(PatternInterface.ExpandCollapse)).OfType<IExpandCollapseProvider>().Single().Expand();
        }
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
    private static ListView? FindSubmenuList(DependencyObject root, int depth = 1)
    {
        if (root is ListView list && AutomationProperties.GetAutomationId(list) == $"FilteredMenuItemsDepth{depth}")
        {
            return list;
        }

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            if (FindSubmenuList(VisualTreeHelper.GetChild(root, i), depth) is { } found)
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

    private async Task UnnamedNativeSubmenuUsesParentFilterName()
    {
        var group = new MenuFlyoutSubItem { Text = string.Empty, Items = { new MenuFlyoutItem { Text = "Child" } } };
        var menu = new SearchableMenuFlyout { FilterBoxAutomationName = "Find an operation", Items = { group } };
        var anchor = await this.AddControl(new Button { Content = "Native menu", Flyout = menu });
        menu.ShowAt(anchor);
        try
        {
            await Until(() => menu.FilteredItemCount == 1);
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(group);
            ((IExpandCollapseProvider)peer.GetPattern(PatternInterface.ExpandCollapse)).Expand();
            FrameworkElement? filter = null;
            await Until(() =>
            {
                filter = group.Items.OfType<MenuSearchItem>().Select(item => FindNamedElement(item, "SearchFilterBox")).FirstOrDefault();
                return filter is { IsLoaded: true } && AutomationProperties.GetAutomationId(filter) == "NativeSearchFilterBoxDepth1";
            });
            var name = AutomationProperties.GetName(filter);
            Check(
                name == "Find an operation",
                $"Expected the parent filter name for the unnamed native submenu, got '{name}'.");
        }
        finally
        {
            menu.Hide();
        }
    }

    private async Task ItemsFlyoutUsesAcrylicByDefault()
    {
        var menu = new SearchableItemsFlyout { Items = { new SearchableFlyoutItem { Text = "Command" } } };
        var anchor = await this.AddControl(new Button { Content = "Open", Flyout = menu });
        menu.ShowAt(anchor);
        try
        {
            var (themed, backdrop) = await WaitForBackdropBordersAsync(menu);
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

    private async Task DropDownBackdropIsOptIn()
    {
        var picker = await this.AddControl(new SearchableDropDown { Items = { "Choice" } });
        var menu = picker.DropDownFlyout;
        menu.ShowAt(picker);
        try
        {
            var (themed, backdrop) = await WaitForBackdropBordersAsync(menu);
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

    [WinRT.DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(Border))]
    private static async Task<(Border Themed, Border Backdrop)> WaitForBackdropBordersAsync(SearchableItemsFlyout menu)
    {
        Border? backdrop = null;
        Border? themed = null;
        await Until(() =>
        {
            if (menu.Content is not FrameworkElement root)
            {
                return false;
            }

            backdrop = root.FindName("MenuBackdrop") as Border;
            themed = root.FindName("MenuThemedBackground") as Border;
            return backdrop is { IsLoaded: true } && themed is { IsLoaded: true };
        });
        return (themed!, backdrop!);
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
        var backdropType = new XamlControlsXamlMetaDataProvider().GetXamlType("Microsoft.UI.Xaml.Controls.SystemBackdropElement");
        Check(
            backdropType is null
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

    private sealed class TestStringLoader(Func<string, string, string?> getString) : IStringResourceLoader
    {
        public string? GetString(string catalogName, string key) => getString(catalogName, key);
    }
}
