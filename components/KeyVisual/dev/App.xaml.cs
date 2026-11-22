// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using JPSoftworks.DesktopToolkit.Controls.Samples;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace JPSoftworks.DesktopToolkit.Controls.KeyVisualDevApp;

public sealed partial class App : Application
{
    private Window? _window;

    public App()
    {
        this.InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var examples = new StackPanel { Spacing = 24 };
        examples.Children.Add(new KeyVisualExample());
        examples.Children.Add(new ShortcutEditorExample());

        this._window = new Window
        {
            Title = "KeyVisual Development", Content = new ScrollViewer { Padding = new Thickness(24), Content = examples, },
        };
        this._window.Activate();
    }
}
