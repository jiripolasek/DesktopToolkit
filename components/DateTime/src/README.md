# Date and Time Controls

The date and time package includes CalendarDateRangePicker for selecting inclusive ranges in WinUI 3.

## Requirements

- Windows 10 version 2004 (build 19041) or later
- A WinUI 3 app targeting .NET 8 or later
- Windows App SDK 1.8.260317003 or later

### .NET 10 trimmed publishing

If a trimmed .NET 10 publish reports `IL2081` in Windows SDK projection code,
add this property to your app's project before restore.

```xml
<PropertyGroup>
  <WindowsSdkPackageMinimumRevision>87</WindowsSdkPackageMinimumRevision>
</PropertyGroup>
```

This sets the minimum projection revision for your app's Windows target. If your SDK still selects an
older revision or your app already pins `WindowsSdkPackageVersion`, set that property to the matching
revision 87 or later, such as `10.0.19041.87` for Windows 19041. Use one projection setting per project.
Remove the workaround once your SDK selects a fixed projection by default.

## Installation

For a local preview, run `./eng/Build.ps1` from the toolkit checkout and add `artifacts/packages` to your NuGet sources.
Keep your regular sources enabled for dependencies.

Run this command from your WinUI app project directory.

```powershell
dotnet add package JPSoftworks.DesktopToolkit.Controls.DateTime --version 0.1.0-preview.2
```

## CalendarDateRangePicker

### Usage

Add the standard `XamlControlsResources` to your app. The package provides the default template.

```xml
<dates:CalendarDateRangePicker
    xmlns:dates="using:JPSoftworks.DesktopToolkit.Controls"
    Header="Reporting dates"
    Selection="{x:Bind ViewModel.ReportingDates, Mode=TwoWay}"
    Presets="{x:Bind ViewModel.DatePresets, Mode=OneWay}">
    <dates:CalendarDateRangePicker.FlyoutHeader>
        <TextBlock Text="Website traffic report" />
    </dates:CalendarDateRangePicker.FlyoutHeader>
</dates:CalendarDateRangePicker>
```

Set `Selection` to a `DateRangeSelection` or null. Each inclusive `DateRange`
uses the supplied year, month, and day. Its dates are represented at midnight
UTC. Supply a new `DateRangePreset[]` to replace the preset list and set
`MinimumDate` and `MaximumDate` to constrain edited dates.

Set `DateFormat="O"` to show ISO 8601 dates in the dropdown button and the live flyout summary.
A one-day selection displays `2026-11-02`. A range displays `2026-11-02 - 2026-11-09` with the default separator.
`DateFormat` accepts .NET `DateOnly` formats, including regional patterns such as `dd MMMM`.
Clear it to restore localized date formatting. Use `CalendarDateRangePickerStrings.DateRangeFormat` to customize the order and separator of the two endpoints.

The start and end calendars sit side by side, highlight the draft range, and
use the first day of the week from the current regional culture.
The summary shows the dates and their day count. `DayCountFormat` uses one format for every count, such as `Days: {0:N0}`.
Selecting a day or a preset edits the draft. **Apply** commits a selection and raises
`SelectionChanged` when it changes. **Cancel** and dismissing the flyout
discard draft edits. Changes to bounds do not replace an existing selection.
Assign a `CalendarDateRangePickerStrings` instance to `Strings` to localize the prompts, button and calendar labels,
validation messages, date summaries, and accessibility text. Missing overrides use the library's localized resources,
with English as the neutral fallback.
Set `PlaceholderText`, `PresetPlaceholderText`, `ApplyButtonText`, or `CancelButtonText` on the picker
to override an individual label. Set `PlaceholderText=""` to hide the closed picker's prompt or
`PresetPlaceholderText=""` to hide the preset prompt. An empty string in `CalendarDateRangePickerStrings` also hides that prompt.
Set a control property to null or call `ClearValue` to restore its style or localized value.
Whitespace-only overrides and blank button labels use the corresponding `Strings` value.

Custom templates can use `TemplateBinding` with `EffectivePlaceholderText`, `EffectivePresetPlaceholderText`,
`EffectiveApplyButtonText`, and `EffectiveCancelButtonText` to display the same fallback text as the default template.
The default template uses WinUI's `ComboBoxPlaceHolderForeground` brush for the prompt, including on hover and press.
Selected dates and disabled pickers use the native button foreground for their current state. Custom templates can
implement `Unselected`, `Selected`, and the optional `UnselectedDisabled` state in the `SelectionStates` group.

