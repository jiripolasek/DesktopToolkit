// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using JPSoftworks.DesktopToolkit.Gallery.UI;
using Microsoft.UI.Xaml.Controls;

namespace JPSoftworks.DesktopToolkit.Styles.Samples;

public sealed partial class PillSearchBoxStylePage : Page
{
    public PillSearchBoxStylePage()
    {
        this.InitializeComponent();
        this.PageHeader.SetLinks(
        [
            GalleryLink.ForComponentDocumentation("styles", "#pill-search-box"),
            GalleryLink.ForComponentSource("Styles"),
            GalleryLink.ForNuGetPackage("JPSoftworks.DesktopToolkit.Styles"),
        ]);
        this.SearchBoxExample.Xaml = """
            <!-- Add to App.xaml's merged dictionaries after XamlControlsResources. -->
            <ResourceDictionary
                Source="ms-appx:///JPSoftworks.DesktopToolkit.Styles/Themes/PillSearchBoxStyle.xaml" />

            <!-- Add to your page or window. -->
            <AutoSuggestBox
                Header="Search settings"
                PlaceholderText="Find a setting"
                QueryIcon="Find"
                Style="{StaticResource PillSearchBoxStyle}" />
            """;
    }
}
