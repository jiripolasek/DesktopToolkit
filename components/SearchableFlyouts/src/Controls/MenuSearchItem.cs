// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace JPSoftworks.DesktopToolkit.Controls;

/// <summary>Identifies a temporary filter or empty-message container inserted by <see cref="SearchableMenuFlyout"/>.</summary>
public sealed partial class MenuSearchItem : MenuFlyoutItem
{
    internal bool IsFilter { get; init; }

    internal MenuFlyoutItemBase? MenuParent { get; init; }

    internal MenuSearchItem() { }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new SearchItemPeer(this);
    }

    private sealed partial class SearchItemPeer(MenuSearchItem owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return owner.IsFilter ? AutomationControlType.Group : AutomationControlType.Text;
        }

        protected override string GetClassNameCore()
        {
            return nameof(MenuSearchItem);
        }

        protected override string GetNameCore()
        {
            return owner.IsFilter ? AutomationProperties.GetName(owner) : owner.Text;
        }
    }
}
