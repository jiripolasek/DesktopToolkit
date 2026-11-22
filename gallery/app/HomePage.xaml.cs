// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace JPSoftworks.DesktopToolkit.Gallery;

public sealed partial class HomePage : Page
{
    public HomePage()
    {
        this.InitializeComponent();
    }

    private void OnCardClicked(object sender, RoutedEventArgs args)
    {
        if (sender is Button { Tag: GalleryDestination destination })
        {
            this.Frame.Navigate(destination.PageType, destination.ComponentId);
        }
    }
}