### Bounds and blackout dates

`MinimumDate` and `MaximumDate` are inclusive. Set `BlackoutDates` to a
`DateTimeOffset[]` of unavailable dates and replace the array to update it. The
picker uses each value's calendar date and treats duplicates as one blocked day.
Blackout dates are unavailable as start and end dates in every mode.

`BlackoutMode` controls how the picker treats blocked days inside a range.

- `RejectRange` (default) shows a validation error on Apply for a range that contains a blackout date.
- `Ignore` accepts the range and counts all days.
- `ExcludeFromCount` accepts the range and skips blocked dates in its count.

Presets must meet the same bounds and blackout rules. Read
`DateRangeSelection.DayCount` for the committed count or `DayCount` on the
`SelectionApplying` event arguments for the pending count. `Range.DayCount`
counts the inclusive calendar span and determines the duration of a preceding
comparison. Restrictions apply when editing and applying a range.

### Custom flyout content

`FlyoutHeader` appears above the presets and calendars. `FlyoutFooter` appears
below the range summary, before the action buttons. Both accept text or a XAML
element and collapse when empty. `Header` labels the closed picker.

For interactive content, use `DraftReset` to restore your controls from the
committed `Selection` when the flyout opens or the selection changes. Read
`DraftRange` in `DraftRangeChanged` for a live preview. `SelectionApplying` runs
after date validation and can supply a `DateRangeSelection` for the validated
range or set `ErrorMessage` to keep the flyout open.

The Gallery's booking example uses `SelectionApplying` to enforce a minimum
of three counted days. Its handler can reject a range after blackout days have
been deducted.

```csharp
private void OnSelectionApplying(object? sender, DateRangeSelectionApplyingEventArgs args)
{
    if (args.DayCount < 3)
    {
        args.ErrorMessage = "Choose at least 3 counted days.";
    }
}
```

The [custom flyout content sample](https://github.com/jiripolasek/DesktopToolkit/blob/main/components/DateTime/samples/Examples/CalendarDateRangePickerExample.xaml)
uses a `ToggleSwitch` in `FlyoutFooter` to opt in to an equal-length comparison
ending the day before the chosen range starts. A selection that already includes
a comparison keeps that option on subsequent applies unless the handler overrides it.

### Styling and templating

Set `CalendarViewStyle` to a `Style` that targets `CalendarView` to configure
both calendars in the default flyout. The example inherits WinUI typography
and colors from `DefaultCalendarViewStyle`. Use the same base for replacement
styles when updating an open flyout. A `FirstDayOfWeek` setter overrides the
regional week start.

```xml
<dates:CalendarDateRangePicker Header="Booking dates">
    <dates:CalendarDateRangePicker.CalendarViewStyle>
        <Style BasedOn="{StaticResource DefaultCalendarViewStyle}" TargetType="CalendarView">
            <Setter Property="FirstDayOfWeek" Value="Monday" />
            <Setter Property="NumberOfWeeksInView" Value="5" />
            <Setter Property="CalendarItemCornerRadius" Value="8" />
        </Style>
    </dates:CalendarDateRangePicker.CalendarViewStyle>
</dates:CalendarDateRangePicker>
```

Override the control template to customize the layout. The native
`PART_Button` (`DropDownButton`) hosts the flyout. The optional `PART_Header`
(`TextBlock`) displays the header. Use `PART_StartCalendar` and
`PART_EndCalendar` (`CalendarView`, with single selection) for the inline
calendars, or keep `PART_StartDate` and `PART_EndDate` (`CalendarDatePicker`)
for date fields. Custom templates can bind `CalendarView.Style` or
`CalendarDatePicker.CalendarViewStyle` to the picker's `CalendarViewStyle`.
`RangeHighlightBrush` and `RangeEndpointBrush` style inline
day cells. Keep `PART_Apply` (`Button`) for committing. Include `PART_Presets`
(`ComboBox`), `PART_Error` (`TextBlock`), and `PART_Cancel` (`Button`) for
presets, validation feedback, and an explicit dismissal action. The optional
`PART_RangeSummary` (`TextBlock`) displays the live draft summary. Use
`PART_FlyoutHeader` and `PART_FlyoutFooter` (`ContentPresenter`) to host the
optional custom content.
