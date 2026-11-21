# DateRangePicker

A WinUI 3 custom control for choosing inclusive calendar dates. Add the
`JPSoftworks.DesktopToolkit.Controls.DateRangePicker` package to a WinUI app with the
standard `XamlControlsResources`. The default template is provided by the package.

```xml
<dates:DateRangePicker
    xmlns:dates="using:JPSoftworks.DesktopToolkit.Controls"
    Header="Reporting dates"
    IsComparisonEnabled="True"
    Selection="{x:Bind ViewModel.ReportingDates, Mode=TwoWay}"
    Presets="{x:Bind ViewModel.DatePresets, Mode=OneWay}" />
```

Set `Selection` to a `DateRangeSelection` or null. Each inclusive `DateRange`
uses the supplied year, month, and day. Its dates are represented at midnight
UTC. Supply a new `DateRangePreset[]` to replace the preset list and set
`MinimumDate` and `MaximumDate` to constrain edited dates. The optional
preceding-period comparison has the selected range's duration and ends the
day before it starts. It may precede the selectable minimum.

The start and end calendars sit side by side and highlight the draft range.
The summary shows the dates, inclusive day count, and comparison dates when enabled.
Selecting a day or a preset edits the draft. **Apply** commits a selection and raises
`SelectionChanged` when it changes. **Cancel** and dismissing the flyout
discard draft edits. Changes to bounds do not replace an existing selection.

## Styling and templating

Override the control template to customize the layout. The native
`PART_Button` (`DropDownButton`) hosts the flyout. The optional `PART_Header`
(`TextBlock`) displays the header. Use `PART_StartCalendar` and
`PART_EndCalendar` (`CalendarView`, with single selection) for the inline
calendars, or keep `PART_StartDate` and `PART_EndDate` (`CalendarDatePicker`)
for date fields. `RangeHighlightBrush` and `RangeEndpointBrush` style inline
day cells. Keep `PART_Apply` (`Button`) for committing. Include `PART_Presets`
(`ComboBox`), `PART_Comparison` (`CheckBox`), `PART_Error` (`TextBlock`),
and `PART_Cancel` (`Button`) for presets, comparisons, validation feedback,
and an explicit dismissal action. The optional `PART_RangeSummary` and
`PART_ComparisonSummary` (`TextBlock`) display the live draft summary.
