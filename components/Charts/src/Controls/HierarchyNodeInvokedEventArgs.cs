// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Describes a tile activated by touch, mouse, or keyboard.</summary>
public sealed class HierarchyNodeInvokedEventArgs : EventArgs
{
    /// <summary>Gets the node represented by the activated tile.</summary>
    public HierarchyChartNode Node { get; }

    /// <summary>Gets or sets whether to dismiss the inspector and consume the input.</summary>
    public bool Handled { get; set; }

    internal HierarchyNodeInvokedEventArgs(HierarchyChartNode node)
    {
        this.Node = node;
    }
}
