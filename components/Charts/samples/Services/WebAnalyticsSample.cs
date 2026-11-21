// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Controls.Demo.Services;

internal static class WebAnalyticsSample
{
    internal static readonly DateTimeOffset LastDay = new(2026, 6, 28, 0, 0, 0, TimeSpan.Zero);
    internal static readonly DateTimeOffset FirstDay = LastDay.AddDays(-27);
    internal static readonly string[] Devices = ["Mobile", "Desktop"];
    internal static readonly string[] Channels = ["Organic search", "Direct", "Paid search", "Referral", "Email"];

    internal static readonly string[] LandingPages =
        ["/", "/collections/hiking", "/collections/summer", "/guides/day-hikes", "/products/trail-pack"];

    internal static readonly string[] CheckoutSteps = ["Session start", "Product view", "Add to cart", "Begin checkout", "Purchase"];

    private static readonly int[] ChannelVolume = [29, 20, 15, 9, 6];
    private static readonly double[] DesktopTraffic = [0.10, 0.09, 0.55, 1.14, 1.06, 0.84, 0.45, 0.18];
    private static readonly double[] MobileTraffic = [0.17, 0.07, 0.46, 0.75, 0.82, 0.96, 1.19, 0.64];

    internal static AnalyticsPeriod CreatePeriod(DateTimeOffset first, DateTimeOffset last, int deviceFilter, int channelFilter)
    {
        var days = (last - first).Days + 1;
        var period = new AnalyticsPeriod(first, days);
        for (var day = 0; day < days; day++)
        {
            var date = first.AddDays(day);
            var weekday = ((int)date.DayOfWeek + 6) % 7;
            var dayIndex = (date - LastDay).Days + 55;
            period.DaysByWeekday[weekday]++;
            for (var device = 0; device < Devices.Length; device++)
            {
                if (deviceFilter < 0 || device == deviceFilter)
                {
                    AddDeviceDay(period, day, dayIndex, weekday, device, channelFilter);
                }
            }
        }

        return period;
    }

    private static void AddDeviceDay(AnalyticsPeriod period, int day, int dayIndex, int weekday, int device, int channelFilter)
    {
        var mobile = device == 0;
        var hourlyWeights = mobile ? MobileTraffic : DesktopTraffic;
        var weekendFactor = weekday >= 5 ? (mobile ? 1.05 : 0.65) : 1;
        var stages = period.CheckoutByDevice[device];
        for (var block = 0; block < hourlyWeights.Length; block++)
        {
            for (var channel = 0; channel < Channels.Length; channel++)
            {
                if (channelFilter >= 0 && channelFilter != channel)
                {
                    continue;
                }

                var variation = 0.85 + ((dayIndex * 17 + block * 7 + channel * 13) % 25) / 80.0;
                var campaign = channel == 2 && dayIndex is >= 45 and <= 48 ? 1.65 : 1;
                var growth = 0.83 + dayIndex * 0.005;
                var sessions = (int)Math.Round(
                    ChannelVolume[channel] * hourlyWeights[block] * weekendFactor * (mobile ? 1.35 : 1) * variation * campaign * growth);
                var engaged = (int)Math.Round(sessions * (0.64 + channel * 0.025 + (mobile ? 0 : 0.04)));
                var views = (int)Math.Round(engaged * (0.82 + channel * 0.025));
                var carts = (int)Math.Round(views * (0.22 + channel * 0.025));
                var checkouts = (int)Math.Round(carts * (mobile ? 0.60 : 0.76));
                var purchases = (int)Math.Round(checkouts * (mobile ? 0.70 : 0.82));
                var traffic = new AnalyticsTotals(sessions, engaged, purchases);
                stages[0] += sessions;
                stages[1] += views;
                stages[2] += carts;
                stages[3] += checkouts;
                stages[4] += purchases;
                period.Daily[day] += traffic;
                period.ByChannel[channel] += traffic;
                period.ByDevice[device] += traffic;
                period.ByLandingPage[GetLandingPage(channel, dayIndex, block)] += traffic;
                period.SessionsByHourBlock[weekday, block] += sessions;
                period.Total += traffic;
            }
        }
    }

    private static int GetLandingPage(int channel, int day, int block)
    {
        return (day + block) % 5 < 3
            ? channel switch
            {
                0 => 1,
                1 => 0,
                2 => 2,
                3 => 3,
                _ => 4,
            }
            : (day + 2 * block + channel) % LandingPages.Length;
    }

    internal readonly record struct AnalyticsTotals(int Sessions, int Engaged, int Purchases)
    {
        internal double EngagementRate => this.Sessions > 0 ? (double)this.Engaged / this.Sessions : 0;

        internal double PurchaseRate => this.Sessions > 0 ? (double)this.Purchases / this.Sessions : 0;

        public static AnalyticsTotals operator +(AnalyticsTotals left, AnalyticsTotals right) =>
            new(left.Sessions + right.Sessions, left.Engaged + right.Engaged, left.Purchases + right.Purchases);
    }

    internal sealed class AnalyticsPeriod(DateTimeOffset firstDay, int days)
    {
        internal DateTimeOffset FirstDay { get; } = firstDay;

        internal AnalyticsTotals Total { get; set; }

        internal AnalyticsTotals[] Daily { get; } = new AnalyticsTotals[days];

        internal AnalyticsTotals[] ByChannel { get; } = new AnalyticsTotals[Channels.Length];

        internal AnalyticsTotals[] ByDevice { get; } = new AnalyticsTotals[Devices.Length];

        internal AnalyticsTotals[] ByLandingPage { get; } = new AnalyticsTotals[LandingPages.Length];

        internal int[] DaysByWeekday { get; } = new int[7];

        internal int[,] SessionsByHourBlock { get; } = new int[7, MobileTraffic.Length];

        internal double[][] CheckoutByDevice { get; } = [new double[CheckoutSteps.Length], new double[CheckoutSteps.Length]];
    }
}
