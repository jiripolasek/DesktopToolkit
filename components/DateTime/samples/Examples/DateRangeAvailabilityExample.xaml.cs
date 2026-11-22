// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace JPSoftworks.DesktopToolkit.Controls.Samples;

public sealed partial class DateRangeAvailabilityExample : Page
{
    private const int MinimumBookingDays = 3;

    private static readonly DateTimeOffset Month = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    public DateTimeOffset MinimumDate { get; } = Month.AddDays(7);

    public DateTimeOffset MaximumDate { get; } = Month.AddDays(20);

    public DateTimeOffset[] BlackoutDates { get; } = [Month.AddDays(11), Month.AddDays(16)];

    public CalendarDateRangePickerStrings BookingStrings { get; } = new()
    {
        DayCountFormat = "Counted days: {0:N0}",
    };

    public string Availability =>
        $"Booking window {this.MinimumDate:d} to {this.MaximumDate:d}. "
        + $"{this.BlackoutDates[0]:d} and {this.BlackoutDates[1]:d} are blocked. "
        + $"Bookings require at least {MinimumBookingDays} counted days.";

    public DateRangeAvailabilityExample()
    {
        this.InitializeComponent();
    }

    public FrameworkElement TakeSampleControls()
    {
        this.SampleLayout.Children.Remove(this.BlackoutModePicker);
        return this.BlackoutModePicker;
    }

    private void OnBlackoutModeChanged(object sender, SelectionChangedEventArgs args)
    {
        if (this.BookingDates is { } picker && sender is ComboBox modes)
        {
            picker.BlackoutMode = modes.SelectedIndex switch
            {
                1 => DateRangeBlackoutMode.Ignore,
                2 => DateRangeBlackoutMode.ExcludeFromCount,
                _ => DateRangeBlackoutMode.RejectRange,
            };
        }
    }

    private void OnSelectionApplying(object? sender, DateRangeSelectionApplyingEventArgs args)
    {
        if (args.DayCount < MinimumBookingDays)
        {
            args.ErrorMessage = $"Choose at least {MinimumBookingDays} counted days.";
        }
    }

    private void OnSelectionChanged(object? sender, EventArgs args)
    {
        if (sender is CalendarDateRangePicker { Selection: { } selection })
        {
            this.AppliedCount.Text = $"Counted days in selection: {selection.DayCount:N0}";
            this.AppliedCount.Visibility = Visibility.Visible;
        }
    }
}
