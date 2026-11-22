// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Windows.Input;
using JPSoftworks.DesktopToolkit.Controls;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace JPSoftworks.DesktopToolkit.Controls.SearchableFlyouts.DevApp;

public sealed partial class MainPage : Page
{
    public static readonly DependencyProperty NativeAdvancedCommandsProperty = DependencyProperty.Register(
        nameof(NativeAdvancedCommands), typeof(bool), typeof(MainPage), new PropertyMetadata(false));

    private readonly List<SearchableFlyoutItem> _commands = [];
    private readonly List<SearchableFlyoutItem> _recentCommands = [];
    private bool _initialized;

    public bool NativeAdvancedCommands
    {
        get => (bool)this.GetValue(NativeAdvancedCommandsProperty);
        set => this.SetValue(NativeAdvancedCommandsProperty, value);
    }

    public SearchableFlyoutStrings AccessibilityStrings { get; } = new()
    {
        CommandsName = "Available operations",
        MatchingCommandCountFormat = "Results available: {0:N0}",
        FilterSubmenuFormat = "Find in {0}",
        KeyboardHelp = "Type to find an operation. Use the arrow keys to navigate.",
    };

    public ICommand NativeSearchCommand { get; }

    public MainPage()
    {
        this.NativeSearchCommand = new DemoCommand(_ => this.LastActionText.Text = "Native searchable command executed.");
        this.InitializeComponent();
        this.ThemePicker.ItemsSource = new[] { "System", "Light", "Dark" };
        this.ThemePicker.SelectedItem = "System";
        this.WorkspacePicker.ItemsSource = Enumerable.Range(1, 10_000).Select(index => $"Workspace {index:00000}").ToArray();
        this.WorkspacePicker.SelectedItem = "Workspace 00042";
        this.WorkspacePicker.DropDownFlyout.Opened += this.OnMenuOpened;
        this.WorkspacePicker.DropDownFlyout.FilterChanged += this.OnMenuFilterChanged;
        for (var index = 1; index <= 250; index++)
        {
            var item = new MenuFlyoutItem { Text = $"Native command {index:000}" };
            item.Click += this.OnNativeItemClick;
            this.NativeOverflowMenu.Items.Add(item);
        }

        this.AddStandardCommands();
        var openRecentCommand = new DemoCommand(parameter =>
            this.LastActionText.Text = $"Opened recent document {parameter:00000} through ICommand.");
        for (var index = 1; index <= 10_000; index++)
        {
            this._recentCommands.Add(
                new SearchableFlyoutItem
                {
                    Text = $"Open recent document {index:00000}",
                    Keywords = "file workspace history",
                    Glyph = "\uE8A5",
                    CommandParameter = index,
                    Command = openRecentCommand,
                });
        }

        this._initialized = true;
        this.UpdateLargeMenu();
    }

    private void AddStandardCommands()
    {
        this._commands.Add(
            new SearchableFlyoutItem
            {
                Text = "New file", Glyph = "\uE8A5", KeyboardAcceleratorText = "Ctrl+N", Keywords = "create document"
            });
        this._commands.Add(
            new SearchableFlyoutItem
            {
                Text = "Open file", Glyph = "\uE8E5", KeyboardAcceleratorText = "Ctrl+O", Keywords = "browse document"
            });
        this._commands.Add(
            new SearchableFlyoutItem { Text = "Save", Glyph = "\uE74E", KeyboardAcceleratorText = "Ctrl+S", Keywords = "write document" });
        this._commands.Add(new SearchableFlyoutItem { Text = "Save as", Glyph = "\uE792", Keywords = "write copy document" });
        this._commands.Add(new SearchableFlyoutItem { Text = "Copy path", Glyph = "\uE8C8", Keywords = "clipboard location" });
        this._commands.Add(
            new SearchableFlyoutItem { Text = "Pin to favorites", Glyph = "\uE734", IsCheckable = true, Keywords = "bookmark star" });
        this._commands.Add(new SearchableFlyoutItem { IsSeparator = true });
        this._commands.Add(
            new SearchableFlyoutItem
            {
                Text = "Export selection",
                Glyph = "\uEDE1",
                Keywords = "share output",
                Command = new DemoCommand(_ => { }, canExecute: false),
            });
        var exportMenu = new SearchableFlyoutItem { Text = "Export as", Glyph = "\uEDE1", Keywords = "format output pdf png svg" };
        exportMenu.Items.Add(new SearchableFlyoutItem { Text = "PDF document", Glyph = "\uE8A5", Keywords = "print portable document" });
        exportMenu.Items.Add(new SearchableFlyoutItem { Text = "PNG image", Glyph = "\uEB9F", Keywords = "bitmap raster picture" });
        exportMenu.Items.Add(new SearchableFlyoutItem { Text = "SVG vector", Glyph = "\uE8D2", Keywords = "scalable graphic drawing" });
        this._commands.Add(exportMenu);
    }

