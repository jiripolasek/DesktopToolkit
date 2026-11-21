// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;

namespace JPSoftworks.DesktopToolkit.Charts;

internal sealed class LineChartOverviewPresenter
{
    private static readonly DependencyProperty[] PreviewProperties =
    [
        LineChart.DataProperty, LineChart.ModeProperty, ChartPalette.ColorsProperty,
    ];

    private readonly LineChartOverview _owner;
    private readonly Canvas _track;
    private readonly AccessibilitySettings _accessibility = new();
    private readonly UISettings _settings = new();
    private readonly long[] _tokens = new long[PreviewProperties.Length];
    private readonly Rectangle _before = new() { IsHitTestVisible = false };
    private readonly Rectangle _after = new() { IsHitTestVisible = false };
    private readonly Rectangle _window = new() { IsHitTestVisible = false, RadiusX = 3, RadiusY = 3 };
    private readonly Rectangle _startGrip = CreateGrip();
    private readonly Rectangle _endGrip = CreateGrip();
    private readonly LineChartPinchInput _pinch;
    private LineChart? _target;
    private Pointer? _capturedPointer;
    private LineChartSelectionEdit? _resize;
    private double? _hoverX;
    private double _dragOffset;
    private double _automationValue;
    private bool _automationReadOnly = true;

    public LineChartOverviewPresenter(LineChartOverview owner, Canvas track)
    {
        this._owner = owner;
        this._track = track;
        this._pinch = new LineChartPinchInput(track, this.GetTrackBounds, this.CancelDrag, owner.ZoomAt);
        track.PointerWheelChanged += this.OnWheel;
        track.SizeChanged += this.OnSizeChanged;
        track.PointerPressed += this.OnPointerPressed;
        track.PointerMoved += this.OnPointerMoved;
        track.PointerEntered += this.OnPointerMoved;
        track.PointerExited += this.OnPointerExited;
        track.PointerReleased += this.OnPointerEnded;
        track.PointerCaptureLost += this.OnPointerEnded;
        track.PointerCanceled += this.OnPointerEnded;
        owner.KeyDown += this.OnKeyDown;
    }

    public void Connect(LineChart? target)
    {
        if (this._target == target)
        {
            return;
        }

        this.CancelDrag();
        if (this._target is { } previous)
        {
            previous.ViewportChanged -= this.OnViewportChanged;
            previous.NavigationExtentChanged -= this.OnNavigationExtentChanged;
            previous.ActualThemeChanged -= this.OnTargetThemeChanged;
            previous.IsEnabledChanged -= this.OnTargetEnabledChanged;
            for (var index = 0; index < PreviewProperties.Length; index++)
            {
                previous.UnregisterPropertyChangedCallback(PreviewProperties[index], this._tokens[index]);
            }
        }

        this._target = target;
        if (target is not null)
        {
            target.ViewportChanged += this.OnViewportChanged;
            target.NavigationExtentChanged += this.OnNavigationExtentChanged;
            target.ActualThemeChanged += this.OnTargetThemeChanged;
            target.IsEnabledChanged += this.OnTargetEnabledChanged;
            for (var index = 0; index < PreviewProperties.Length; index++)
            {
                this._tokens[index] = target.RegisterPropertyChangedCallback(PreviewProperties[index], this.OnTargetPreviewChanged);
            }
        }

        this.Render();
    }

    public void Detach()
    {
        this.Connect(null);
        this._pinch.Detach();
        this._track.PointerWheelChanged -= this.OnWheel;
        this._track.SizeChanged -= this.OnSizeChanged;
        this._track.PointerPressed -= this.OnPointerPressed;
        this._track.PointerMoved -= this.OnPointerMoved;
        this._track.PointerEntered -= this.OnPointerMoved;
        this._track.PointerExited -= this.OnPointerExited;
        this._track.PointerReleased -= this.OnPointerEnded;
        this._track.PointerCaptureLost -= this.OnPointerEnded;
        this._track.PointerCanceled -= this.OnPointerEnded;
        this._owner.KeyDown -= this.OnKeyDown;
    }

    public void Render()
    {
        this._track.Children.Clear();
        var width = this._track.ActualWidth;
        var height = this._track.ActualHeight;
        this._track.Clip = new RectangleGeometry { Rect = new Rect(0, 0, width, height) };
        if (this._target is { } target)
        {
            var highContrast = this._accessibility.HighContrast;
            LineChartOverviewTrace.Draw(target, this._track, highContrast, this._settings);
            var shade = highContrast ? this._settings.UIElementColor(UIElementType.Window) :
                this._owner.ActualTheme == ElementTheme.Dark ? Color.FromArgb(255, 24, 24, 24) : Color.FromArgb(255, 248, 248, 248);
            this._before.Fill = this._after.Fill = new SolidColorBrush(shade);
            this._before.Opacity = this._after.Opacity = 0.52;
            this._track.Children.Add(this._before);
            this._track.Children.Add(this._after);
            this._track.Children.Add(this._startGrip);
            this._track.Children.Add(this._endGrip);
            this._track.Children.Add(this._window);
        }

        this.RefreshWindow();
    }

