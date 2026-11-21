// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;

namespace JPSoftworks.DesktopToolkit.Charts;

internal sealed class LineChartViewportInput
{
    private readonly LineChart _owner;
    private readonly UIElement _plot;
    private readonly Func<Rect> _bounds;
    private readonly Action _clearInspection;
    private readonly Func<double, double, LineChartViewport?> _getRangeBetween;
    private readonly Func<Rect?> _selectionBounds;
    private readonly Action<LineChartViewport?> _showSelectionPreview;
    private readonly LineChartPinchInput _pinch;
    private Pointer? _activePointer;
    private LineChartSelectionEdit? _selectionEdit;
    private bool _hasDragged;
    private double _panX;
    private Point? _hover;

    public bool IsInteracting => this._activePointer is not null || this._pinch.IsPinching;

    private bool CanNavigate => this._owner.IsEnabled && this._owner.IsViewportInteractionEnabled && this._owner.Data?.Extent is not null;

    public LineChartViewportInput(
        LineChart owner,
        UIElement plot,
        Func<Rect> bounds,
        Action clearInspection,
        Func<double, double, LineChartViewport?> getRangeBetween,
        Func<Rect?> selectionBounds,
        Action<LineChartViewport?> showSelectionPreview)
    {
        this._owner = owner;
        this._plot = plot;
        this._bounds = bounds;
        this._clearInspection = clearInspection;
        this._getRangeBetween = getRangeBetween;
        this._selectionBounds = selectionBounds;
        this._showSelectionPreview = showSelectionPreview;
        this._pinch = new LineChartPinchInput(plot, bounds, clearInspection, (factor, anchor) => owner.ZoomBy(factor, anchor));
        plot.PointerWheelChanged += this.OnWheel;
        plot.PointerPressed += this.OnPointerPressed;
        plot.PointerEntered += this.OnPointerMoved;
        plot.PointerMoved += this.OnPointerMoved;
        plot.PointerExited += this.OnPointerExited;
        plot.PointerReleased += this.OnPointerReleased;
        plot.PointerCaptureLost += this.OnPointerCanceled;
        plot.PointerCanceled += this.OnPointerCanceled;
        owner.KeyDown += this.OnKeyDown;
        owner.Loaded += this.OnLoaded;
        owner.Unloaded += this.OnUnloaded;
        owner.IsEnabledChanged += this.OnEnabledChanged;
    }

    public void CancelSelectionDrag()
    {
        if (this._selectionEdit.HasValue)
        {
            this.CancelGesture();
        }
    }

    public void CancelGesture()
    {
        var pointer = this._activePointer;
        this._activePointer = null;
        if (this._selectionEdit.HasValue)
        {
            this._selectionEdit = null;
            this._showSelectionPreview(null);
        }

        if (pointer is not null)
        {
            this._plot.ReleasePointerCapture(pointer);
        }

        this.RefreshCursor();
    }

    public void Detach()
    {
        this.LeavePlot();
        this._pinch.Detach();
        this._plot.PointerWheelChanged -= this.OnWheel;
        this._plot.PointerPressed -= this.OnPointerPressed;
        this._plot.PointerEntered -= this.OnPointerMoved;
        this._plot.PointerMoved -= this.OnPointerMoved;
        this._plot.PointerExited -= this.OnPointerExited;
        this._plot.PointerReleased -= this.OnPointerReleased;
        this._plot.PointerCaptureLost -= this.OnPointerCanceled;
        this._plot.PointerCanceled -= this.OnPointerCanceled;
        this._owner.KeyDown -= this.OnKeyDown;
        this._owner.Loaded -= this.OnLoaded;
        this._owner.Unloaded -= this.OnUnloaded;
        this._owner.IsEnabledChanged -= this.OnEnabledChanged;
    }

    public void RefreshCursor()
    {
        this._pinch.SetEnabled(this.CanNavigate && this._owner.IsZoomEnabled && this._owner.IsLoaded);
        InputSystemCursorShape? cursor = null;
        if (this.CanNavigate)
        {
            if (this._activePointer is not null)
            {
                cursor = this._selectionEdit is { } edit ? GetCursor(edit.Part) : InputSystemCursorShape.SizeAll;
            }
            else if (this._owner.IsSelectionEnabled
                     && this._hover is { } hover
                     && this._bounds() is { Width: > 0 } bounds
                     && bounds.Contains(hover))
            {
                cursor = GetCursor(LineChartSelectionEdit.HitTest(this._selectionBounds(), hover.X));
            }
        }

        this._owner.SetViewportCursor(cursor);
    }

