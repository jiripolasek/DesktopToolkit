// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32.SafeHandles;
using CompositionColorBrush = Windows.UI.Composition.CompositionColorBrush;
using Compositor = Windows.UI.Composition.Compositor;

namespace JPSoftworks.DesktopToolkit.ToastWindows;

internal sealed partial class TransparentWindowBackdrop : SystemBackdrop
{
    private const uint BlurEnable = 0x1;
    private const uint BlurRegion = 0x2;
    private const uint EraseBackground = 0x0014;
    private const uint DestroyNonClient = 0x0082;
    private const uint DwmCompositionChanged = 0x031E;
    private const int BlackBrush = 4;

    private nint _window;
    private Compositor? _compositor;
    private CompositionColorBrush? _brush;

    protected override unsafe void OnTargetConnected(ICompositionSupportsSystemBackdrop target, XamlRoot xamlRoot)
    {
        this.DispatcherQueue.EnsureSystemDispatcherQueue();
        this._compositor = new Compositor();
        this._brush = this._compositor.CreateColorBrush(Colors.Transparent);
        this._window = Win32Interop.GetWindowFromWindowId(xamlRoot.ContentIslandEnvironment.AppWindowId);
        if (this._window != 0)
        {
            _ = SetWindowSubclass(this._window, &WindowProcedure, 0, 0);
            EnableTransparency(this._window);
        }

        target.SystemBackdrop = this._brush;
        base.OnTargetConnected(target, xamlRoot);

        var dc = this._window == 0 ? 0 : GetDC(this._window);
        if (dc != 0)
        {
            try
            {
                _ = ClearBackground(this._window, dc);
            }
            finally
            {
                _ = ReleaseDC(this._window, dc);
            }
        }
    }

    protected override unsafe void OnTargetDisconnected(ICompositionSupportsSystemBackdrop target)
    {
        if (this._window != 0)
        {
            _ = RemoveWindowSubclass(this._window, &WindowProcedure, 0);
            _ = DwmEnableBlurBehindWindow(this._window, new BlurBehind { Flags = BlurEnable });
            this._window = 0;
        }

        target.SystemBackdrop = null;
        this._brush?.Dispose();
        this._brush = null;
        this._compositor?.Dispose();
        this._compositor = null;
        base.OnTargetDisconnected(target);
    }

    private static void EnableTransparency(nint window)
    {
        _ = DwmExtendFrameIntoClientArea(window, default);

        // Keep the blur region outside the client area so DWM uses the client alpha.
        using var region = CreateRectRgn(-2, -2, -1, -1);
        if (!region.IsInvalid)
        {
            var blur = new BlurBehind { Flags = BlurEnable | BlurRegion, Enabled = 1, Region = region.DangerousGetHandle() };
            _ = DwmEnableBlurBehindWindow(window, blur);
        }
    }

    private static bool ClearBackground(nint window, nint dc)
    {
        return GetClientRect(window, out var bounds) != 0 && FillRect(dc, in bounds, GetStockObject(BlackBrush)) != 0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvStdcall)])]
    private static unsafe nint WindowProcedure(nint window, uint message, nuint wParam, nint lParam, nuint id, nuint data)
    {
        switch (message)
        {
            case EraseBackground when ClearBackground(window, (nint)wParam):
                return 1;
            case DwmCompositionChanged:
                EnableTransparency(window);
                break;
            case DestroyNonClient:
                _ = RemoveWindowSubclass(window, &WindowProcedure, id);
                break;
        }

        return DefSubclassProc(window, message, wParam, lParam);
    }

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmExtendFrameIntoClientArea(nint window, in Margins margins);

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmEnableBlurBehindWindow(nint window, in BlurBehind blur);

    [LibraryImport("gdi32.dll")]
    private static partial RegionHandle CreateRectRgn(int left, int top, int right, int bottom);

    [LibraryImport("gdi32.dll")]
    private static partial int DeleteObject(nint handle);

    [LibraryImport("gdi32.dll")]
    private static partial nint GetStockObject(int index);

    [LibraryImport("user32.dll")]
    private static partial nint GetDC(nint window);

    [LibraryImport("user32.dll")]
    private static partial int ReleaseDC(nint window, nint dc);

    [LibraryImport("user32.dll")]
    private static partial int GetClientRect(nint window, out Rect bounds);

    [LibraryImport("user32.dll")]
    private static partial int FillRect(nint dc, in Rect bounds, nint brush);

    [LibraryImport("comctl32.dll")]
    private static unsafe partial int SetWindowSubclass(
        nint window,
        delegate* unmanaged[Stdcall]<nint, uint, nuint, nint, nuint, nuint, nint> procedure,
        nuint id,
        nuint data);

    [LibraryImport("comctl32.dll")]
    private static unsafe partial int RemoveWindowSubclass(
        nint window,
        delegate* unmanaged[Stdcall]<nint, uint, nuint, nint, nuint, nuint, nint> procedure,
        nuint id);

    [LibraryImport("comctl32.dll")]
    private static partial nint DefSubclassProc(nint window, uint message, nuint wParam, nint lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct Margins
    {
        public int Left;
        public int Right;
        public int Top;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BlurBehind
    {
        public uint Flags;
        public int Enabled;
        public nint Region;
        public int TransitionOnMaximized;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private sealed partial class RegionHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public RegionHandle()
            : base(ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return DeleteObject(this.handle) != 0;
        }
    }
}
