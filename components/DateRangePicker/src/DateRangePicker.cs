// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace JPSoftworks.DesktopToolkit.Controls;

/// <summary>Presents a flyout for editing and committing an inclusive date range.</summary>
[TemplatePart(Name = "PART_Header", Type = typeof(TextBlock))]
[TemplatePart(Name = "PART_Button", Type = typeof(DropDownButton))]
[TemplatePart(Name = "PART_StartCalendar", Type = typeof(CalendarView))]
[TemplatePart(Name = "PART_EndCalendar", Type = typeof(CalendarView))]
[TemplatePart(Name = "PART_StartDate", Type = typeof(CalendarDatePicker))]
[TemplatePart(Name = "PART_EndDate", Type = typeof(CalendarDatePicker))]
[TemplatePart(Name = "PART_Presets", Type = typeof(ComboBox))]
[TemplatePart(Name = "PART_Comparison", Type = typeof(CheckBox))]
[TemplatePart(Name = "PART_RangeSummary", Type = typeof(TextBlock))]
[TemplatePart(Name = "PART_ComparisonSummary", Type = typeof(TextBlock))]
[TemplatePart(Name = "PART_Error", Type = typeof(TextBlock))]
[TemplatePart(Name = "PART_Apply", Type = typeof(Button))]
[TemplatePart(Name = "PART_Cancel", Type = typeof(Button))]
public sealed partial class DateRangePicker : Control
{
    /// <summary>Occurs when the committed selection changes through input or binding.</summary>
    public event EventHandler? SelectionChanged;

    /// <summary>Identifies the <see cref="Selection"/> dependency property.</summary>
    public static readonly DependencyProperty SelectionProperty = DependencyProperty.Register(
        nameof(Selection), typeof(DateRangeSelection), typeof(DateRangePicker), new PropertyMetadata(null, OnSelectionChanged));