    private static InputSystemCursorShape GetCursor(LineChartSelectionPart part)
    {
        return part switch
        {
            LineChartSelectionPart.StartEdge or LineChartSelectionPart.EndEdge => InputSystemCursorShape.SizeWestEast,
            LineChartSelectionPart.Body => InputSystemCursorShape.SizeAll,
            _ => InputSystemCursorShape.Cross,
        };
    }

    private void OnWheel(object sender, PointerRoutedEventArgs e)
    {
        if (e.Handled || !this.CanNavigate || this.IsInteracting)
        {
            return;
        }

        var point = e.GetCurrentPoint(this._plot);
        var bounds = this._bounds();
        if (bounds.Width <= 0 || !bounds.Contains(point.Position))
        {
            return;
        }

        var steps = Math.Clamp(point.Properties.MouseWheelDelta / 120d, -16, 16);
        if ((e.KeyModifiers & VirtualKeyModifiers.Control) != 0 && !point.Properties.IsHorizontalMouseWheel)
        {
            if (this._owner.IsZoomEnabled)
            {
                var anchor = Math.Clamp((point.Position.X - bounds.Left) / bounds.Width, 0, 1);
                this._owner.ZoomBy(Math.Pow(1.35, steps), anchor);
                e.Handled = true;
            }
        }
        else if (point.Properties.IsHorizontalMouseWheel || (e.KeyModifiers & VirtualKeyModifiers.Shift) != 0)
        {
            var fraction = (point.Properties.IsHorizontalMouseWheel ? steps : -steps) * 0.15;
            e.Handled = this._owner.PanBy(fraction);
        }
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (e.Handled || !this.CanNavigate || this.IsInteracting || e.Pointer.PointerDeviceType != PointerDeviceType.Mouse)
        {
            return;
        }

        var point = e.GetCurrentPoint(this._plot);
        var bounds = this._bounds();
        var kind = point.Properties.PointerUpdateKind;
        var selects = this._owner.IsSelectionEnabled && kind == PointerUpdateKind.LeftButtonPressed;
        var pans = kind == PointerUpdateKind.MiddleButtonPressed && this._owner.VisibleViewport is not null;
        if ((selects || pans) && bounds.Width > 0 && bounds.Contains(point.Position) && this._plot.CapturePointer(e.Pointer))
        {
            this._activePointer = e.Pointer;
            this._hasDragged = false;
            this._hover = point.Position;
            if (selects)
            {
                var range = this._selectionBounds();
                this._selectionEdit = new LineChartSelectionEdit(
                    LineChartSelectionEdit.HitTest(range, point.Position.X), point.Position.X, range.GetValueOrDefault());
            }
            else
            {
                this._panX = point.Position.X;
                this._clearInspection();
            }

            this.RefreshCursor();
            this._owner.Focus(FocusState.Pointer);
            e.Handled = true;
        }
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (e.Pointer.PointerDeviceType != PointerDeviceType.Mouse || !this.CanNavigate)
        {
            return;
        }

        var point = e.GetCurrentPoint(this._plot).Position;
        this._hover = point;
        if (this._activePointer?.PointerId != e.Pointer.PointerId)
        {
            this.RefreshCursor();
            return;
        }

        if (this._selectionEdit is { } edit)
        {
            if (!this._hasDragged && IsDrag(edit, point.X))
            {
                this._hasDragged = true;
                this._clearInspection();
            }

            if (this._hasDragged)
            {
                this._showSelectionPreview(this.GetEditedRange(edit, point.X));
            }
        }
        else if (this._bounds().Width is > 0 and var width)
        {
            this._owner.PanBy((this._panX - point.X) / width);
            this._panX = point.X;
        }

        e.Handled = true;
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (this._activePointer?.PointerId != e.Pointer.PointerId)
        {
            return;
        }

        var point = e.GetCurrentPoint(this._plot);
        var edit = this._selectionEdit;
        var release = edit.HasValue ? PointerUpdateKind.LeftButtonReleased : PointerUpdateKind.MiddleButtonReleased;
        if (point.Properties.PointerUpdateKind != release)
        {
            return;
        }

        var position = point.Position;
        var dragged = this._hasDragged;
        this._hover = position;
        this.CancelGesture();
        if (edit is { } selection)
        {
            if (dragged || IsDrag(selection, position.X))
            {
                this._clearInspection();
                var range = selection.Part != LineChartSelectionPart.Plot || IsDrag(selection, position.X)
                    ? this.GetEditedRange(selection, position.X)
                    : null;
                this._owner.SetSelectedRange(range ?? (selection.Part == LineChartSelectionPart.Plot ? null : this._owner.SelectedRange));
            }
            else if (this._owner.IsZoomEnabled && selection.Part == LineChartSelectionPart.Body && selection.Range.Contains(position))
            {
                this._owner.ZoomToSelection();
            }
            else if (selection.Part == LineChartSelectionPart.Plot)
            {
                this._owner.ClearSelection();
            }
        }

        e.Handled = true;
    }

