// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml.Controls.Primitives;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>Creates the flyout to show for a chart data item or selection.</summary>
public interface IChartContextMenuProvider
{
    /// <summary>Returns a flyout for the request, or null to leave the request unhandled.</summary>
    FlyoutBase? CreateFlyout(ChartContextMenuRequest request);
}
