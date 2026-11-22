// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace JPSoftworks.DesktopToolkit.Controls;

internal sealed partial class SearchableDropDownAutomationPeer(SearchableDropDown owner)
    : DropDownButtonAutomationPeer(owner), IValueProvider
{
    public bool IsReadOnly => true;

    public string Value => owner.SelectedText;

    public void SetValue(string value)
    {
        throw new InvalidOperationException(WinUIResourceStrings.Get("ChooseDropDownValue"));
    }

    protected override AutomationControlType GetAutomationControlTypeCore()
    {
        return AutomationControlType.ComboBox;
    }

    protected override string GetClassNameCore()
    {
        return nameof(SearchableDropDown);
    }

    protected override object GetPatternCore(PatternInterface patternInterface)
    {
        return patternInterface == PatternInterface.Value ? this : base.GetPatternCore(patternInterface);
    }
}