    private static bool IsDrag(LineChartSelectionEdit edit, double x)
    {
        return Math.Abs(x - edit.Anchor) >= LineChartSelectionEdit.DragThreshold;
    }

    private LineChartViewport? GetEditedRange(LineChartSelectionEdit edit, double x)
    {
        var (first, last) = edit.GetEdges(this._bounds(), x);
        var edited = this._getRangeBetween(first, last);
        if (edited is null
            || this._owner.SelectedRange is not { } original
            || edit.Part is not (LineChartSelectionPart.StartEdge or LineChartSelectionPart.EndEdge))
        {
            return edited;
        }

        // Keep the stationary edge in data coordinates to avoid projection rounding.
        var start = edit.Part == LineChartSelectionPart.StartEdge ? edited : original;
        var end = edit.Part == LineChartSelectionPart.EndEdge ? edited : original;
        if (edited.IsTimeBased)
        {
            return start.Start < end.End ? new LineChartViewport(start.Start!.Value, end.End!.Value) : null;
        }

        return start.Minimum < end.Maximum ? new LineChartViewport(start.Minimum!.Value, end.Maximum!.Value) : null;
    }

    private void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
        {
            this._hover = null;
            this.RefreshCursor();
        }
    }

    private void OnPointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        if (this._activePointer?.PointerId == e.Pointer.PointerId)
        {
            this.CancelGesture();
        }
    }

    private void LeavePlot()
    {
        this._hover = null;
        this.CancelGesture();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        this.RefreshCursor();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        this.LeavePlot();
        this._pinch.SetEnabled(false);
    }

    private void OnEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        this.CancelGesture();
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Handled || !this.CanNavigate || this._owner.FocusState == FocusState.Unfocused)
        {
            return;
        }

        if (e.Key == VirtualKey.Escape && (this._selectionEdit.HasValue || this._owner.SelectedRange is not null))
        {
            if (this._selectionEdit.HasValue)
            {
                this.CancelGesture();
            }
            else
            {
                this._owner.ClearSelection();
            }

            e.Handled = true;
            return;
        }

        if (!this.IsInteracting && this._owner.IsZoomEnabled && e.Key == VirtualKey.Enter && this._owner.ZoomToSelection())
        {
            e.Handled = true;
            return;
        }

        if (this.IsInteracting || (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control) & CoreVirtualKeyStates.Down) == 0)
        {
            return;
        }

        switch (e.Key)
        {
            case VirtualKey.Up when this._owner.IsZoomEnabled: this._owner.ZoomBy(1.5); break;
            case VirtualKey.Down when this._owner.IsZoomEnabled: this._owner.ZoomBy(1 / 1.5); break;
            case VirtualKey.Left: this._owner.PanBy(-0.2); break;
            case VirtualKey.Right: this._owner.PanBy(0.2); break;
            case VirtualKey.Home: this._owner.ResetViewport(); break;
            default: return;
        }

        e.Handled = true;
    }
}
