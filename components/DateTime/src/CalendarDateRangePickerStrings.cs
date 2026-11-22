// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace JPSoftworks.DesktopToolkit.Controls;

/// <summary>Provides labels, messages, and formats for a date range picker.</summary>
public sealed partial class CalendarDateRangePickerStrings
{
    private readonly string? _pickerName;
    private readonly string? _placeholderText;
    private readonly string? _presetPlaceholderText;
    private readonly string? _applyButtonText;
    private readonly string? _cancelButtonText;
    private readonly string? _startDateLabel;
    private readonly string? _endDateLabel;
    private readonly string? _startCalendarName;
    private readonly string? _endCalendarName;
    private readonly string? _presetLabel;
    private readonly string? _previousPeriodError;
    private readonly string? _missingDatesError;
    private readonly string? _reversedDatesError;
    private readonly string? _outOfBoundsError;
    private readonly string? _unavailableEndpointError;
    private readonly string? _unavailableDateError;
    private readonly string? _unavailableDateStatus;
    private readonly string? _singleDateStatus;
    private readonly string? _startDateStatus;
    private readonly string? _endDateStatus;
    private readonly string? _inRangeStatus;
    private readonly string? _dateRangeFormat;
    private readonly string? _dayCountFormat;
    private readonly string? _summaryFormat;
    private readonly string? _summaryWithExcludedDaysFormat;

    /// <summary>Gets the shared localized strings.</summary>
    public static CalendarDateRangePickerStrings Default { get; } = new();

    /// <summary>Gets the accessible name when the picker has no header or explicit name.</summary>
    public string PickerName
    {
        get => WinUIResourceStrings.GetOverride(this._pickerName, nameof(PickerName));
        init => this._pickerName = value;
    }

    /// <summary>Gets the prompt shown when no range is selected.</summary>
    /// <remarks>An empty string hides the prompt.</remarks>
    [AllowNull]
    public string PlaceholderText
    {
        get => WinUIResourceStrings.GetPlaceholderOverride(this._placeholderText)
            ?? WinUIResourceStrings.Get(nameof(PlaceholderText));
        init => this._placeholderText = value;
    }

    /// <summary>Gets the prompt shown when no preset is selected.</summary>
    /// <remarks>An empty string hides the prompt.</remarks>
    [AllowNull]
    public string PresetPlaceholderText
    {
        get => WinUIResourceStrings.GetPlaceholderOverride(this._presetPlaceholderText)
            ?? WinUIResourceStrings.Get(nameof(PresetPlaceholderText));
        init => this._presetPlaceholderText = value;
    }

    /// <summary>Gets the text for the button that commits the selection.</summary>
    public string ApplyButtonText
    {
        get => WinUIResourceStrings.GetOverride(this._applyButtonText, nameof(ApplyButtonText));
        init => this._applyButtonText = value;
    }

    /// <summary>Gets the text for the button that discards draft edits.</summary>
    public string CancelButtonText
    {
        get => WinUIResourceStrings.GetOverride(this._cancelButtonText, nameof(CancelButtonText));
        init => this._cancelButtonText = value;
    }

    /// <summary>Gets the start calendar heading.</summary>
    public string StartDateLabel
    {
        get => WinUIResourceStrings.GetOverride(this._startDateLabel, nameof(StartDateLabel));
        init => this._startDateLabel = value;
    }

    /// <summary>Gets the end calendar heading.</summary>
    public string EndDateLabel
    {
        get => WinUIResourceStrings.GetOverride(this._endDateLabel, nameof(EndDateLabel));
        init => this._endDateLabel = value;
    }

    /// <summary>Gets the accessible name of the start calendar.</summary>
    public string StartCalendarName
    {
        get => WinUIResourceStrings.GetOverride(this._startCalendarName, nameof(StartCalendarName));
        init => this._startCalendarName = value;
    }

    /// <summary>Gets the accessible name of the end calendar.</summary>
    public string EndCalendarName
    {
        get => WinUIResourceStrings.GetOverride(this._endCalendarName, nameof(EndCalendarName));
        init => this._endCalendarName = value;
    }

    /// <summary>Gets the heading of the preset selector.</summary>
    public string PresetLabel
    {
        get => WinUIResourceStrings.GetOverride(this._presetLabel, nameof(PresetLabel));
        init => this._presetLabel = value;
    }

    /// <summary>Gets the error when the comparison range is outside the calendar.</summary>
    public string PreviousPeriodError
    {
        get => WinUIResourceStrings.GetOverride(this._previousPeriodError, nameof(PreviousPeriodError));
        init => this._previousPeriodError = value;
    }

    /// <summary>Gets the error when the draft is missing a start or end date.</summary>
    public string MissingDatesError
    {
        get => WinUIResourceStrings.GetOverride(this._missingDatesError, nameof(MissingDatesError));
        init => this._missingDatesError = value;
    }

    /// <summary>Gets the error when the end date precedes the start date.</summary>
    public string ReversedDatesError
    {
        get => WinUIResourceStrings.GetOverride(this._reversedDatesError, nameof(ReversedDatesError));
        init => this._reversedDatesError = value;
    }