    public void RefreshWindow()
    {
        var (start, end) = this._target is null ? default : this._owner.GetWindow();
        var width = this._track.ActualWidth;
        var height = this._track.ActualHeight;
        var visible = end > start && width > 0 && height > 0;
        var canResize = visible && this._owner.CanResize;
        this._pinch.SetEnabled(canResize);
        this._before.Visibility = this._after.Visibility = this._window.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        this._startGrip.Visibility = this._endGrip.Visibility = canResize ? Visibility.Visible : Visibility.Collapsed;
        if (visible)
        {
            this._window.Stroke = this._owner.Foreground;
            this._window.StrokeThickness = canResize ? 1.5 : 1;
            this._window.Opacity = canResize ? 1 : 0.45;
            var left = start * width;
            var right = end * width;
            Place(this._before, 0, left, height);
            Place(this._after, right, width - right, height);
            Place(this._window, left, Math.Max(1, right - left), height);
            if (canResize)
            {
                var gripWidth = Math.Min(5, (right - left) / 2);
                var gripHeight = Math.Min(18, height / 2);
                PlaceGrip(this._startGrip, left);
                PlaceGrip(this._endGrip, right - gripWidth);

                void PlaceGrip(Rectangle grip, double x)
                {
                    grip.Fill = this._owner.Foreground;
                    Place(grip, x, gripWidth, gripHeight);
                    Canvas.SetTop(grip, (height - gripHeight) / 2);
                }
            }
        }

        this.RefreshCursor();
        if (FrameworkElementAutomationPeer.FromElement(this._owner) is { } peer && peer is IRangeValueProvider range)
        {
            var value = range.Value;
            var readOnly = range.IsReadOnly;
            if (this._automationValue != value)
            {
                var previous = this._automationValue;
                this._automationValue = value;
                peer.RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty, previous, value);
            }

            if (this._automationReadOnly != readOnly)
            {
                var previous = this._automationReadOnly;
                this._automationReadOnly = readOnly;
                peer.RaisePropertyChangedEvent(RangeValuePatternIdentifiers.IsReadOnlyProperty, previous, readOnly);
            }
        }
    }

    private static void Place(Rectangle rectangle, double left, double width, double height)
    {
        Canvas.SetLeft(rectangle, left);
        rectangle.Width = width;
        rectangle.Height = height;
    }

    private static Rectangle CreateGrip()
    {
        var grip = new Rectangle { IsHitTestVisible = false, RadiusX = 2, RadiusY = 2 };
        Canvas.SetZIndex(grip, 1);
        return grip;
    }

    private Rect GetTrackBounds()
    {
        return new(0, 0, this._track.ActualWidth, this._track.ActualHeight);
    }

    private Rect GetWindowBounds()
    {
        var (start, end) = this._owner.GetWindow();
        var width = this._track.ActualWidth;
        return new Rect(start * width, 0, (end - start) * width, this._track.ActualHeight);
    }

    private void RefreshCursor()
    {
        InputSystemCursorShape? cursor = null;
        if (this._target is not null && this._owner.CanResize)
        {
            var part = this._resize?.Part
                       ?? (this._capturedPointer is not null ? LineChartSelectionPart.Body :
                           this._hoverX is double x ? LineChartSelectionEdit.HitTest(this.GetWindowBounds(), x) :
                           LineChartSelectionPart.Plot);
            cursor = part switch
            {
                LineChartSelectionPart.StartEdge or LineChartSelectionPart.EndEdge => InputSystemCursorShape.SizeWestEast,
                LineChartSelectionPart.Body when this._owner.CanPan => InputSystemCursorShape.SizeAll,
                _ => null,
            };
        }

        this._owner.SetViewportCursor(cursor);
    }

    public void CancelDrag()
    {
        if (this._capturedPointer is { } pointer)
        {
            this._capturedPointer = null;
            this._resize = null;
            this._track.ReleasePointerCapture(pointer);
        }

        this.RefreshCursor();
    }

    private void OnWheel(object sender, PointerRoutedEventArgs e)
    {
        if (e.Handled || !this._owner.CanResize || this._capturedPointer is not null || (e.KeyModifiers & VirtualKeyModifiers.Control) == 0)
        {
            return;
        }

        var point = e.GetCurrentPoint(this._track);
        var bounds = this.GetTrackBounds();
        if (bounds.Width > 0 && bounds.Contains(point.Position) && !point.Properties.IsHorizontalMouseWheel)
        {
            var steps = Math.Clamp(point.Properties.MouseWheelDelta / 120d, -16, 16);
            this._owner.ZoomAt(Math.Pow(1.35, steps), point.Position.X / bounds.Width);
            e.Handled = true;
        }
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (e.Handled
            || this._capturedPointer is not null
            || this._pinch.IsPinching
            || !this._owner.CanResize
            || this._track.ActualWidth <= 0)
        {
            return;
        }

        var point = e.GetCurrentPoint(this._track);
        if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse
            && point.Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonPressed)
        {
            return;
        }

        var window = this.GetWindowBounds();
        var x = point.Position.X;
        var part = LineChartSelectionEdit.HitTest(window, x);
        var resize = part is LineChartSelectionPart.StartEdge or LineChartSelectionPart.EndEdge;
        if ((!resize && !this._owner.CanPan) || !this._track.CapturePointer(e.Pointer))
        {
            return;
        }

        this._capturedPointer = e.Pointer;
        if (resize)
        {
            this._resize = new LineChartSelectionEdit(part, x, window);
        }
        else
        {
            var fraction = x / this._track.ActualWidth;
            var (start, end) = this._owner.GetWindow();
            this._dragOffset = part == LineChartSelectionPart.Body ? fraction - start : (end - start) / 2;
            if (part == LineChartSelectionPart.Plot)
            {
                this._owner.PanTo(fraction - this._dragOffset);
            }
        }

        this.RefreshCursor();
        this._owner.Focus(FocusState.Pointer);
        e.Handled = e.Pointer.PointerDeviceType != PointerDeviceType.Touch;
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var x = e.GetCurrentPoint(this._track).Position.X;
        if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
        {
            this._hoverX = x;
            this.RefreshCursor();
        }

        if (this._capturedPointer?.PointerId == e.Pointer.PointerId && this._track.ActualWidth > 0 && !this._pinch.IsPinching)
        {
            if (this._resize is { } resize)
            {
                var bounds = this.GetTrackBounds();
                var (first, last) = resize.GetEdges(bounds, x);
                this._owner.SetWindow(first / bounds.Width, last / bounds.Width);
            }
            else
            {
                this._owner.PanTo(x / this._track.ActualWidth - this._dragOffset);
            }

            e.Handled = e.Pointer.PointerDeviceType != PointerDeviceType.Touch;
        }
    }

    private void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
        {
            this._hoverX = null;
            this.RefreshCursor();
        }
    }

    private void OnPointerEnded(object sender, PointerRoutedEventArgs e)
    {
        if (this._capturedPointer?.PointerId == e.Pointer.PointerId)
        {
            this.CancelDrag();
        }
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Handled || !this._owner.CanResize || this._owner.FocusState == FocusState.Unfocused)
        {
            return;
        }

        var (start, end) = this._owner.GetWindow();
        var width = end - start;
        if ((InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control) & CoreVirtualKeyStates.Down) != 0)
        {
            switch (e.Key)
            {
                case VirtualKey.Up: this._owner.ZoomAt(1.5, start + width / 2); break;
                case VirtualKey.Down: this._owner.ZoomAt(1 / 1.5, start + width / 2); break;
                case VirtualKey.Home: this._owner.SetWindow(0, 1); break;
                default: return;
            }

            e.Handled = true;
            return;
        }

        if (!this._owner.CanPan)
        {
            return;
        }

        var next = e.Key switch
        {
            VirtualKey.Left => start - width * LineChartOverview.SmallPanFraction,
            VirtualKey.Right => start + width * LineChartOverview.SmallPanFraction,
            VirtualKey.PageUp => start - width * LineChartOverview.LargePanFraction,
            VirtualKey.PageDown => start + width * LineChartOverview.LargePanFraction,
            VirtualKey.Home => 0,
            VirtualKey.End => 1 - width,
            _ => (double?)null,
        };
        if (next is { } position)
        {
            this._owner.PanTo(position);
            e.Handled = true;
        }
    }

    private void OnViewportChanged(object? sender, EventArgs e)
    {
        this.RefreshWindow();
    }

    private void OnNavigationExtentChanged(object? sender, EventArgs e)
    {
        this.CancelDrag();
        this.Render();
    }

    private void OnTargetThemeChanged(FrameworkElement sender, object e)
    {
        this.Render();
    }

    private void OnTargetEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        this.CancelDrag();
        this.RefreshWindow();
    }

    private void OnTargetPreviewChanged(DependencyObject sender, DependencyProperty property)
    {
        if (property == LineChart.DataProperty)
        {
            this.CancelDrag();
        }

        this.Render();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        this.CancelDrag();
        this.Render();
    }
}