    /// <summary>Identifies the <see cref="MinimumDate"/> dependency property.</summary>
    public static readonly DependencyProperty MinimumDateProperty = DependencyProperty.Register(
        nameof(MinimumDate), typeof(DateTimeOffset), typeof(DateRangePicker),
        new PropertyMetadata(new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.Zero), OnOptionsChanged));

    /// <summary>Identifies the <see cref="MaximumDate"/> dependency property.</summary>
    public static readonly DependencyProperty MaximumDateProperty = DependencyProperty.Register(
        nameof(MaximumDate), typeof(DateTimeOffset), typeof(DateRangePicker),
        new PropertyMetadata(new DateTimeOffset(2100, 12, 31, 0, 0, 0, TimeSpan.Zero), OnOptionsChanged));

    /// <summary>Identifies the <see cref="Presets"/> dependency property.</summary>
    public static readonly DependencyProperty PresetsProperty = DependencyProperty.Register(
        nameof(Presets), typeof(DateRangePreset[]), typeof(DateRangePicker), new PropertyMetadata(null, OnOptionsChanged));

    /// <summary>Identifies the <see cref="IsComparisonEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty IsComparisonEnabledProperty = DependencyProperty.Register(
        nameof(IsComparisonEnabled), typeof(bool), typeof(DateRangePicker), new PropertyMetadata(false, OnOptionsChanged));

    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
        nameof(Header), typeof(string), typeof(DateRangePicker), new PropertyMetadata(null, OnSummaryChanged));

    /// <summary>Identifies the <see cref="PlaceholderText"/> dependency property.</summary>
    public static readonly DependencyProperty PlaceholderTextProperty = DependencyProperty.Register(
        nameof(PlaceholderText), typeof(string), typeof(DateRangePicker), new PropertyMetadata("Select dates", OnSummaryChanged));

    /// <summary>Identifies the <see cref="RangeHighlightBrush"/> dependency property.</summary>
    public static readonly DependencyProperty RangeHighlightBrushProperty = DependencyProperty.Register(
        nameof(RangeHighlightBrush), typeof(Brush), typeof(DateRangePicker), new PropertyMetadata(null, OnRangeAppearanceChanged));

    /// <summary>Identifies the <see cref="RangeEndpointBrush"/> dependency property.</summary>
    public static readonly DependencyProperty RangeEndpointBrushProperty = DependencyProperty.Register(
        nameof(RangeEndpointBrush), typeof(Brush), typeof(DateRangePicker), new PropertyMetadata(null, OnRangeAppearanceChanged));

    private readonly Dictionary<CalendarViewDayItem, (object? Background, object? Status)> _dayItems = [];

    private TextBlock? _header;
    private DropDownButton? _button;
    private FlyoutBase? _flyout;
    private CalendarView? _startCalendar;
    private CalendarView? _endCalendar;
    private bool _settingDraft;
    private DateTimeOffset? _highlightStart;
    private DateTimeOffset? _highlightEnd;
    private CalendarDatePicker? _start;
    private CalendarDatePicker? _end;
    private ComboBox? _presets;
    private CheckBox? _comparison;
    private TextBlock? _rangeSummary;
    private TextBlock? _comparisonSummary;
    private TextBlock? _error;
    private Button? _apply;
    private Button? _cancel;
    private DateRangePreset[] _visiblePresets = [];

    /// <summary>Gets or sets the committed selection. Null clears the selection.</summary>
    public DateRangeSelection? Selection
    {
        get => (DateRangeSelection?)this.GetValue(SelectionProperty);
        set => this.SetValue(SelectionProperty, value);
    }

    /// <summary>Gets or sets the earliest date accepted when applying a draft.</summary>
    public DateTimeOffset MinimumDate
    {
        get => (DateTimeOffset)this.GetValue(MinimumDateProperty);
        set => this.SetValue(MinimumDateProperty, value);
    }

    /// <summary>Gets or sets the latest date accepted when applying a draft.</summary>
    public DateTimeOffset MaximumDate
    {
        get => (DateTimeOffset)this.GetValue(MaximumDateProperty);
        set => this.SetValue(MaximumDateProperty, value);
    }

    /// <summary>Gets or sets the presets. Replace the array to update the list.</summary>
    public DateRangePreset[]? Presets
    {
        get => (DateRangePreset[]?)this.GetValue(PresetsProperty);
        set => this.SetValue(PresetsProperty, value);
    }

    /// <summary>Gets or sets whether users can add a preceding-period comparison in the flyout.</summary>
    public bool IsComparisonEnabled
    {
        get => (bool)this.GetValue(IsComparisonEnabledProperty);
        set => this.SetValue(IsComparisonEnabledProperty, value);
    }

    /// <summary>Gets or sets the optional label above the date range.</summary>
    public string? Header
    {
        get => (string?)this.GetValue(HeaderProperty);
        set => this.SetValue(HeaderProperty, value);
    }

    /// <summary>Gets or sets the text displayed when no range is selected.</summary>
    public string PlaceholderText
    {
        get => (string)this.GetValue(PlaceholderTextProperty);
        set => this.SetValue(PlaceholderTextProperty, value);
    }

    /// <summary>Gets or sets the brush for days between the two draft endpoints.</summary>
    public Brush? RangeHighlightBrush
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(Brush))]
        get => (Brush?)this.GetValue(RangeHighlightBrushProperty);
        set => this.SetValue(RangeHighlightBrushProperty, value);
    }

    /// <summary>Gets or sets the brush for the draft start and end dates.</summary>
    public Brush? RangeEndpointBrush
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(Brush))]
        get => (Brush?)this.GetValue(RangeEndpointBrushProperty);
        set => this.SetValue(RangeEndpointBrushProperty, value);
    }

    private bool CompareDraft => this.IsComparisonEnabled && this._comparison is not null
        ? this._comparison.IsChecked == true
        : this.Selection?.ComparisonRange is not null;

    /// <summary>Initializes a date range picker with the native calendar flyout template.</summary>
    public DateRangePicker()
    {
        this.DefaultStyleKey = typeof(DateRangePicker);
    }

    /// <inheritdoc />
    [WinRT.DynamicWindowsRuntimeCast(typeof(TextBlock))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(DropDownButton))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(CalendarView))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(CalendarDatePicker))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(ComboBox))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(CheckBox))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(Button))]
    protected override void OnApplyTemplate()
    {
        if (this._flyout is not null)
        {
            this._flyout.Opening -= this.OnOpening;
            this._flyout.Hide();
        }

        if (this._presets is not null)
        {
            this._presets.SelectionChanged -= this.OnPresetChanged;
        }

        if (this._apply is not null)
        {
            this._apply.Click -= this.OnApply;
        }

        if (this._cancel is not null)
        {
            this._cancel.Click -= this.OnCancel;
        }

        this.UnhookCalendar(this._startCalendar);
        this.UnhookCalendar(this._endCalendar);
        if (this._start is not null)
        {
            this._start.DateChanged -= this.OnDateChanged;
        }

        if (this._end is not null)
        {
            this._end.DateChanged -= this.OnDateChanged;
        }

        if (this._comparison is not null)
        {
            this._comparison.Checked -= this.OnComparisonChanged;
            this._comparison.Unchecked -= this.OnComparisonChanged;
        }

        foreach (var (item, original) in this._dayItems)
        {
            RestoreDayStyle(item, original);
        }

        this._dayItems.Clear();

        base.OnApplyTemplate();
        this._header = this.GetTemplateChild("PART_Header") as TextBlock;
        this._button = this.GetTemplateChild("PART_Button") as DropDownButton;
        this._flyout = this._button?.Flyout;
        this._startCalendar = this.GetTemplateChild("PART_StartCalendar") as CalendarView;
        this._endCalendar = this.GetTemplateChild("PART_EndCalendar") as CalendarView;
        this._start = this.GetTemplateChild("PART_StartDate") as CalendarDatePicker;
        this._end = this.GetTemplateChild("PART_EndDate") as CalendarDatePicker;
        this._presets = this.GetTemplateChild("PART_Presets") as ComboBox;
        this._comparison = this.GetTemplateChild("PART_Comparison") as CheckBox;
        this._rangeSummary = this.GetTemplateChild("PART_RangeSummary") as TextBlock;
        this._comparisonSummary = this.GetTemplateChild("PART_ComparisonSummary") as TextBlock;
        this._error = this.GetTemplateChild("PART_Error") as TextBlock;
        this._apply = this.GetTemplateChild("PART_Apply") as Button;
        this._cancel = this.GetTemplateChild("PART_Cancel") as Button;

        if (this._flyout is not null)
        {
            this._flyout.Opening += this.OnOpening;
        }

        if (this._presets is not null)
        {
            this._presets.SelectionChanged += this.OnPresetChanged;
        }

        if (this._apply is not null)
        {
            this._apply.Click += this.OnApply;
        }

        if (this._cancel is not null)
        {
            this._cancel.Click += this.OnCancel;
        }

        this.HookCalendar(this._startCalendar);
        this.HookCalendar(this._endCalendar);
        if (this._start is not null)
        {
            this._start.DateChanged += this.OnDateChanged;
        }

        if (this._end is not null)
        {
            this._end.DateChanged += this.OnDateChanged;
        }

        if (this._comparison is not null)
        {
            this._comparison.Checked += this.OnComparisonChanged;
            this._comparison.Unchecked += this.OnComparisonChanged;
        }

        this.UpdateOptions();
        this.UpdateSummary();
        this.ResetDraft();
    }

    private static void OnSelectionChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var picker = (DateRangePicker)sender;
        picker.UpdateSummary();
        picker.ResetDraft();
        if (!Equals(args.OldValue, args.NewValue))
        {
            picker.SelectionChanged?.Invoke(picker, EventArgs.Empty);
        }
    }

    private static void OnOptionsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((DateRangePicker)sender).UpdateOptions();
    }

    private static void OnSummaryChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((DateRangePicker)sender).UpdateSummary();
    }

    private static void OnRangeAppearanceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((DateRangePicker)sender).UpdateHighlights();
    }

    private void UpdateSummary()
    {
        if (this._header is { } header)
        {
            header.Visibility = string.IsNullOrEmpty(this.Header) ? Visibility.Collapsed : Visibility.Visible;
        }

        if (this._button is not { } button)
        {
            return;
        }

        var range = this.Selection?.Range;
        var text = range is null ? this.PlaceholderText : FormatRange(range);
        button.Content = text;
        var name = AutomationProperties.GetName(this);
        AutomationProperties.SetName(button, !string.IsNullOrEmpty(name) ? name : this.Header ?? "Date range");
        AutomationProperties.SetItemStatus(button, text);
    }

    private void UpdateOptions()
    {
        var min = DateRange.CalendarDay(this.MinimumDate);
        var max = DateRange.CalendarDay(this.MaximumDate);
        SetBounds(this._start, min, max < min ? min : max);
        SetBounds(this._end, min, max < min ? min : max);
        SetCalendarBounds(this._startCalendar, min, max < min ? min : max);
        SetCalendarBounds(this._endCalendar, min, max < min ? min : max);

        if (this._presets is { } presets)
        {
            this._visiblePresets = this.Presets?.Where(preset =>
                                       preset is { Range: not null, Label.Length: > 0 }
                                       && preset.Range.StartDate >= min
                                       && preset.Range.EndDate <= max).ToArray()
                                   ?? [];
            presets.ItemsSource = this._visiblePresets.Select(preset => preset.Label).ToArray();
            presets.Visibility = this._visiblePresets.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        if (this._comparison is { } comparison)
        {
            comparison.Visibility = this.IsComparisonEnabled ? Visibility.Visible : Visibility.Collapsed;
        }

        this.UpdateDraftSummary();
    }

    private void OnOpening(object? sender, object args)
    {
        this.ResetDraft();
    }

    private void ResetDraft()
    {
        if (this._presets is not null)
        {
            this._presets.SelectedIndex = -1;
        }

        this.SetDraft(this.Selection?.Range);
        if (this._comparison is not null)
        {
            this._comparison.IsChecked = this.Selection?.ComparisonRange is not null;
        }

        this.ClearError();
    }

    private void SetDraft(DateRange? range)
    {
        this._settingDraft = true;
        try
        {
            SetCalendarDate(this._startCalendar, range?.StartDate);
            SetCalendarDate(this._endCalendar, range?.EndDate);
            if (this._start is not null)
            {
                this._start.Date = range is null ? null : LocalDate(range.StartDate);
            }

            if (this._end is not null)
            {
                this._end.Date = range is null ? null : LocalDate(range.EndDate);
            }
        }
        finally
        {
            this._settingDraft = false;
        }

        this.UpdateDraftPresentation();
    }

    private void OnPresetChanged(object sender, SelectionChangedEventArgs args)
    {
        if (this._presets is { SelectedIndex: >= 0 } presets && presets.SelectedIndex < this._visiblePresets.Length)
        {
            this.SetDraft(this._visiblePresets[presets.SelectedIndex].Range);
            this.ClearError();
        }
    }

    private void OnApply(object sender, RoutedEventArgs args)
    {
        if (ReadDraftDate(this._startCalendar, this._start) is not { } first || ReadDraftDate(this._endCalendar, this._end) is not { } last)
        {
            this.ShowError("Choose a start and end date.");
            return;
        }

        if (last < first)
        {
            this.ShowError("The end date must be on or after the start date.");
            return;
        }

        if (first < DateRange.CalendarDay(this.MinimumDate) || last > DateRange.CalendarDay(this.MaximumDate))
        {
            this.ShowError("Choose dates within the allowed range.");
            return;
        }

        var range = new DateRange(first, last);
        var compare = this.CompareDraft;
        if (compare && !range.HasPreviousPeriod)
        {
            this.ShowError("The previous period is outside the calendar.");
            return;
        }

        var next = new DateRangeSelection(range, compare);
        if (this.Selection != next)
        {
            this.Selection = next;
        }

        this._flyout?.Hide();
    }

    private void OnCancel(object sender, RoutedEventArgs args)
    {
        this._flyout?.Hide();
    }

    private void ShowError(string text)
    {
        if (this._error is { } error)
        {
            error.Text = text;
            error.Visibility = Visibility.Visible;
        }
    }

    private void ClearError()
    {
        if (this._error is not null)
        {
            this._error.Visibility = Visibility.Collapsed;
        }
    }

    private void HookCalendar(CalendarView? calendar)
    {
        if (calendar is not null)
        {
            calendar.CalendarViewDayItemChanging += this.OnCalendarDayChanging;
            calendar.SelectedDatesChanged += this.OnCalendarSelectionChanged;
        }
    }

    private void UnhookCalendar(CalendarView? calendar)
    {
        if (calendar is not null)
        {
            calendar.CalendarViewDayItemChanging -= this.OnCalendarDayChanging;
            calendar.SelectedDatesChanged -= this.OnCalendarSelectionChanged;
        }
    }

    private void OnCalendarSelectionChanged(CalendarView sender, CalendarViewSelectedDatesChangedEventArgs args)
    {
        this.OnDraftEndpointChanged();
    }

    private void OnDateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
    {
        this.OnDraftEndpointChanged();
    }

    private void OnDraftEndpointChanged()
    {
        if (!this._settingDraft)
        {
            if (this._presets is not null)
            {
                this._presets.SelectedIndex = -1;
            }

            this.ClearError();
            this.UpdateDraftPresentation();
        }
    }

    private void OnComparisonChanged(object sender, RoutedEventArgs args)
    {
        if (!this._settingDraft)
        {
            this.ClearError();
            this.UpdateDraftSummary();
        }
    }

    private void UpdateDraftPresentation()
    {
        this.UpdateHighlights();
        this.UpdateDraftSummary();
    }

    private void UpdateDraftSummary()
    {
        if (this._rangeSummary is null && this._comparisonSummary is null)
        {
            return;
        }

        if (ReadDraftDate(this._startCalendar, this._start) is not { } first
            || ReadDraftDate(this._endCalendar, this._end) is not { } last
            || first > last
            || first < DateRange.CalendarDay(this.MinimumDate)
            || last > DateRange.CalendarDay(this.MaximumDate))
        {
            SetSummaryText(this._rangeSummary, null);
            SetSummaryText(this._comparisonSummary, null);
            return;
        }

        var range = new DateRange(first, last);
        var days = range.DayCount == 1 ? "1 day" : $"{range.DayCount:N0} days";
        SetSummaryText(this._rangeSummary, $"{FormatRange(range)} ({days})");

        var comparison = this.CompareDraft && range.HasPreviousPeriod ? new DateRangeSelection(range, true).ComparisonRange : null;
        SetSummaryText(this._comparisonSummary, comparison is null ? null : $"Compared with {FormatRange(comparison)}");
    }

    private static void SetSummaryText(TextBlock? part, string? text)
    {
        if (part is not null)
        {
            part.Text = text ?? string.Empty;
            part.Visibility = text is null ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    private void OnCalendarDayChanging(CalendarView sender, CalendarViewDayItemChangingEventArgs args)
    {
        var item = args.Item;
        if (args.InRecycleQueue)
        {
            if (this._dayItems.Remove(item, out var original))
            {
                RestoreDayStyle(item, original);
            }

            return;
        }

        if (!this._dayItems.TryGetValue(item, out var style))
        {
            style = (item.ReadLocalValue(BackgroundProperty), item.ReadLocalValue(AutomationProperties.ItemStatusProperty));
            this._dayItems.Add(item, style);
        }

        this.UpdateDayStyle(item, style);
    }

    private void UpdateHighlights()
    {
        this._highlightStart = ReadDraftDate(this._startCalendar, this._start);
        this._highlightEnd = ReadDraftDate(this._endCalendar, this._end);
        foreach (var (item, original) in this._dayItems)
        {
            this.UpdateDayStyle(item, original);
        }
    }

    private void UpdateDayStyle(CalendarViewDayItem item, (object? Background, object? Status) original)
    {
        var date = DateRange.CalendarDay(item.Date.ToLocalTime());
        var start = date == this._highlightStart;
        var end = date == this._highlightEnd;
        var inside = date > this._highlightStart && date < this._highlightEnd;
        var brush = start || end ? this.RangeEndpointBrush : inside ? this.RangeHighlightBrush : null;
        var status = (start, end, inside) switch
        {
            (true, true, _) => "Start and end of range",
            (true, false, _) => "Start of range",
            (false, true, _) => "End of range",
            (_, _, true) => "In selected range",
            _ => null,
        };

        SetLocalValue(item, BackgroundProperty, (object?)brush ?? original.Background);
        SetLocalValue(item, AutomationProperties.ItemStatusProperty, (object?)status ?? original.Status);
    }

    private static void RestoreDayStyle(CalendarViewDayItem item, (object? Background, object? Status) original)
    {
        SetLocalValue(item, BackgroundProperty, original.Background);
        SetLocalValue(item, AutomationProperties.ItemStatusProperty, original.Status);
    }

    private static void SetLocalValue(DependencyObject item, DependencyProperty property, object? value)
    {
        if (Equals(item.ReadLocalValue(property), value))
        {
            return;
        }

        if (Equals(value, DependencyProperty.UnsetValue))
        {
            item.ClearValue(property);
        }
        else
        {
            item.SetValue(property, value);
        }
    }

    private static DateTimeOffset? ReadDraftDate(CalendarView? calendar, CalendarDatePicker? picker)
    {
        var date = calendar is null ? picker?.Date : calendar.SelectedDates.Count > 0 ? calendar.SelectedDates[0] : (DateTimeOffset?)null;
        return date is { } day ? DateRange.CalendarDay(day.ToLocalTime()) : null;
    }

    private static void SetCalendarDate(CalendarView? calendar, DateTimeOffset? date)
    {
        if (calendar is null)
        {
            return;
        }

        calendar.SelectedDates.Clear();
        var display = date is { } day ? LocalDate(day) : DateTimeOffset.Now;
        if (date.HasValue && display >= calendar.MinDate && display <= calendar.MaxDate)
        {
            calendar.SelectedDates.Add(display);
        }

        if (display < calendar.MinDate || display > calendar.MaxDate)
        {
            display = display < calendar.MinDate ? calendar.MinDate : calendar.MaxDate;
        }

        calendar.SetDisplayDate(display);
    }

    private static void SetCalendarBounds(CalendarView? calendar, DateTimeOffset min, DateTimeOffset max)
    {
        if (calendar is null)
        {
            return;
        }

        var first = LocalDate(min);
        var last = LocalDate(max);
        if (first > calendar.MaxDate)
        {
            calendar.MaxDate = last;
        }

        calendar.MinDate = first;
        calendar.MaxDate = last;
    }

    private static void SetBounds(CalendarDatePicker? picker, DateTimeOffset min, DateTimeOffset max)
    {
        if (picker is null)
        {
            return;
        }

        var first = LocalDate(min);
        var last = LocalDate(max);
        if (first > picker.MaxDate)
        {
            picker.MaxDate = last;
        }

        picker.MinDate = first;
        picker.MaxDate = last;
    }

    private static DateTimeOffset LocalDate(DateTimeOffset date)
    {
        return new(date.Date, TimeZoneInfo.Local.GetUtcOffset(date.Date));
    }

    private static string FormatRange(DateRange range)
    {
        return range.StartDate == range.EndDate ? $"{range.EndDate:d}" : $"{range.StartDate:d} - {range.EndDate:d}";
    }
}
