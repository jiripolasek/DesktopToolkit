// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace JPSoftworks.DesktopToolkit.Controls.Demo.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        this.InitializeComponent();
        this.Loaded += (_, _) =>
        {
            if (this.XamlRoot.Content is FrameworkElement root)
            {
                this.ThemeBox.SelectedIndex = (int)root.RequestedTheme;
            }
        };
    }

    private void OnThemeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (this.IsLoaded && this.XamlRoot.Content is FrameworkElement root)
        {
            root.RequestedTheme = (ElementTheme)this.ThemeBox.SelectedIndex;
        }
    }
}
