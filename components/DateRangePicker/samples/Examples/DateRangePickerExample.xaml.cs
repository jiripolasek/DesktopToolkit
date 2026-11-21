// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml.Controls;

namespace JPSoftworks.DesktopToolkit.Controls.Samples;

public sealed partial class DateRangePickerExample : Page
{
    private static readonly DateTimeOffset End = new(2026, 6, 28, 0, 0, 0, TimeSpan.Zero);

    public DateRangeSelection Selection { get; set; } = new(new DateRange(End.AddDays(-6), End), true);

    public DateRangePreset[] Presets { get; } =
    [
        new("Last 7 days", new DateRange(End.AddDays(-6), End)),
        new("Last 14 days", new DateRange(End.AddDays(-13), End)),
        new("Last 28 days", new DateRange(End.AddDays(-27), End)),
    ];

    public DateRangePickerExample()
    {
        this.InitializeComponent();
        this.ShowSelection(this.Selection);
    }

    private void OnSelectionChanged(object? sender, EventArgs args)
    {
        if (sender is DateRangePicker { Selection: { } selection })
        {
            this.ShowSelection(selection);
        }
    }

    private void ShowSelection(DateRangeSelection selection)
    {
        var text = $"Selected {selection.Range.StartDate:d} - {selection.Range.EndDate:d}";
        if (selection.ComparisonRange is { } comparison)
        {
            text += $"\nCompared with {comparison.StartDate:d} - {comparison.EndDate:d}";
        }

        this.Status.Text = text;
    }
}
