// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace JPSoftworks.DesktopToolkit.Controls.SearchableFlyouts.Samples;

public sealed partial class SearchableContextMenuExample : Page
{
    public SearchableContextMenuExample()
    {
        this.InitializeComponent();
    }

    private void OnOpenMenu(object sender, RoutedEventArgs args)
    {
        this.FileMenu.ShowAt(this.FileTarget);
    }

    private void OnItemInvoked(SearchableItemsFlyout sender, FlyoutItemInvokedEventArgs args)
    {
        var item = args.InvokedItem;
        this.FileResult.Text = item.IsCheckable
            ? item.IsChecked ? "Project notes.docx pinned to favorites" : "Project notes.docx removed from favorites"
            : $"{item.Text} selected for Project notes.docx";
    }
}
