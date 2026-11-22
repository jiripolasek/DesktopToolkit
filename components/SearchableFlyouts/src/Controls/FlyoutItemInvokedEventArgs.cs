// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Controls;

/// <summary>Provides data for the <see cref="SearchableItemsFlyout.ItemInvoked"/> event.</summary>
public sealed class FlyoutItemInvokedEventArgs : EventArgs
{
    /// <summary>Gets the original item model that was invoked.</summary>
    public SearchableFlyoutItem InvokedItem { get; }

    internal FlyoutItemInvokedEventArgs(SearchableFlyoutItem item)
    {
        this.InvokedItem = item;
    }
}
