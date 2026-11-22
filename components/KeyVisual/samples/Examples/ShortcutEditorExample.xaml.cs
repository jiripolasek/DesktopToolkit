// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.System;

namespace JPSoftworks.DesktopToolkit.Controls.Samples;

public sealed partial class ShortcutEditorExample : Page
{
    public ShortcutEditorExample()
    {
        this.InitializeComponent();
        this.LauncherEditor.Strings = new() { DialogTitle = "Edit launcher shortcut" };
        this.SearchEditor.Strings = new() { DialogTitle = "Edit search shortcut" };
        var launcherShortcut = new KeyboardShortcut(VirtualKey.Space, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);
        var searchShortcut = new KeyboardShortcut(VirtualKey.K, VirtualKeyModifiers.Control);
        Configure(this.LauncherEditor, launcherShortcut, this.SearchEditor, "search", "Open search");
        Configure(this.SearchEditor, searchShortcut, this.LauncherEditor, "launcher", "Open launcher");
    }

    public FrameworkElement TakeSampleControls()
    {
        this.SampleLayout.Children.Remove(this.SampleOptions);
        return this.SampleOptions;
    }

    private static void Configure(
        ShortcutEditor editor,
        KeyboardShortcut defaultShortcut,
        ShortcutEditor other,
        string otherId,
        string otherName)
    {
        var printShortcut = new KeyboardShortcut(VirtualKey.P, VirtualKeyModifiers.Control);
        editor.DefaultShortcut = defaultShortcut;
        editor.Shortcut = defaultShortcut;
        editor.ValidateShortcut = shortcut => shortcut == printShortcut ? "Ctrl+P is reserved for printing." : null;
        editor.FindConflict = shortcut => other.Shortcut == shortcut
            ? new ShortcutConflict(
                otherId, otherName, () =>
                {
                    if (other.Shortcut != shortcut)
                    {
                        return false;
                    }

                    other.Shortcut = null;
                    editor.Shortcut = shortcut;
                    return true;
                })
            : null;
    }
}
