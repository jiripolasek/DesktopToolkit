// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CalendarDayOfWeek = Windows.Globalization.DayOfWeek;

namespace JPSoftworks.DesktopToolkit.Controls.Samples;

public sealed partial class DateRangeCalendarStyleExample : Page
{
    private const double CalendarHeaderHeight = 80;
    private const double CalendarWeekHeight = 45;

    public DateRangeCalendarStyleExample()
    {
        this.InitializeComponent();
        this.WeekStartBox.ItemsSource = Enum.GetNames<CalendarDayOfWeek>();
        this.VisibleWeeksBox.ItemsSource = new[] { 2, 3, 4, 5, 6, 7, 8 };
        this.WeekStartBox.SelectedItem = nameof(CalendarDayOfWeek.Saturday);
        this.VisibleWeeksBox.SelectedItem = 4;
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(Style))]
    private void OnStyleOptionChanged(object sender, SelectionChangedEventArgs args)
    {
        if (this.WeekStartBox?.SelectedItem is not string weekStart || this.VisibleWeeksBox?.SelectedItem is not int weeks)
        {
            return;
        }

        var style = new Style(typeof(CalendarView)) { BasedOn = (Style)this.Resources["CalendarStyle"] };
        style.Setters.Add(new Setter(CalendarView.FirstDayOfWeekProperty, Enum.Parse<CalendarDayOfWeek>(weekStart)));
        style.Setters.Add(new Setter(CalendarView.NumberOfWeeksInViewProperty, weeks));
        style.Setters.Add(new Setter(HeightProperty, CalendarHeaderHeight + weeks * CalendarWeekHeight));
        this.StyledDates.CalendarViewStyle = style;
    }
}
