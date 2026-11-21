// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Diagnostics;
using System.Net.NetworkInformation;

namespace JPSoftworks.DesktopToolkit.Controls.Demo.Services;

internal sealed record NetworkCounterSample(long Timestamp, IReadOnlyDictionary<string, AdapterCounters> Adapters)
{
    public static NetworkCounterSample Read()
    {
        var counters = new Dictionary<string, AdapterCounters>(StringComparer.Ordinal);
        try
        {
            foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (adapter.OperationalStatus != OperationalStatus.Up
                    || adapter.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
                {
                    continue;
                }

                try
                {
                    var statistics = adapter.GetIPStatistics();
                    counters[adapter.Id] = new AdapterCounters(statistics.BytesReceived, statistics.BytesSent);
                }
                catch (NetworkInformationException)
                {
                    // The adapter may have disconnected after enumeration.
                }
            }
        }
        catch (NetworkInformationException)
        {
            // Leave the sample empty when counters are unavailable.
        }

        return new NetworkCounterSample(Stopwatch.GetTimestamp(), counters);
    }

    public NetworkRates? GetRatesSince(NetworkCounterSample? previous)
    {
        if (previous is null || this.Timestamp <= previous.Timestamp)
        {
            return null;
        }

        var seconds = Stopwatch.GetElapsedTime(previous.Timestamp, this.Timestamp).TotalSeconds;
        var received = 0d;
        var sent = 0d;
        var matched = false;
        foreach (var (id, current) in this.Adapters)
        {
            if (previous.Adapters.TryGetValue(id, out var baseline)
                && baseline.Received >= 0
                && baseline.Sent >= 0
                && current.Received >= baseline.Received
                && current.Sent >= baseline.Sent)
            {
                received += current.Received - baseline.Received;
                sent += current.Sent - baseline.Sent;
                matched = true;
            }
        }

        return matched ? new NetworkRates(received / seconds / 1024, sent / seconds / 1024) : null;
    }
}

internal readonly record struct AdapterCounters(long Received, long Sent);

internal readonly record struct NetworkRates(double Received, double Sent);
