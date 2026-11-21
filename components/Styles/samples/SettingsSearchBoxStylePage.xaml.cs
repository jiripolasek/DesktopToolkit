// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using JPSoftworks.DesktopToolkit.Gallery.UI;
using Microsoft.UI.Xaml.Controls;

namespace JPSoftworks.DesktopToolkit.Styles.Samples;

public sealed partial class SettingsSearchBoxStylePage : Page
{
    public SettingsSearchBoxStylePage()
    {
        this.InitializeComponent();
        this.PageHeader.SetLinks(
        [
            GalleryLink.ForComponentDocumentation("styles", "#settings-search-box"),
            GalleryLink.ForComponentSource("Styles"),
            GalleryLink.ForNuGetPackage("JPSoftworks.DesktopToolkit.Styles"),
        ]);
        this.SearchBoxExample.UseEmbeddedSource(typeof(SettingsSearchBoxStylePage));
    }
}
