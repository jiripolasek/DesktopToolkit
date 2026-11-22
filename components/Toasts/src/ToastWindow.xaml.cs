// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Graphics;
using Windows.UI.ViewManagement;

namespace JPSoftworks.DesktopToolkit.ToastWindows;

/// <summary>Shows a transient acrylic message with an optional action.</summary>
public sealed partial class ToastWindow : TransparentWindow
{
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds(2.5);

    private readonly DispatcherQueueTimer _autoHideTimer;
    private readonly WindowId _placementWindowId;
    private readonly UISettings _uiSettings = new();

    private ToastAction? _action;
    private Storyboard? _animation;
    private bool _pointerOverSurface;
    private int _slideDirection;

    /// <summary>Gets or sets the position used the next time a toast is shown.</summary>
    public ToastPosition Position { get; set; } = ToastPosition.UseSystemSettings;

    /// <summary>Initializes a reusable toast window.</summary>
    public ToastWindow()
        : this(default)
    {
    }

    /// <summary>Initializes a toast window that follows the display of another window.</summary>
    /// <param name="placementWindowId">The window whose display work area is used for placement.</param>
    public ToastWindow(WindowId placementWindowId)
        : base(alwaysOnTop: true)
    {
        this._placementWindowId = placementWindowId;
        this.InitializeComponent();
        this.SurfaceBackdrop.SystemBackdrop = new AlwaysActiveDesktopAcrylicBackdrop { CloseExternalLinkOnDisconnect = true };
        this.AppWindow.Title = "Toast";

        this._autoHideTimer = this.DispatcherQueue.CreateTimer();
        this._autoHideTimer.IsRepeating = false;
        this._autoHideTimer.Tick += this.OnAutoHide;
        this.Closed += this.OnClosed;
    }

    /// <summary>Shows or replaces the toast without taking foreground focus.</summary>
    /// <param name="message">The text shown in the toast.</param>
    /// <param name="icon">An optional message icon.</param>
    /// <param name="action">An optional action button.</param>
    /// <param name="duration">The time to show the toast, defaulting to 2.5 seconds.</param>
    /// <remarks>Call on the window's UI thread. Hovering pauses dismissal.</remarks>
    public void ShowToast(string message, IconSource? icon = null, ToastAction? action = null, TimeSpan? duration = null)
    {
        ArgumentNullException.ThrowIfNull(message);
        var visibleDuration = duration ?? DefaultDuration;
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(visibleDuration, TimeSpan.Zero, nameof(duration));

        this._autoHideTimer.Stop();
        this.StopAnimation();

        this._action = action;
        this.ToastMessage.Text = message;
        UpdateIcon(this.ToastIconHost, icon);
        this.ActionLabel.Text = action?.Label ?? string.Empty;
        UpdateIcon(this.ActionIconHost, action?.Icon);
        this.ActionButton.Visibility = action is null ? Visibility.Collapsed : Visibility.Visible;
        AutomationProperties.SetName(this.ActionButton, this.ActionLabel.Text);

        this.PositionWindow();
        if (!this.AppWindow.IsVisible)
        {
            this.Show();
            this.Animate(show: true);
        }

        this._autoHideTimer.Interval = visibleDuration;
        if (!this._pointerOverSurface)
        {
            this._autoHideTimer.Start();
        }

        FrameworkElementAutomationPeer.FromElement(this.ToastMessage)?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
    }

    /// <summary>Dismisses the current toast.</summary>
    public void Dismiss()
    {
        this._autoHideTimer.Stop();
        if (this.AppWindow.IsVisible)
        {
            this.StopAnimation();
            this.Animate(show: false);
        }
    }

    private static void UpdateIcon(Viewbox host, IconSource? source)
    {
        // WinUI can reject clearing IconSource on a populated element.
        host.Child = source is null ? null : new IconSourceElement { IconSource = source };
        host.Visibility = source is null ? Visibility.Collapsed : Visibility.Visible;
    }

    private void PositionWindow()
    {
        var windowId = this._placementWindowId.Value == 0 ? this.AppWindow.Id : this._placementWindowId;
        var display = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest);
        if (display is null)
        {
            return;
        }

