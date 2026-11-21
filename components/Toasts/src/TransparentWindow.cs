// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace JPSoftworks.DesktopToolkit.ToastWindows;

/// <summary>Hosts transient WinUI content in a transparent, borderless window.</summary>
public partial class TransparentWindow : Window
{
    /// <summary>Initializes a transparent window that is hidden from shell switchers.</summary>
    public TransparentWindow()
        : this(alwaysOnTop: false)
    {
    }

    /// <summary>Initializes the transparent window with an optional topmost presenter.</summary>
    /// <param name="alwaysOnTop">Whether the presenter stays above other windows.</param>
    protected TransparentWindow(bool alwaysOnTop)
    {
        this.ExtendsContentIntoTitleBar = true;
        this.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;
        this.AppWindow.IsShownInSwitchers = false;

        var presenter = OverlappedPresenter.Create();
        presenter.SetBorderAndTitleBar(false, false);
        presenter.IsResizable = false;
        presenter.IsMinimizable = false;
        presenter.IsMaximizable = false;
        presenter.IsAlwaysOnTop = alwaysOnTop;
        this.AppWindow.SetPresenter(presenter);
        this.ApplyTransparentChrome();

        this.SystemBackdrop = new TransparentWindowBackdrop();
    }

    /// <summary>Shows the window without activating it.</summary>
    public void Show()
    {
        this.AppWindow.Show(activateWindow: false);
    }

    /// <summary>Hides the window so it can be shown again.</summary>
    public void Hide()
    {
        this.AppWindow.Hide();
    }

    /// <summary>Reapplies the transparent frame after a change such as a cross-monitor move.</summary>
    protected void ApplyTransparentChrome()
    {
        const uint windowCornerPreference = 33;
        const uint borderColor = 34;
        const uint doNotRound = 1;
        const uint noColor = 0xFFFFFFFE;

        var handle = WinRT.Interop.WindowNative.GetWindowHandle(this);
        _ = DwmSetWindowAttribute(handle, windowCornerPreference, doNotRound, sizeof(uint));
        _ = DwmSetWindowAttribute(handle, borderColor, noColor, sizeof(uint));
    }

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmSetWindowAttribute(nint hwnd, uint attribute, in uint value, uint size);
}
