// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace JPSoftworks.DesktopToolkit.Charts;

internal static class SankeyChartRenderer
{
    public static void Draw(SankeyChart owner, FlowChartPresenter presenter, Rect bounds)
    {
        if (owner.Data is not { Layers.Length: > 1 } data)
        {
            return;
        }

        var labelWidth = owner.ShowLabels && bounds.Width >= 160 ? Math.Min(130, bounds.Width * 0.19) : 0;
        var gutter = labelWidth + 3;
        var layout = SankeyLayout.Create(
            data, new Rect(gutter, 4, bounds.Width - 2 * gutter, bounds.Height - 8), owner.NodeWidth, owner.NodeSpacing);
        var fills = data.Nodes.Select((node, index) => presenter.Fill(index, node.Brush)).ToArray();
        for (var index = 0; index < layout.Ribbons.Length; index++)
        {
            var ribbon = layout.Ribbons[index];
            if (ribbon.Thickness <= 0)
            {
                continue;
            }

            var edge = data.Edges[index];
            var label = WinUIResourceStrings.FormatDefault("SankeyLinkFormat", data.Nodes[edge.Source].Label, data.Nodes[edge.Target].Label);
            var fill = edge.Link.Brush is null ? fills[edge.Source] : presenter.Fill(edge.Source, edge.Link.Brush);
            var value = ChartInspectorPresenter.FormatValue(edge.Link.Value, owner.ValueFormat);
            presenter.AddMark(
                CreateRibbon(ribbon), ribbon.Bounds, fill, new ChartInspectionItem(ribbon.Anchor, WinUIResourceStrings.FormatDefault("LabelValueFormat", label, value)).WithSwatch(fill),
                new ChartContextValue(label, edge.Link.Value, edge.Link), 0.46, ribbon.Contains);
        }

        for (var layerIndex = 0; layerIndex < data.Layers.Length; layerIndex++)
        {
            var layer = data.Layers[layerIndex];
            foreach (var index in layer)
            {
                var rect = layout.Nodes[index];
                if (rect.Height <= 0)
                {
                    continue;
                }

                var node = data.Nodes[index];
                var incoming = data.IncomingTotals[index];
                var outgoing = data.OutgoingTotals[index];
                var value = Math.Max(incoming, outgoing);
                var middle = rect.Top + rect.Height / 2;
                var fill = fills[index];
                var rows = new List<ChartInspectionRow>
                {
                    new(WinUIResourceStrings.FormatDefault("LabelValueFormat", node.Label, ChartInspectorPresenter.FormatValue(value, owner.ValueFormat)), fill),
                };
                if (incoming > 0 && outgoing > 0 && incoming != outgoing)
                {
                    rows.Add(new(WinUIResourceStrings.FormatDefault("SankeyInflowFormat", ChartInspectorPresenter.FormatValue(incoming, owner.ValueFormat))));
                    rows.Add(new(WinUIResourceStrings.FormatDefault("SankeyOutflowFormat", ChartInspectorPresenter.FormatValue(outgoing, owner.ValueFormat))));
                }

                presenter.AddMark(
                    new RectangleGeometry { Rect = rect }, rect, fill, new ChartInspectionItem(new Point(rect.Right, middle), rows),
                    new ChartContextValue(node.Label, value, node));
                if (labelWidth > 0 && rect.Height >= owner.FontSize * 1.4)
                {
                    if (layerIndex == 0)
                    {
                        presenter.AddLabel(node.Label, 0, middle, rect.Left - 7, TextAlignment.Right);
                    }
                    else
                    {
                        var nextX = layerIndex + 1 < data.Layers.Length ? layout.Nodes[data.Layers[layerIndex + 1][0]].Left : bounds.Right;
                        presenter.AddLabel(
                            node.Label, rect.Right + 7, middle, Math.Min(labelWidth, nextX - rect.Right - 10), TextAlignment.Left);
                    }
                }
            }
        }
    }

    private static PathGeometry CreateRibbon(SankeyRibbon ribbon)
    {
        var third = (ribbon.Right - ribbon.Left) / 3;
        var firstX = ribbon.Left + third;
        var lastX = ribbon.Right - third;
        var sourceBottom = ribbon.SourceTop + ribbon.Thickness;
        var targetBottom = ribbon.TargetTop + ribbon.Thickness;
        return new PathGeometry
        {
            Figures =
            {
                new PathFigure
                {
                    StartPoint = new Point(ribbon.Left, ribbon.SourceTop),
                    IsClosed = true,
                    Segments =
                    {
                        new BezierSegment
                        {
                            Point1 = new Point(firstX, ribbon.SourceTop),
                            Point2 = new Point(lastX, ribbon.TargetTop),
                            Point3 = new Point(ribbon.Right, ribbon.TargetTop),
                        },
                        new LineSegment { Point = new Point(ribbon.Right, targetBottom) },
                        new BezierSegment
                        {
                            Point1 = new Point(lastX, targetBottom),
                            Point2 = new Point(firstX, sourceBottom),
                            Point3 = new Point(ribbon.Left, sourceBottom),
                        },
                    },
                },
            },
        };
    }
}
