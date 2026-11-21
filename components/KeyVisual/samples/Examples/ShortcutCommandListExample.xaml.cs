// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml.Controls;
using Windows.System;

namespace JPSoftworks.DesktopToolkit.Controls.Samples;

public sealed partial class ShortcutCommandListExample : Page
{
    private KeyboardShortcut NewDocumentShortcut { get; } = new(VirtualKey.N, VirtualKeyModifiers.Control);

    private KeyboardShortcut OpenProjectShortcut { get; } = new(VirtualKey.O, VirtualKeyModifiers.Control);

    private KeyboardShortcut FindInFilesShortcut { get; } = new(VirtualKey.F, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);

    public ShortcutCommandListExample()
    {
        this.InitializeComponent();
    }
}
