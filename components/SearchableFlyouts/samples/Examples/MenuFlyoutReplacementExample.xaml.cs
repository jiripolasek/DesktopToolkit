// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace JPSoftworks.DesktopToolkit.Controls.SearchableFlyouts.Samples;

public sealed partial class MenuFlyoutReplacementExample : Page
{
    public static readonly DependencyProperty IsPinnedProperty = DependencyProperty.Register(
        nameof(IsPinned), typeof(bool), typeof(MenuFlyoutReplacementExample), new PropertyMetadata(false));

    public bool IsPinned
    {
        get => (bool)this.GetValue(IsPinnedProperty);
        set => this.SetValue(IsPinnedProperty, value);
    }

    public MenuFlyoutReplacementExample()
    {
        this.InitializeComponent();
    }

    private void OnFileAction(object sender, RoutedEventArgs args)
    {
        this.FileActionResult.Text = $"{((MenuFlyoutItem)sender).Text} selected for Project notes.docx";
    }

    private void OnPinChanged(object sender, RoutedEventArgs args)
    {
        this.FileActionResult.Text = ((ToggleMenuFlyoutItem)sender).IsChecked
            ? "Project notes.docx pinned to favorites"
            : "Project notes.docx removed from favorites";
    }
}
