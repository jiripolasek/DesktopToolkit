// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Gallery.UI;

public enum GalleryLinkKind
{
    SourceCode,
    Documentation,
    NuGetPackage,
    Styling,
}

public sealed record GalleryLink(GalleryLinkKind Kind, string Label, Uri? Uri, string? PackageId = null)
{
    public const string RepositoryUrl = "https://github.com/jiripolasek/DesktopToolkit";

    public static GalleryLink ForComponentDocumentation(string componentSlug, string section = "")
    {
        return new(
            GalleryLinkKind.Documentation, "Documentation",
            new Uri($"https://desktoptoolkit.jiripolasek.com/components/{componentSlug}/{section}"));
    }

    public static GalleryLink ForComponentSource(string componentDirectory)
    {
        return new(GalleryLinkKind.SourceCode, "Source", new Uri($"{RepositoryUrl}/tree/main/components/{componentDirectory}"));
    }

    public static GalleryLink ForNuGetPackage(string packageId)
    {
        return new(GalleryLinkKind.NuGetPackage, "NuGet", new Uri($"https://www.nuget.org/packages/{packageId}"), packageId);
    }
}
