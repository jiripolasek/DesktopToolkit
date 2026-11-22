// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Windows.Foundation;

namespace JPSoftworks.DesktopToolkit.Charts;

internal static class HierarchyChartReadout
{
    public static ChartInspectionItem Read<T>(
        IReadOnlyList<T> items,
        int index,
        HierarchyChartNode root,
        Func<T, (HierarchyChartNode Node, int Parent)> getItem,
        Point anchor,
        string? format)
    {
        var (node, parentIndex) = getItem(items[index]);
        var parent = parentIndex >= 0 ? getItem(items[parentIndex]).Node : root;
        var path = new Stack<string>();
        path.Push(node.Label);
        var first = node;
        for (var current = parentIndex; current >= 0;)
        {
            var item = getItem(items[current]);
            first = item.Node;
            path.Push(first.Label);
            current = item.Parent;
        }

        if (!ReferenceEquals(first, root))
        {
            path.Push(root.Label);
        }

        var value = ChartInspectorPresenter.FormatValue(node.Value, format);
        var detail = ReferenceEquals(node, root)
            ? value
            : WinUIResourceStrings.FormatDefault("HierarchyShareFormat", value, node.Value / parent.Value, parent.Label);
        var text = $"{string.Join(WinUIResourceStrings.Get("HierarchyPathSeparator"), path)}\n{detail}";
        if (node.IsDeferred)
        {
            text += "\n" + WinUIResourceStrings.Get("ChildDetailsPending");
        }

        return new ChartInspectionItem(anchor, text);
    }
}
