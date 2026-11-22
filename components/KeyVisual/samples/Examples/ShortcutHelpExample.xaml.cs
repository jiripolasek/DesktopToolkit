// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml.Controls;
using Windows.System;

namespace JPSoftworks.DesktopToolkit.Controls.Samples;

public sealed partial class ShortcutHelpExample : Page
{
    private KeyboardShortcut PreviewResultShortcut { get; } = new(VirtualKey.Space, VirtualKeyModifiers.Control);

    private KeyboardShortcut ShortcutHelpShortcut { get; } = new(VirtualKey.F1, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);

    public ShortcutHelpExample()
    {
        this.InitializeComponent();
    }
}
