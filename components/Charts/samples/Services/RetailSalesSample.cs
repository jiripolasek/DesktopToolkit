// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Controls.Demo.Services;

internal static class RetailSalesSample
{
    public static IReadOnlyList<string> Regions { get; } = Array.AsReadOnly<string>(["North America", "Europe", "Asia Pacific"]);

    public static IReadOnlyList<string> Channels { get; } = Array.AsReadOnly<string>(["Web", "Marketplace", "Retail"]);

    public static DateTimeOffset Month(int index)
    {
        return new(2025, index + 1, 1, 0, 0, 0, TimeSpan.Zero);
    }

    public static MonthlySales[] Create()
    {
        double[] season = [0.88, 0.89, 0.97, 1.02, 1.06, 1.04, 0.96, 0.98, 1.09, 1.16, 1.42, 1.58];
        int[] regionVolume = [2450, 1720, 1370];
        double[] channelShare = [0.44, 0.32, 0.24];
        int[] channelOrderValue = [106, 82, 128];
        var data = new List<MonthlySales>();
        for (var month = 0; month < season.Length; month++)
        {
            for (var region = 0; region < Regions.Count; region++)
            {
                for (var channel = 0; channel < Channels.Count; channel++)
                {
                    if (region == 2 && channel == 2 && month < 3)
                    {
                        continue;
                    }

                    var plannedOrders = regionVolume[region] * channelShare[channel] * season[month];
                    var performance = 1.01 + 0.15 * Math.Sin((month - region * 2 + channel * 0.7) * 0.65);
                    var campaign = region == 0 && channel == 1 && month is 7 or 8;
                    var orders = (int)Math.Round(plannedOrders * performance * (campaign ? 1.24 : 1));
                    var rate = 0.027 + channel * 0.015 + 0.014 * Math.Cos(month * 0.53 + region) + (campaign ? 0.079 : 0);
                    var returned = (int)Math.Round(orders * rate);
                    var orderValue = channelOrderValue[channel];
                    var net = (orders - returned) * orderValue / 1000d;
                    var plan = plannedOrders * orderValue * 0.95 / 1000;
                    data.Add(new MonthlySales(month, region, channel, orders, returned, net, plan));
                }
            }
        }

        return data.ToArray();
    }
}

internal readonly record struct MonthlySales(
    int Month,
    int Region,
    int Channel,
    int Orders,
    int ReturnedOrders,
    double NetRevenue,
    double Plan);