    /// <summary>Gets the error when the draft is outside the allowed date range.</summary>
    public string OutOfBoundsError
    {
        get => WinUIResourceStrings.GetOverride(this._outOfBoundsError, nameof(OutOfBoundsError));
        init => this._outOfBoundsError = value;
    }

    /// <summary>Gets the error when a draft endpoint is unavailable.</summary>
    public string UnavailableEndpointError
    {
        get => WinUIResourceStrings.GetOverride(this._unavailableEndpointError, nameof(UnavailableEndpointError));
        init => this._unavailableEndpointError = value;
    }

    /// <summary>Gets the error when an unavailable date occurs within the draft range.</summary>
    public string UnavailableDateError
    {
        get => WinUIResourceStrings.GetOverride(this._unavailableDateError, nameof(UnavailableDateError));
        init => this._unavailableDateError = value;
    }

    /// <summary>Gets the accessible status of an unavailable day.</summary>
    public string UnavailableDateStatus
    {
        get => WinUIResourceStrings.GetOverride(this._unavailableDateStatus, nameof(UnavailableDateStatus));
        init => this._unavailableDateStatus = value;
    }

    /// <summary>Gets the accessible status of a day that starts and ends the range.</summary>
    public string SingleDateStatus
    {
        get => WinUIResourceStrings.GetOverride(this._singleDateStatus, nameof(SingleDateStatus));
        init => this._singleDateStatus = value;
    }

    /// <summary>Gets the accessible status of the first day in the range.</summary>
    public string StartDateStatus
    {
        get => WinUIResourceStrings.GetOverride(this._startDateStatus, nameof(StartDateStatus));
        init => this._startDateStatus = value;
    }

    /// <summary>Gets the accessible status of the last day in the range.</summary>
    public string EndDateStatus
    {
        get => WinUIResourceStrings.GetOverride(this._endDateStatus, nameof(EndDateStatus));
        init => this._endDateStatus = value;
    }

    /// <summary>Gets the accessible status of a day between the range endpoints.</summary>
    public string InRangeStatus
    {
        get => WinUIResourceStrings.GetOverride(this._inRangeStatus, nameof(InRangeStatus));
        init => this._inRangeStatus = value;
    }

    /// <summary>Gets the format for a multi-day range. Arguments are the start and end dates.</summary>
    /// <remarks>When <see cref="CalendarDateRangePicker.DateFormat"/> is set, the arguments contain the formatted dates.</remarks>
    public string DateRangeFormat
    {
        get => WinUIResourceStrings.GetOverride(this._dateRangeFormat, nameof(DateRangeFormat));
        init => this._dateRangeFormat = value;
    }

    /// <summary>Gets the day count format for all counts. The argument is the day count.</summary>
    public string DayCountFormat
    {
        get => WinUIResourceStrings.GetOverride(this._dayCountFormat, nameof(DayCountFormat));
        init => this._dayCountFormat = value;
    }

    /// <summary>Gets the summary format. Arguments are the formatted date range and day count.</summary>
    public string SummaryFormat
    {
        get => WinUIResourceStrings.GetOverride(this._summaryFormat, nameof(SummaryFormat));
        init => this._summaryFormat = value;
    }

    /// <summary>Gets the summary format when dates are excluded. The third argument is the excluded day count.</summary>
    public string SummaryWithExcludedDaysFormat
    {
        get => WinUIResourceStrings.GetOverride(this._summaryWithExcludedDaysFormat, nameof(SummaryWithExcludedDaysFormat));
        init => this._summaryWithExcludedDaysFormat = value;
    }

    /// <summary>Initializes a new set of date range picker strings.</summary>
    public CalendarDateRangePickerStrings() { }

    internal static string WithFallback(string? text, string fallback) => string.IsNullOrWhiteSpace(text) ? fallback : text;

    internal string FormatRange(DateRange range, string? dateFormat)
    {
        var start = TryFormatDate(range.StartDate, dateFormat);
        if (range.StartDate == range.EndDate)
        {
            return start ?? TryFormatDate(range.StartDate, "d") ?? range.StartDate.ToString("d", CultureInfo.InvariantCulture);
        }

        return start is not null && TryFormatDate(range.EndDate, dateFormat) is { } end
            ? WinUIResourceStrings.Format(this._dateRangeFormat, nameof(DateRangeFormat), start, end)
            : WinUIResourceStrings.Format(this._dateRangeFormat, nameof(DateRangeFormat), range.StartDate, range.EndDate);
    }

    private static string? TryFormatDate(DateTimeOffset date, string? format)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return null;
        }

        try
        {
            return DateOnly.FromDateTime(date.Date).ToString(format, CultureInfo.CurrentCulture);
        }
        catch (Exception error) when (error is FormatException or ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    internal string FormatSummary(DateRange range, int dayCount, string? dateFormat)
    {
        var dates = this.FormatRange(range, dateFormat);
        var days = WinUIResourceStrings.Format(this._dayCountFormat, nameof(DayCountFormat), dayCount);
        var excludedDayCount = range.DayCount - dayCount;
        return excludedDayCount == 0
            ? WinUIResourceStrings.Format(this._summaryFormat, nameof(SummaryFormat), dates, days)
            : WinUIResourceStrings.Format(this._summaryWithExcludedDaysFormat, nameof(SummaryWithExcludedDaysFormat), dates, days, excludedDayCount);
    }
}