        var workArea = display.WorkArea;
        var hwnd = Win32Interop.GetWindowFromWindowId(windowId);
        var dpi = GetDpiForWindow(hwnd);
        var scale = dpi == 0 ? 1.0 : dpi / 96.0;
        var width = Math.Min(workArea.Width, (int)Math.Round(600 * scale));
        var height = Math.Min(workArea.Height, (int)Math.Round(180 * scale));
        var centeredX = workArea.X + (workArea.Width - width) / 2;
        var bottomY = workArea.Y + workArea.Height - height;

        var position = ToastPositionSettings.Resolve(this.Position);
        var top = position is ToastPosition.TopLeft or ToastPosition.TopCenter;
        var left = position == ToastPosition.TopLeft;
        var x = left ? workArea.X : centeredX;
        var y = top ? workArea.Y : bottomY;

        this.Surface.HorizontalAlignment = left ? HorizontalAlignment.Left : HorizontalAlignment.Center;
        this.Surface.VerticalAlignment = top ? VerticalAlignment.Top : VerticalAlignment.Bottom;
        this.Surface.Margin = new Thickness(left ? 16 : 24, top ? 16 : 24, 24, top ? 24 : 16);
        this._slideDirection = top ? -1 : 1;

        this.AppWindow.MoveAndResize(new RectInt32(x, y, width, height));
        this.ApplyTransparentChrome();
    }

    private void Animate(bool show)
    {
        if (!this._uiSettings.AnimationsEnabled)
        {
            if (show)
            {
                this.Surface.Opacity = 1;
            }
            else
            {
                this.CompleteDismissal();
            }

            return;
        }

        var duration = TimeSpan.FromMilliseconds(show ? 180 : 120);
        var fade = new DoubleAnimation { From = show ? 0 : 1, To = show ? 1 : 0, Duration = duration };
        var slide = new DoubleAnimation
        {
            From = show ? this._slideDirection * 24 : 0,
            To = show ? 0 : this._slideDirection * 12,
            Duration = duration,
            EasingFunction = new CubicEase { EasingMode = show ? EasingMode.EaseOut : EasingMode.EaseIn },
        };
        Storyboard.SetTarget(fade, this.Surface);
        Storyboard.SetTargetProperty(fade, nameof(UIElement.Opacity));
        Storyboard.SetTarget(slide, this.Slide);
        Storyboard.SetTargetProperty(slide, nameof(TranslateTransform.Y));

        this._animation = new Storyboard();
        this._animation.Children.Add(fade);
        this._animation.Children.Add(slide);
        if (!show)
        {
            this._animation.Completed += this.OnDismissAnimationCompleted;
        }

        this._animation.Begin();
    }

    private void StopAnimation()
    {
        if (this._animation is { } animation)
        {
            animation.Completed -= this.OnDismissAnimationCompleted;
            animation.Stop();
            this._animation = null;
        }

        this.Surface.Opacity = 1;
    }

    private void OnDismissAnimationCompleted(object? sender, object args)
    {
        this.CompleteDismissal();
    }

    private void CompleteDismissal()
    {
        this.Hide();
        this.StopAnimation();
        this._pointerOverSurface = false;
        this._action = null;
    }

    private void OnPointerEntered(object sender, PointerRoutedEventArgs args)
    {
        this._pointerOverSurface = true;
        this._autoHideTimer.Stop();
    }

    private void OnPointerExited(object sender, PointerRoutedEventArgs args)
    {
        this._pointerOverSurface = false;
        if (this.AppWindow.IsVisible)
        {
            this._autoHideTimer.Start();
        }
    }

    private void OnAutoHide(DispatcherQueueTimer sender, object args)
    {
        this.Dismiss();
    }

    private void OnActionClick(object sender, RoutedEventArgs args)
    {
        var action = this._action;
        this._action = null;
        this.Dismiss();
        action?.Invoke();
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        this._autoHideTimer.Stop();
        this.StopAnimation();
        this._action = null;
    }

    [LibraryImport("user32.dll")]
    private static partial uint GetDpiForWindow(nint hwnd);
}
