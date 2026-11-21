// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace JPSoftworks.DesktopToolkit.Controls.SearchableFlyouts.Samples;

public sealed partial class SearchableMenuFlyoutExample : Page
{
    public SearchableMenuFlyoutExample()
    {
        this.InitializeComponent();
    }

    private void OnAction(object sender, RoutedEventArgs args)
    {
        this.ActionResult.Text = $"{((MenuFlyoutItem)sender).Text} selected";
    }

    private void OnPinChanged(object sender, RoutedEventArgs args)
    {
        this.ActionResult.Text = ((ToggleMenuFlyoutItem)sender).IsChecked
            ? "Document pinned to favorites"
            : "Document removed from favorites";
    }
}
