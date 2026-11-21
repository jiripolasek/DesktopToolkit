// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>
/// Projects a monotonic elapsed-time source onto a stable UTC origin.
/// </summary>
/// <remarks>
/// The returned value remains comparable with caller-provided <see cref="DateTimeOffset"/>
/// timestamps while avoiding jumps when the system wall clock is corrected.
/// </remarks>
internal sealed class MonotonicClock
{
    private readonly TimeProvider _timeProvider;
    private readonly long _originTimestamp;
    private readonly DateTimeOffset _originUtc;

    public DateTimeOffset Now
    {
        get
        {
            var currentTimestamp = this._timeProvider.GetTimestamp();
            return this._originUtc + this._timeProvider.GetElapsedTime(this._originTimestamp, currentTimestamp);
        }
    }

    public MonotonicClock(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        this._timeProvider = timeProvider;
        this._originTimestamp = timeProvider.GetTimestamp();
        this._originUtc = timeProvider.GetUtcNow();
    }
}
