// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Windows.UI;

namespace JPSoftworks.DesktopToolkit.Charts;

/// <summary>
/// Describes one slice in a <see cref="DoughnutGraph"/>.
/// </summary>
public sealed record DoughnutGraphSeries(string Name, Color Color);