    private void OnRenameDestination(object sender, RoutedEventArgs args)
    {
        this.LiveChoiceData.Text = this.LiveChoiceData.Text == "Primary display" ? "Work display" : "Primary display";
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
    private void OnThemeChanged(object? sender, SelectionChangedEventArgs args)
    {
        this.RequestedTheme = (args.AddedItems.FirstOrDefault() as string) switch
        {
            "Light" => ElementTheme.Light,
            "Dark" => ElementTheme.Dark,
            _ => ElementTheme.Default,
        };
        if (this.XamlRoot?.Content is FrameworkElement root)
        {
            root.RequestedTheme = this.RequestedTheme;
        }
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(ToggleSwitch))]
    private void OnMenuDirectionToggled(object sender, RoutedEventArgs args)
    {
        this.FlowDirection = ((ToggleSwitch)sender).IsOn ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    }

    private void OnSensitivityChanged(object? sender, SelectionChangedEventArgs args)
    {
        if (this.SelectionStatusText is not null)
        {
            this.SelectionStatusText.Text = $"Sensitivity: {args.AddedItems.FirstOrDefault()}";
        }
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(ComboBox))]
    private void OnNativeSensitivityChanged(object sender, SelectionChangedEventArgs args)
    {
        if (this.SelectionStatusText is not null && ((ComboBox)sender).SelectedItem is { } item)
        {
            this.SelectionStatusText.Text = $"Built-in sensitivity: {item}";
        }
    }

    private void OnWorkspaceChanged(object? sender, SelectionChangedEventArgs args)
    {
        this.SelectionStatusText.Text = $"Selected {args.AddedItems.FirstOrDefault()}";
    }

    private void OnLibraryChanged(object? sender, SelectionChangedEventArgs args)
    {
        if (this.SelectionStatusText is not null && args.AddedItems.FirstOrDefault() is SearchableFlyoutItem item)
        {
            this.SelectionStatusText.Text = $"Library: {item.Text}";
        }
    }

    private void OnLargeMenuToggled(object sender, RoutedEventArgs args)
    {
        if (this._initialized)
        {
            this.UpdateLargeMenu();
        }
    }

    private void UpdateLargeMenu()
    {
        var source = this.LargeMenuToggle.IsOn ? this._commands.Concat(this._recentCommands).ToArray() : this._commands.ToArray();
        this.ButtonMenu.ItemsSource = source;
        this.ContextMenu.ItemsSource = source;
        this.AboveMenu.ItemsSource = source;
    }

    private void OnShowAtClick(object sender, RoutedEventArgs args)
    {
        this.ContextMenu.ShowAt(this.ContextTarget);
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(MenuFlyoutItem))]
    private void OnNativeItemClick(object sender, RoutedEventArgs args)
    {
        if (sender is MenuFlyoutItem item)
        {
            this.LastActionText.Text = $"Built-in menu: {item.Text}.";
        }
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(ToggleMenuFlyoutItem))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(MenuFlyoutItem))]
    private void OnSearchableNativeItemClick(object sender, RoutedEventArgs args)
    {
        if (sender is ToggleMenuFlyoutItem toggle)
        {
            this.LastActionText.Text = $"Native searchable: {toggle.Text}: {(toggle.IsChecked ? "checked" : "unchecked")}.";
        }
        else if (sender is MenuFlyoutItem item)
        {
            this.LastActionText.Text = $"Native searchable: {item.Text}.";
        }
    }

    private void OnNativeSearchAcceleratorInvoked(
        Microsoft.UI.Xaml.Input.KeyboardAccelerator sender,
        Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        this.LastActionText.Text = "Native searchable accelerator handled.";
    }

    private void OnNativeSearchFilterChanged(SearchableMenuFlyout sender, object? args)
    {
        this.DiagnosticsText.Text = $"Native searchable: {this.NativeSearchMenu.FilteredItemCount} matching commands.";
    }

    private void OnNativeOverflowOpened(object? sender, object args)
    {
        var presenter = VisualTreeHelper.GetOpenPopupsForXamlRoot(this.XamlRoot).Select(popup => popup.Child).OfType<MenuFlyoutPresenter>()
            .FirstOrDefault(presenter => AutomationProperties.GetAutomationId(presenter) == "NativeOverflowItems");
        this.DiagnosticsText.Text = $"Native menu: {this.NativeOverflowMenu.Items.Count} items, "
                                    + $"{presenter?.ItemsPanelRoot?.Children.Count ?? 0} realized menu controls.";
    }

    private void OnItemInvoked(SearchableItemsFlyout sender, FlyoutItemInvokedEventArgs args)
    {
        if (args.InvokedItem.Command is null)
        {
            this.LastActionText.Text = args.InvokedItem.IsCheckable
                ? $"{args.InvokedItem.Text}: {(args.InvokedItem.IsChecked ? "checked" : "unchecked")}."
                : $"Invoked {args.InvokedItem.Text}.";
        }
    }

    private void OnMenuOpened(object? sender, object args)
    {
        this.UpdateDiagnostics((SearchableItemsFlyout)sender!);
    }

    private void OnMenuFilterChanged(SearchableItemsFlyout sender, object? args)
    {
        this.UpdateDiagnostics(sender);
    }

    private void UpdateDiagnostics(SearchableItemsFlyout menu)
    {
        this.DispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            () =>
            {
                this.DiagnosticsText.Text
                    = $"{menu.RealizedContainerCount:N0} realized item containers for {menu.FilteredItemCount:N0} matching commands.";
            });
    }

    private sealed partial class DemoCommand(Action<object?> execute, bool canExecute = true) : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter)
        {
            return canExecute;
        }

        public void Execute(object? parameter)
        {
            execute(parameter);
        }
    }
}
