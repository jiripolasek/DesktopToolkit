// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using JPSoftworks.DesktopToolkit.Charts;

namespace JPSoftworks.DesktopToolkit.Controls.Demo.Services;

internal static class FolderSizeSample
{
    public static HierarchyChartData Summary { get; } = new(
        new HierarchyChartNode(
            "workspace", "Workspace",
            new(
                "projects", "Projects", new("web", "Web app", new("web/source", "Source", 18), new("web/tests", "Tests", 8)),
                new("mobile", "Mobile app", new("mobile/source", "Source", 12), new("mobile/assets", "Assets", 7)),
                new("archive", "Archive", 9)), HierarchyChartNode.CreateDeferredBranch("packages", "Packages", 40),
            new("build", "Build output", new("debug", "Debug", 16), new("release", "Release", 10)),
            new("media", "Media", new("images", "Images", 10), new("recordings", "Recordings", 6)), new("tools", "Tools", 8)));

    public static HierarchyChartData LoadPackages(HierarchyChartData data)
    {
        return data.WithChildren("packages", new("nuget", "NuGet", 24), new("npm", "npm", 16));
    }
}
