// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Security;
using Microsoft.Win32;

namespace JPSoftworks.DesktopToolkit.ToastWindows;

/// <summary>Positions a toast within the display's work area.</summary>
public enum ToastPosition
{
    /// <summary>Follow the Windows on-screen indicator position, with a bottom-center fallback.</summary>
    UseSystemSettings = 0,

    /// <summary>Use the bottom center of the work area.</summary>
    BottomCenter = 1,

    /// <summary>Use the top left of the work area.</summary>
    TopLeft = 2,

    /// <summary>Use the top center of the work area.</summary>
    TopCenter = 3,
}

internal static class ToastPositionSettings
{
    internal static ToastPosition Resolve(ToastPosition position)
    {
        if (position != ToastPosition.UseSystemSettings)
        {
            return position;
        }

        try
        {
            // Windows uses a different index for bottom center.
            return Registry.GetValue(
                    @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\SystemSettings\ConfirmatorPosition", "PositionIndex",
                    null) switch
                {
                    3 => ToastPosition.TopCenter,
                    2 => ToastPosition.TopLeft,
                    _ => ToastPosition.BottomCenter,
                };
        }
        catch (Exception exception) when (exception is IOException or SecurityException or UnauthorizedAccessException)
        {
            return ToastPosition.BottomCenter;
        }
    }
}
