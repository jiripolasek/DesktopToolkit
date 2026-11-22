# Shared gallery UI

[Gallery.UI](JPSoftworks.DesktopToolkit.Gallery.UI.csproj) is an internal WinUI library for presenting samples.
It contains `ExampleCard` for live previews and source tabs, `GalleryPageHeader` for page titles and API
details, and `GalleryLink` for resource links.

The [main Gallery](../app/JPSoftworks.DesktopToolkit.Gallery.csproj) and the
[standalone Charts dev app](../../components/Charts/dev/JPSoftworks.DesktopToolkit.Controls.Charts.DevApp.csproj)
both host pages from [Charts.Samples](../../components/Charts/samples/JPSoftworks.DesktopToolkit.Controls.Charts.Samples.csproj).
Those pages use the shared controls, and the main Gallery also uses them directly on its component pages.

The separate project prevents a circular reference. The Gallery already references `Charts.Samples`, so putting
the shared controls in the Gallery app would require `Charts.Samples` to reference the Gallery in return.
