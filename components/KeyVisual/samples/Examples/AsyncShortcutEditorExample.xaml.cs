// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Windows.System;

namespace JPSoftworks.DesktopToolkit.Controls.Samples;

public sealed partial class AsyncShortcutEditorExample : Page
{
    public AsyncShortcutEditorExample()
    {
        this.InitializeComponent();
        this.LauncherEditor.Shortcut = new KeyboardShortcut(VirtualKey.Space, VirtualKeyModifiers.Control);
        this.SearchEditor.Shortcut = new KeyboardShortcut(VirtualKey.K, VirtualKeyModifiers.Control);
        Configure(this.LauncherEditor, this.SearchEditor, "search", "Open search");
        Configure(this.SearchEditor, this.LauncherEditor, "launcher", "Open launcher");
    }

    private static void Configure(ShortcutEditor editor, ShortcutEditor other, string ownerId, string ownerName)
    {
        var printShortcut = new KeyboardShortcut(VirtualKey.P, VirtualKeyModifiers.Control);
        editor.ValidateShortcutAsync = async (shortcut, cancellationToken) =>
        {
            var reserved = shortcut == printShortcut;
            await Task.Delay(TimeSpan.FromMilliseconds(reserved ? 1200 : 200), cancellationToken);
            return reserved ? "Ctrl+P is reserved for printing." : null;
        };

        editor.FindConflictAsync = async (shortcut, cancellationToken) =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(shortcut.Key == VirtualKey.K ? 1200 : 200), cancellationToken);
            if (other.Shortcut != shortcut)
            {
                return null;
            }

            return new ShortcutConflict(ownerId, ownerName)
            {
                TryReplaceAsync = async () =>
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(700));
                    if (other.Shortcut != shortcut)
                    {
                        return false;
                    }

                    other.Shortcut = null;
                    editor.Shortcut = shortcut;
                    return true;
                },
            };
        };
    }
}
