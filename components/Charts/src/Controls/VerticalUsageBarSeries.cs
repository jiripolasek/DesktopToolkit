// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml.Media;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>
/// Describes one stacked contribution in a <see cref="VerticalUsageBar"/>.
/// </summary>
public sealed record VerticalUsageBarSeries(string Name, Brush IndicatorBrush);
