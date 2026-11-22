// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Globalization;
using JPSoftworks.DesktopToolkit.Localization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace JPSoftworks.DesktopToolkit.Controls.DateTimeControlTests;

public partial class App : Application
{
    private Window _window = null!;

    public App() => this.InitializeComponent();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var content = (FrameworkElement)((DataTemplate)this.Resources["PickerTestContent"]).LoadContent();
        var picker = (CalendarDateRangePicker)content.FindName("Picker");
        picker.Strings = new() { CancelButtonText = "Keep cancel" };
        this._window = new Window { Content = content };
        picker.Loaded += this.VerifyLocalizedText;
        this._window.AppWindow.IsShownInSwitchers = false;
        this._window.AppWindow.Show(false);
    }

    private async void VerifyLocalizedText(object sender, RoutedEventArgs args)
    {
        var picker = (CalendarDateRangePicker)sender;
        picker.Loaded -= this.VerifyLocalizedText;
        var previous = ToolkitLocalization.StringResourceLoader;
        try
        {
            var loader = new TestLoader { Language = "First" };
            ToolkitLocalization.StringResourceLoader = loader;
            picker.ApplyTemplate();
            var root = (FrameworkElement)VisualTreeHelper.GetChild(picker, 0);
            var button = (DropDownButton)root.FindName("PART_Button");
            var startLabel = (TextBlock)root.FindName("StartCalendarLabel");
            var presets = (ComboBox)root.FindName("PART_Presets");
            var apply = (Button)root.FindName("PART_Apply");
            var cancel = (Button)root.FindName("PART_Cancel");
            DateFormatUpdatesSelectionAndDraft(picker, button, presets, (TextBlock)root.FindName("PART_RangeSummary"));
            Console.WriteLine("PASS DateFormatUpdatesSelectionAndDraft");
            EmptyPlaceholdersCanBeRestored(picker, button, presets);
            Console.WriteLine("PASS EmptyPlaceholdersCanBeRestored");
            Check(Equals(button.Content, "First PlaceholderText"), "The rendered prompt missed the initial loader.");

            picker.SetValue(CalendarDateRangePicker.ApplyButtonTextProperty, picker.ApplyButtonText);
            loader.Language = "Second";
            ToolkitLocalization.InvalidateStringResources();
            Check(Equals(button.Content, "Second PlaceholderText") && AutomationProperties.GetName(button) == "Second PickerName",
                "The existing button did not refresh its visible or accessible name.");
            Check(presets.PlaceholderText == "Second PresetPlaceholderText" && startLabel.Text == "Second StartDateLabel",
                "The flyout template retained its old prompt or calendar heading.");
            Check(Equals(cancel.Content, "Keep cancel") && Equals(apply.Content, "First ApplyButtonText"),
                "Invalidation overwrote explicit strings or a local value equal to the old default.");

            picker.ClearValue(CalendarDateRangePicker.ApplyButtonTextProperty);
            Check(Equals(apply.Content, "Second ApplyButtonText"), "Clearing the local value did not restore the current default.");
            var start = new DateTimeOffset(2026, 1, 4, 0, 0, 0, TimeSpan.Zero);
            picker.Selection = new DateRangeSelection(new DateRange(start, start.AddDays(1)));
            Check(Equals(button.Content, $"Second {start:dd}-{start.AddDays(1):dd}"), "The default date range format was stale.");
            Console.WriteLine("PASS ExistingPickerRefreshesLocalizedText");
            await this.PlaceholderForegroundFollowsSelection(picker, button);
            Console.WriteLine("PASS PlaceholderForegroundFollowsSelection");
            await this.HeadingFreeTemplateKeepsAccessibleNames(picker, loader);
            Console.WriteLine("PASS HeadingFreeTemplateKeepsAccessibleNames");
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            Environment.ExitCode = 1;
        }
        finally
        {
            ToolkitLocalization.StringResourceLoader = previous;
            this._window.Close();
        }
    }

    private async Task HeadingFreeTemplateKeepsAccessibleNames(CalendarDateRangePicker picker, TestLoader loader)
    {
        picker.IsEnabled = true;
        picker.Presets = [new DateRangePreset("Selected range", picker.Selection!.Range)];
        picker.Template = (ControlTemplate)this.Resources["HeadingFreeTemplate"];
        picker.ApplyTemplate();
        picker.UpdateLayout();
        await Task.Yield();
        var root = (FrameworkElement)VisualTreeHelper.GetChild(picker, 0);
        var start = (CalendarView)root.FindName("PART_StartCalendar");
        var end = (CalendarView)root.FindName("PART_EndCalendar");
        var presets = (ComboBox)root.FindName("PART_Presets");
        var startDate = (CalendarDatePicker)root.FindName("PART_StartDate");
        var endDate = (CalendarDatePicker)root.FindName("PART_EndDate");
        var startPeer = FrameworkElementAutomationPeer.CreatePeerForElement(start) ?? throw new InvalidOperationException("The start calendar has no automation peer.");
        var endPeer = FrameworkElementAutomationPeer.CreatePeerForElement(end) ?? throw new InvalidOperationException("The end calendar has no automation peer.");
        var presetPeer = FrameworkElementAutomationPeer.CreatePeerForElement(presets) ?? throw new InvalidOperationException("The presets have no automation peer.");
        CheckNames("Second");
        presets.Header = "Reporting period";
        Check(presetPeer.GetName() == "Reporting period", "The toolkit name hid the custom preset header.");
        loader.Language = "Third";
        ToolkitLocalization.InvalidateStringResources();
        Check(presetPeer.GetName() == "Reporting period", "A resource refresh hid the custom preset header.");
        presets.Header = null;
        CheckNames("Third");

        AutomationProperties.SetName(end, "Third EndCalendarName");
        var source = new ContentControl { Content = "Named presets" };
        presets.SetBinding(AutomationProperties.NameProperty, new Binding { Source = source, Path = new PropertyPath("Content") });
        presets.Header = "Custom header";
        loader.Language = "Fourth";
        ToolkitLocalization.InvalidateStringResources();
        source.Content = "Updated presets";
        Check(startPeer.GetName() == "Fourth StartCalendarName" && endPeer.GetName() == "Third EndCalendarName"
              && presetPeer.GetName() == "Updated presets", "A refresh replaced an explicit or bound automation name.");

        void CheckNames(string language)
        {
            Check(startPeer.GetName() == language + " StartCalendarName" && endPeer.GetName() == language + " EndCalendarName"
                  && presetPeer.GetName() == language + " PresetLabel" && AutomationProperties.GetName(startDate) == language + " StartDateLabel"
                  && AutomationProperties.GetName(endDate) == "Departure", "The heading-free template lost a localized or explicit accessible name.");
        }
    }

    private static void DateFormatUpdatesSelectionAndDraft(CalendarDateRangePicker picker, DropDownButton button, ComboBox presets, TextBlock summary)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalStrings = picker.Strings;
        try
        {
            var culture = new CultureInfo("th-TH", useUserOverride: false);
            culture.DateTimeFormat.Calendar = new ThaiBuddhistCalendar();
            CultureInfo.CurrentCulture = culture;
            picker.Strings = new() { DateRangeFormat = "{1:dd} / {0:dd}", DayCountFormat = "{0}", SummaryFormat = "{0} ({1})" };
            var start = new DateTimeOffset(2026, 11, 2, 0, 0, 0, TimeSpan.Zero);
            var end = start.AddDays(7);
            picker.Selection = new(new DateRange(start, end));
            const string range = "2026-11-09 / 2026-11-02";
            Check(Equals(button.Content, range) && AutomationProperties.GetItemStatus(button) == range && summary.Text == range + " (8)",
                "The XAML date format did not produce ISO dates in the requested range order.");

            var singleDay = new DateRange(start, start);
            picker.Presets = [new DateRangePreset("One day", singleDay)];
            presets.SelectedIndex = 0;
            Check(summary.Text == "2026-11-02 (1)", "A single-day draft did not show one ISO date.");

            picker.DateFormat = "MMMM dd";
            Check(Equals(button.Content, $"{end:MMMM dd} / {start:MMMM dd}") && summary.Text == $"{start:MMMM dd} (1)",
                "Changing to a regional format did not refresh the selection and its independent draft.");
            picker.DateFormat = "T";
            Check(Equals(button.Content, "09 / 02") && summary.Text == $"{start:d} (1)",
                "An invalid date-only format did not restore the localized range and single-day formats.");

            picker.DateFormat = "O";
            picker.Selection = new(singleDay);
            Check(Equals(button.Content, "2026-11-02") && AutomationProperties.GetItemStatus(button) == "2026-11-02",
                "A committed single day did not show and announce one ISO date.");
            picker.ClearValue(CalendarDateRangePicker.DateFormatProperty);
            Check(Equals(button.Content, start.ToString("d", culture)) && summary.Text == $"{start:d} (1)",
                "Clearing DateFormat did not restore the regional short date.");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            picker.Selection = null;
            picker.Presets = null;
            picker.Strings = originalStrings;
        }
    }

    private static void EmptyPlaceholdersCanBeRestored(CalendarDateRangePicker picker, DropDownButton button, ComboBox presets)
    {
        var originalStrings = picker.Strings;
        picker.Strings = new() { PlaceholderText = "Range hint", PresetPlaceholderText = "Preset hint" };
        ToolkitLocalization.InvalidateStringResources();
        Check(Equals(button.Content, string.Empty) && presets.PlaceholderText == string.Empty,
            "An empty XAML placeholder was replaced by a localized prompt.");
        Check(AutomationProperties.GetName(button) == picker.Strings.PickerName, "Hiding the prompt removed the picker's accessible name.");

        picker.Header = "Report dates";
        Check(Equals(button.Content, string.Empty) && AutomationProperties.GetName(button) == "Report dates",
            "Adding a header replaced the explicitly empty prompt or lost the accessible name.");
        picker.ClearValue(CalendarDateRangePicker.PlaceholderTextProperty);
        Check(Equals(button.Content, "Range hint"), "Clearing the range prompt with a header did not restore Strings.");
        picker.PlaceholderText = string.Empty;
        picker.PlaceholderText = null;
        picker.ClearValue(CalendarDateRangePicker.PresetPlaceholderTextProperty);
        Check(Equals(button.Content, "Range hint") && presets.PlaceholderText == "Preset hint",
            "Resetting a prompt did not restore Strings.");
        picker.Strings = new() { PlaceholderText = " " };
        Check(Equals(button.Content, originalStrings.PlaceholderText), "A whitespace-only prompt did not restore the localized default.");
        picker.ClearValue(CalendarDateRangePicker.HeaderProperty);

        picker.Strings = new() { PlaceholderText = string.Empty, PresetPlaceholderText = string.Empty };
        Check(Equals(button.Content, string.Empty) && presets.PlaceholderText == string.Empty, "Strings could not suppress a placeholder.");
        var source = new ContentControl { Content = "Bound hint" };
        picker.SetBinding(CalendarDateRangePicker.PlaceholderTextProperty, new Binding { Source = source, Path = new PropertyPath("Content") });
        Check(Equals(button.Content, "Bound hint"), "A bound placeholder did not override Strings.");
        picker.Strings = originalStrings;
        source.Content = string.Empty;
        Check(Equals(button.Content, string.Empty), "An empty binding value showed the default hint.");
        source.Content = null;
        Check(Equals(button.Content, originalStrings.PlaceholderText), "A null binding value did not restore the localized hint.");
        picker.ClearValue(CalendarDateRangePicker.PlaceholderTextProperty);
    }

    private async Task PlaceholderForegroundFollowsSelection(CalendarDateRangePicker picker, DropDownButton button)
    {
        var content = (FrameworkElement)this._window.Content;
        var hint = (TextBlock)content.FindName("ReferenceHint");
        var normal = (DropDownButton)content.FindName("ReferenceButton");
        var selection = picker.Selection!;
        foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
        {
            content.RequestedTheme = theme;
            foreach (var state in new[] { "Normal", "PointerOver", "Pressed", "Disabled" })
            {
                picker.IsEnabled = normal.IsEnabled = state != "Disabled";
                picker.Selection = null;
                Check(VisualStateManager.GoToState(button, state, false) && VisualStateManager.GoToState(normal, state, false),
                    $"The native button template did not enter {state}.");
                await Task.Yield();
                var expected = picker.IsEnabled ? hint.Foreground : RenderedText(normal).Foreground;
                Check(Equals(RenderedText(button).Foreground, expected), $"The rendered {theme} {state} hint used the wrong brush.");
                picker.Selection = selection;
                Check(Equals(RenderedText(button).Foreground, RenderedText(normal).Foreground),
                    $"The rendered {theme} {state} selection did not use the native button brush.");
            }
        }

        static TextBlock RenderedText(DropDownButton target)
        {
            target.UpdateLayout();
            var root = (FrameworkElement)VisualTreeHelper.GetChild(target, 0);
            var presenter = (ContentPresenter)root.FindName("ContentPresenter");
            return (TextBlock)VisualTreeHelper.GetChild(presenter, 0);
        }
    }

    private static void Check(bool passed, string message)
    {
        if (!passed)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class TestLoader : IStringResourceLoader
    {
        public string Language { get; set; } = string.Empty;

        public string? GetString(string catalogName, string key) => key switch
        {
            "DateRangeFormat" => this.Language + " {0:dd}-{1:dd}",
            "PlaceholderText" or "PickerName" or "ApplyButtonText" or "PresetPlaceholderText" or "StartDateLabel"
                or "StartCalendarName" or "EndCalendarName" or "PresetLabel" => this.Language + " " + key,
            _ => null,
        };
    }
}
