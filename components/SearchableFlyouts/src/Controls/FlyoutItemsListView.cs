// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace JPSoftworks.DesktopToolkit.Controls;

internal sealed partial class FlyoutItemsListView : ListView
{
    internal SearchableItemsFlyout Menu { get; set; } = null!;

    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return this.Menu is null || this.Menu.IsSelectionOnly ? base.OnCreateAutomationPeer() : new FlyoutItemsListAutomationPeer(this);
    }
}
