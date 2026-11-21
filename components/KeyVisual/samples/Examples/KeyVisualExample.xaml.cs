// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.System;

namespace JPSoftworks.DesktopToolkit.Controls.Samples;

public sealed partial class KeyVisualExample : Page
{
    public KeyVisualExample()
    {
        this.InitializeComponent();
        this.ArrowKey.Content = VirtualKey.Up;
    }

    public FrameworkElement TakeSampleControls()
    {
        this.SampleLayout.Children.Remove(this.SampleOptions);
        return this.SampleOptions;
    }
}
