// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Controls;

/// <summary>Represents an inclusive range of calendar dates.</summary>
[WinRT.GeneratedBindableCustomProperty]
public sealed partial record DateRange
{
    /// <summary>Gets the first date, represented at midnight UTC.</summary>
    public DateTimeOffset StartDate { get; }

    /// <summary>Gets the last date, represented at midnight UTC.</summary>
    public DateTimeOffset EndDate { get; }

    /// <summary>Gets the number of dates in the range, including both endpoints.</summary>
    public int DayCount => (this.EndDate - this.StartDate).Days + 1;

    internal bool HasPreviousPeriod => (this.StartDate - DateTimeOffset.MinValue).Days >= this.DayCount;

    /// <summary>Creates a range using the supplied year, month, and day values.</summary>
    /// <exception cref="ArgumentException">The end date is before the start date.</exception>
    public DateRange(DateTimeOffset startDate, DateTimeOffset endDate)
    {
        this.StartDate = CalendarDay(startDate);
        this.EndDate = CalendarDay(endDate);
        if (this.EndDate < this.StartDate)
        {
            throw new ArgumentException("The end date must be on or after the start date.", nameof(endDate));
        }
    }

    internal static DateTimeOffset CalendarDay(DateTimeOffset value)
    {
        return new(value.Date, TimeSpan.Zero);
    }
}

/// <summary>Holds the committed range and its optional immediately preceding comparison.</summary>
[WinRT.GeneratedBindableCustomProperty]
public sealed partial record DateRangeSelection
{
    /// <summary>Gets the selected date range.</summary>
    public DateRange Range { get; }

    /// <summary>Gets the preceding range, or null when comparison is off.</summary>
    public DateRange? ComparisonRange { get; }

    /// <summary>Creates a selection, optionally including a comparison with the preceding range of equal duration.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The comparison would precede the earliest representable date.</exception>
    public DateRangeSelection(DateRange range, bool compareWithPreviousPeriod = false)
    {
        ArgumentNullException.ThrowIfNull(range);
        this.Range = range;
        if (compareWithPreviousPeriod)
        {
            if (!range.HasPreviousPeriod)
            {
                throw new ArgumentOutOfRangeException(nameof(range), "The preceding period is outside the calendar.");
            }

            this.ComparisonRange = new DateRange(range.StartDate.AddDays(-range.DayCount), range.StartDate.AddDays(-1));
        }
    }
}

/// <summary>Represents a labeled range available in the picker.</summary>
public sealed class DateRangePreset
{
    /// <summary>Gets the label displayed in the preset list.</summary>
    public string Label { get; }

    /// <summary>Gets the dates applied to the draft when the preset is selected.</summary>
    public DateRange Range { get; }

    /// <summary>Creates a preset for the supplied label and range.</summary>
    public DateRangePreset(string label, DateRange range)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentNullException.ThrowIfNull(range);
        this.Label = label;
        this.Range = range;
    }
}
