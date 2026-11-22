// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Diagnostics;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace JPSoftworks.DesktopToolkit.Charts;

internal sealed class ChartInspectorMotion
{
    private const double EntranceOffset = 6;
    private readonly FrameworkElement _owner;
    private readonly UISettings _settings = new();
    private readonly DispatcherQueueTimer _timer;
    private readonly Action<Point> _applyPosition;
    private Point? _current;
    private Point _from;
    private Point _target;
    private long _startedAt;
    private TimeSpan _duration;

    public ChartInspectorMotion(FrameworkElement owner, Action<Point> applyPosition)
    {
        this._owner = owner;
        this._applyPosition = applyPosition;
        this._timer = owner.DispatcherQueue.CreateTimer();
        this._timer.Interval = TimeSpan.FromMilliseconds(16);
        this._timer.IsRepeating = true;
        this._timer.Tick += this.OnTick;
    }

    public void MoveTo(Point destination, Rect limits, bool opensRight)
    {
        var target = Clamp(destination, limits);
        var duration = ChartInspection.GetTransitionDuration(this._owner);
        if (duration <= TimeSpan.Zero || !this._settings.AnimationsEnabled)
        {
            this._target = target;
            this.Finish();
            return;
        }

        var start = Clamp(this._current ?? new Point(target.X + (opensRight ? -EntranceOffset : EntranceOffset), target.Y), limits);
        if (this._timer.IsRunning && target == this._target && start == this._current)
        {
            return;
        }

        this._target = target;
        if (Math.Abs(start.X - target.X) < 0.5 && Math.Abs(start.Y - target.Y) < 0.5)
        {
            this.Finish();
            return;
        }

        this.SetPosition(start);
        this._from = start;
        this._duration = duration;
        this._startedAt = Stopwatch.GetTimestamp();
        if (!this._timer.IsRunning)
        {
            this._timer.Start();
        }
    }

    public void Reset()
    {
        this._timer.Stop();
        this._current = null;
    }

    private void OnTick(DispatcherQueueTimer sender, object args)
    {
        if (!sender.IsRunning)
        {
            return;
        }

        var remaining = 1 - Stopwatch.GetElapsedTime(this._startedAt) / this._duration;
        if (remaining <= 0 || ChartInspection.GetTransitionDuration(this._owner) <= TimeSpan.Zero || !this._settings.AnimationsEnabled)
        {
            this.Finish();
            return;
        }

        var weight = remaining * remaining * remaining;
        this.SetPosition(
            new Point(
                this._target.X + (this._from.X - this._target.X) * weight, this._target.Y + (this._from.Y - this._target.Y) * weight));
    }

    private void Finish()
    {
        this._timer.Stop();
        this.SetPosition(this._target);
    }

    private void SetPosition(Point point)
    {
        if (this._current != point)
        {
            this._current = point;
            this._applyPosition(point);
        }
    }

    private static Point Clamp(Point point, Rect limits)
    {
        return new(Math.Clamp(point.X, limits.Left, limits.Right), Math.Clamp(point.Y, limits.Top, limits.Bottom));
    }
}
