// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Dispatching;

namespace JPSoftworks.DesktopToolkit.Charts;

internal sealed class ChartValueDispatcher(DispatcherQueue dispatcher, Action<double[]> apply)
{
    private readonly object _sync = new();
    private (double[] Values, long Version)? _pending;
    private long _version;
    private bool _queued;

    public long Version => Volatile.Read(ref this._version);

    public void Reset()
    {
        lock (this._sync)
        {
            Interlocked.Increment(ref this._version);
            this._pending = null;
        }
    }

    public void Submit(double[] values, long version)
    {
        if (dispatcher.HasThreadAccess)
        {
            lock (this._sync)
            {
                if (version != this._version)
                {
                    return;
                }

                this._pending = null;
            }

            apply(values);
            return;
        }

        lock (this._sync)
        {
            if (version != this._version)
            {
                return;
            }

            this._pending = (values, version);
            if (this._queued)
            {
                return;
            }

            this._queued = dispatcher.TryEnqueue(this.ApplyPending);
            if (!this._queued)
            {
                this._pending = null;
                throw new InvalidOperationException("The chart is no longer connected to a UI dispatcher.");
            }
        }
    }

    private void ApplyPending()
    {
        (double[] Values, long Version)? update;
        lock (this._sync)
        {
            update = this._pending;
            this._pending = null;
            this._queued = false;
        }

        if (update is { } pending && pending.Version == this.Version)
        {
            apply(pending.Values);
        }
    }
}
