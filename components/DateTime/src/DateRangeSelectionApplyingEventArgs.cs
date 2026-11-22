// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Controls;

/// <summary>Provides the validated draft range and allows customization before commit.</summary>
public sealed class DateRangeSelectionApplyingEventArgs : EventArgs
{
    private DateRangeSelection? _selection;

    /// <summary>Gets the validated draft range.</summary>
    public DateRange Range { get; }

    /// <summary>Gets the number of draft days after any blackout date exclusions.</summary>
    public int DayCount { get; }

    /// <summary>Gets or sets a selection to commit for this range. Null uses the default selection.</summary>
    /// <exception cref="ArgumentException">The selection's range differs from <see cref="Range"/>.</exception>
    public DateRangeSelection? Selection
    {
        get => this._selection;
        set
        {
            if (value is not null && value.Range != this.Range)
            {
                throw new ArgumentException("The selection must use the validated draft range.", nameof(value));
            }

            this._selection = value;
        }
    }

    /// <summary>Gets or sets an error to display in the flyout. A non-empty message prevents commit.</summary>
    public string? ErrorMessage { get; set; }

    internal DateRangeSelectionApplyingEventArgs(DateRange range, int dayCount)
    {
        this.Range = range;
        this.DayCount = dayCount;
    }
}
