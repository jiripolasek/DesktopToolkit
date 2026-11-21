// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Windows.System;

namespace JPSoftworks.DesktopToolkit.Controls;

/// <summary>A keyboard chord consisting of a primary key and one or more modifiers.</summary>
public sealed partial record KeyboardShortcut
{
    private const VirtualKeyModifiers AllModifiers
        = VirtualKeyModifiers.Windows | VirtualKeyModifiers.Control | VirtualKeyModifiers.Menu | VirtualKeyModifiers.Shift;

    /// <summary>Gets the primary key.</summary>
    public VirtualKey Key { get; }

    /// <summary>Gets the modifier keys.</summary>
    public VirtualKeyModifiers Modifiers { get; }

    /// <summary>Creates a shortcut using a keyboard key and one or more modifiers.</summary>
    /// <exception cref="ArgumentException">The key or modifier combination is invalid.</exception>
    public KeyboardShortcut(VirtualKey key, VirtualKeyModifiers modifiers)
    {
        if (!IsValid(key, modifiers))
        {
            throw new ArgumentException("Use a key with Ctrl, Alt, Shift, or Windows. Escape and Shift+Tab are reserved for navigation.");
        }

        this.Key = key;
        this.Modifiers = modifiers;
    }

    /// <summary>Returns the labels for the shortcut in display order.</summary>
    public override string ToString()
    {
        return Format(this.Key, this.Modifiers);
    }

    internal static bool IsValid(VirtualKey key, VirtualKeyModifiers modifiers)
    {
        return (int)key is >= 8 and < 255
               && key != VirtualKey.Escape
               && GetModifier(key) == VirtualKeyModifiers.None
               && modifiers != VirtualKeyModifiers.None
               && (modifiers & ~AllModifiers) == 0
               && !(key == VirtualKey.Tab && modifiers == VirtualKeyModifiers.Shift);
    }

    internal static VirtualKeyModifiers GetModifier(VirtualKey key)
    {
        return key switch
        {
            VirtualKey.LeftWindows or VirtualKey.RightWindows => VirtualKeyModifiers.Windows,
            VirtualKey.Control or VirtualKey.LeftControl or VirtualKey.RightControl => VirtualKeyModifiers.Control,
            VirtualKey.Menu or VirtualKey.LeftMenu or VirtualKey.RightMenu => VirtualKeyModifiers.Menu,
            VirtualKey.Shift or VirtualKey.LeftShift or VirtualKey.RightShift => VirtualKeyModifiers.Shift,
            _ => VirtualKeyModifiers.None,
        };
    }

    internal static string Format(VirtualKey key, VirtualKeyModifiers modifiers)
    {
        return string.Join(" + ", GetKeys(key, modifiers).Select(KeyVisual.GetKeyLabel));
    }

    internal static IEnumerable<VirtualKey> GetKeys(VirtualKey key, VirtualKeyModifiers modifiers)
    {
        if ((modifiers & VirtualKeyModifiers.Windows) != 0)
        {
            yield return VirtualKey.LeftWindows;
        }

        if ((modifiers & VirtualKeyModifiers.Control) != 0)
        {
            yield return VirtualKey.Control;
        }

        if ((modifiers & VirtualKeyModifiers.Menu) != 0)
        {
            yield return VirtualKey.Menu;
        }

        if ((modifiers & VirtualKeyModifiers.Shift) != 0)
        {
            yield return VirtualKey.Shift;
        }

        if (key != VirtualKey.None)
        {
            yield return key;
        }
    }
}
