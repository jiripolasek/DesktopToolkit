// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Windows.Foundation;

namespace JPSoftworks.DesktopToolkit.Charts;

internal sealed record SankeyLayout(Rect[] Nodes, SankeyRibbon[] Ribbons)
{
    public static SankeyLayout Create(SankeyChartData data, Rect bounds, double requestedWidth, double requestedSpacing)
    {
        var layers = data.Layers;
        var span = bounds.Width / layers.Length;
        var width = Math.Clamp(double.IsFinite(requestedWidth) ? requestedWidth : 16, 0, span * 0.6);
        var mostNodes = layers.Max(layer => layer.Length);
        var spacing = Math.Clamp(double.IsFinite(requestedSpacing) ? requestedSpacing : 14, 0, bounds.Height / mostNodes / 2);
        var maximum = data.Edges.Max(edge => edge.Link.Value);

        double Weight(int index)
        {
            return Math.Max(data.IncomingTotals[index], data.OutgoingTotals[index]) / maximum;
        }

        var scale = layers.Min(layer => (bounds.Height - (layer.Length - 1) * spacing) / layer.Sum(Weight));
        var nodes = new Rect[data.Nodes.Count];

        for (var column = 0; column < layers.Length; column++)
        {
            var layer = layers[column];
            var x = bounds.Left + column * (bounds.Width - width) / (layers.Length - 1);
            var layerHeight = layer.Sum(Weight) * scale + (layer.Length - 1) * spacing;
            var y = bounds.Top + (bounds.Height - layerHeight) / 2;
            foreach (var index in layer)
            {
                var height = Weight(index) * scale;
                nodes[index] = new Rect(x, y, width, height);
                y += height + spacing;
            }
        }

        var sourceY = new double[data.Edges.Length];
        var targetY = new double[data.Edges.Length];
        PlaceLinks(sourceY, incoming: false);
        PlaceLinks(targetY, incoming: true);
        var ribbons = new SankeyRibbon[data.Edges.Length];
        for (var index = 0; index < ribbons.Length; index++)
        {
            var edge = data.Edges[index];
            ribbons[index] = new(nodes[edge.Source].Right, nodes[edge.Target].Left, sourceY[index], targetY[index], Thickness(index));
        }

        return new(nodes, ribbons);

        double Thickness(int index)
        {
            return data.Edges[index].Link.Value / maximum * scale;
        }

        void PlaceLinks(double[] positions, bool incoming)
        {
            var groups = Enumerable.Range(0, data.Edges.Length)
                .GroupBy(index => incoming ? data.Edges[index].Target : data.Edges[index].Source);
            foreach (var group in groups)
            {
                var rect = nodes[group.Key];
                var y = rect.Top + (rect.Height - group.Sum(Thickness)) / 2;
                foreach (var index in group.OrderBy(index => nodes[incoming ? data.Edges[index].Source : data.Edges[index].Target].Top))
                {
                    positions[index] = y;
                    y += Thickness(index);
                }
            }
        }
    }
}

internal readonly record struct SankeyRibbon(double Left, double Right, double SourceTop, double TargetTop, double Thickness)
{
    public Rect Bounds => new(
        this.Left, Math.Min(this.SourceTop, this.TargetTop), this.Right - this.Left,
        Math.Abs(this.SourceTop - this.TargetTop) + this.Thickness);

    public Point Anchor => new((this.Left + this.Right) / 2, (this.SourceTop + this.TargetTop + this.Thickness) / 2);

    public bool Contains(Point point)
    {
        var t = (point.X - this.Left) / (this.Right - this.Left);
        var top = this.SourceTop + (this.TargetTop - this.SourceTop) * t * t * (3 - 2 * t);
        return point.Y >= top && point.Y <= top + this.Thickness;
    }
}
