// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace JPSoftworks.DesktopToolkit.Controls;

internal sealed partial class FlyoutItemsListAutomationPeer(FlyoutItemsListView owner) : ListViewAutomationPeer(owner)
{
    private readonly ConditionalWeakTable<FlyoutItemEntry, FlyoutItemEntryAutomationPeer> _peers = new();

    internal void NotifyItemChanged(SearchableFlyoutItem item)
    {
        foreach (var entry in owner.Items.OfType<FlyoutItemEntry>().Where(entry => ReferenceEquals(entry.Item, item)))
        {
            if (this._peers.TryGetValue(entry, out var peer))
            {
                peer.NotifyStateChanged();
            }
        }
    }

    protected override AutomationControlType GetAutomationControlTypeCore()
    {
        return AutomationControlType.Menu;
    }

    protected override string GetClassNameCore()
    {
        return nameof(SearchableItemsFlyout);
    }

    protected override object GetPatternCore(PatternInterface patternInterface)
    {
        return patternInterface == PatternInterface.Selection ? null! : base.GetPatternCore(patternInterface);
    }

    protected override ItemAutomationPeer OnCreateItemAutomationPeer(object item)
    {
        return this._peers.GetValue((FlyoutItemEntry)item, entry => new FlyoutItemEntryAutomationPeer(entry, this, owner.Menu));
    }
}

internal sealed partial class FlyoutItemEntryAutomationPeer(
    FlyoutItemEntry entry,
    ListViewBaseAutomationPeer parent,
    SearchableItemsFlyout menu) : ListViewItemDataAutomationPeer(entry, parent), IInvokeProvider, IToggleProvider, IExpandCollapseProvider
{
    private ToggleState _lastToggleState = entry.Item.IsChecked ? ToggleState.On : ToggleState.Off;

    private ExpandCollapseState _lastExpandState
        = menu.IsSubmenuOpen(entry.Item) ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

    private SearchableFlyoutItem MenuItem => entry.Item;

    public ExpandCollapseState ExpandCollapseState =>
        menu.IsSubmenuOpen(this.MenuItem) ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

    public void Expand()
    {
        this.EnsureEnabled();
        menu.OpenSubmenu(this.MenuItem);
    }

    public void Collapse()
    {
        menu.CollapseSubmenu(this.MenuItem);
    }

    public void Invoke()
    {
        this.EnsureEnabled();
        menu.InvokeItem(this.MenuItem);
    }

    public ToggleState ToggleState => this.MenuItem.IsChecked ? ToggleState.On : ToggleState.Off;

    public void Toggle()
    {
        this.Invoke();
    }

    protected override AutomationControlType GetAutomationControlTypeCore()
    {
        return this.MenuItem.IsSeparator ? AutomationControlType.Separator : AutomationControlType.MenuItem;
    }

    protected override string GetClassNameCore()
    {
        return nameof(SearchableFlyoutItem);
    }

    protected override string GetNameCore()
    {
        return string.IsNullOrEmpty(this.MenuItem.AutomationName) ? this.MenuItem.Text : this.MenuItem.AutomationName;
    }

    protected override string GetHelpTextCore()
    {
        return this.MenuItem.AutomationHelpText;
    }

    protected override string GetAutomationIdCore()
    {
        return string.IsNullOrEmpty(this.MenuItem.AutomationId) ? base.GetAutomationIdCore() : this.MenuItem.AutomationId;
    }

    protected override int GetPositionInSetCore()
    {
        return entry.PositionInSet;
    }

    protected override int GetSizeOfSetCore()
    {
        return entry.SizeOfSet;
    }

    protected override string GetAcceleratorKeyCore()
    {
        return this.MenuItem.KeyboardAcceleratorText;
    }

    protected override bool IsEnabledCore()
    {
        return this.MenuItem.CanInvoke;
    }

    protected override object GetPatternCore(PatternInterface patternInterface)
    {
        return patternInterface switch
        {
            PatternInterface.Invoke when !this.MenuItem.IsSeparator && !this.MenuItem.IsCheckable && !this.MenuItem.HasChildren => this,
            PatternInterface.Toggle when this.MenuItem.IsCheckable && !this.MenuItem.HasChildren => this,
            PatternInterface.ExpandCollapse when this.MenuItem.HasChildren => this,
            PatternInterface.Invoke or PatternInterface.Toggle or PatternInterface.ExpandCollapse
                or PatternInterface.SelectionItem => null!,
            _ => base.GetPatternCore(patternInterface),
        };
    }

    internal void NotifyStateChanged()
    {
        var toggle = this.ToggleState;
        if (this.MenuItem.IsCheckable && toggle != this._lastToggleState)
        {
            this.RaisePropertyChangedEvent(TogglePatternIdentifiers.ToggleStateProperty, this._lastToggleState, toggle);
            this._lastToggleState = toggle;
        }

        var expanded = this.ExpandCollapseState;
        if (this.MenuItem.HasChildren && expanded != this._lastExpandState)
        {
            this.RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty, this._lastExpandState, expanded);
            this._lastExpandState = expanded;
        }
    }

    private void EnsureEnabled()
    {
        if (!this.MenuItem.CanInvoke)
        {
            throw new ElementNotEnabledException();
        }
    }
}
