// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace JPSoftworks.DesktopToolkit.Controls.SearchableFlyouts.Samples;

public sealed partial class SearchableSubmenuExample : Page
{
    public SearchableFlyoutStrings MenuStrings { get; } = new()
    {
        CommandsName = "Export options",
        FilterSubmenuFormat = "Find in {0}",
    };

    public SearchableSubmenuExample()
    {
        this.InitializeComponent();
    }

    private void OnFormatSelected(object sender, RoutedEventArgs args)
    {
        this.ExportResult.Text = $"Selected format: {((MenuFlyoutItem)sender).Text}";
    }
}
