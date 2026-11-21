// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Diagnostics;

namespace JPSoftworks.DesktopToolkit.Controls.Demo.Services;

internal static class MemoryAnalysisSample
{
    public const double MiB = 1024d * 1024;

    public static IReadOnlyList<string> Generations { get; } = Array.AsReadOnly<string>(["Gen 0", "Gen 1", "Gen 2", "LOH", "POH"]);

    public static MemoryFrame[] Replay()
    {
        var start = new DateTimeOffset(2026, 4, 8, 14, 22, 0, TimeSpan.Zero);
        var frames = new MemoryFrame[49];
        for (var index = 0; index < frames.Length; index++)
        {
            var import = Math.Clamp((index - 7) / 22d, 0, 1);
            var cleanup = Math.Clamp((index - 29) / 8d, 0, 1);
            var loh = 8 + 88 * import - 71 * cleanup;
            var metadata = 2 + 9 * import - 4 * cleanup;
            var thumbnails = 3 + 56 * import - 50 * cleanup;
            var previews = 2 + 23 * import - 19 * cleanup;
            var gen0 = 2 + (index % 5) * (index is > 7 and < 29 ? 2.9 : 0.9);
            var gen1 = 4 + 14 * import - 13 * cleanup + 2 * Math.Sin(index * 0.5);
            var gen2 = 25 + 36 * import - 7 * cleanup;
            frames[index] = new MemoryFrame(
                start.AddSeconds(index * 2), [gen0, gen1, gen2, loh, 2 + 1.5 * import], thumbnails, previews, metadata);
        }

        return frames;
    }

    public static async Task<MemoryFrame[]> CaptureAsync(CancellationToken cancellationToken)
    {
        const int thumbnailBytes = 192 * 1024;
        const int previewBytes = 384 * 1024;
        const int metadataBytes = 8 * 1024;
        var thumbnails = new List<byte[]>();
        var previews = new List<byte[]>();
        var metadata = new List<byte[]>();
        var frames = new MemoryFrame[13];
        var startTime = DateTimeOffset.UtcNow;
        var startClock = Stopwatch.GetTimestamp();
        for (var index = 0; index < frames.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (index is >= 2 and <= 7)
            {
                for (var image = 0; image < 8; image++)
                {
                    thumbnails.Add(new byte[thumbnailBytes]);
                    previews.Add(new byte[previewBytes]);
                }

                for (var item = 0; item < 96; item++)
                {
                    metadata.Add(new byte[metadataBytes]);
                }
            }

            if (index is >= 8 and <= 10)
            {
                thumbnails.RemoveRange(0, 12);
                previews.RemoveRange(0, 14);
                metadata.RemoveRange(0, 144);
            }

            // Snapshot the actual managed heap at a known collection boundary.
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: false);
            frames[index] = new MemoryFrame(
                startTime + Stopwatch.GetElapsedTime(startClock), ReadGenerations(), thumbnails.Count * thumbnailBytes / MiB,
                previews.Count * previewBytes / MiB, metadata.Count * metadataBytes / MiB);
            if (index < frames.Length - 1)
            {
                await Task.Delay(200, cancellationToken).ConfigureAwait(false);
            }
        }

        return frames;
    }

    private static double[] ReadGenerations()
    {
        var info = GC.GetGCMemoryInfo(GCKind.FullBlocking).GenerationInfo;
        var sizes = new double[Generations.Count];
        for (var index = 0; index < Math.Min(sizes.Length, info.Length); index++)
        {
            sizes[index] = Math.Max(0, info[index].SizeAfterBytes - info[index].FragmentationAfterBytes) / MiB;
        }

        return sizes;
    }
}

internal sealed record MemoryFrame(DateTimeOffset Timestamp, double[] HeapSizes, double Thumbnails, double Previews, double Metadata)
{
    public double HeapTotal => this.HeapSizes.Sum();
}
