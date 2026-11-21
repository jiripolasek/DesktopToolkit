// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml.Controls;

namespace JPSoftworks.DesktopToolkit.ToastWindows;

/// <summary>Provides an optional button on a toast.</summary>
/// <param name="Label">The button's label.</param>
/// <param name="Invoke">The callback to run after dismissing the toast.</param>
/// <param name="Icon">An optional button icon.</param>
public sealed record ToastAction(string Label, Action Invoke, IconSource? Icon = null);
