## Quick start

Add a searchable **Document actions** menu to an existing WinUI&nbsp;3 app. Searching for **duplicate** will find **Save a copy** through its keyword.

![The Gallery's Document actions menu filtered to Save a copy by the keyword duplicate.](../../../assets/quick-starts/searchable-flyouts.png)

**App requirements.** Windows 10 version 2004 (build 19041) or later, Windows App SDK 1.8.260317003 or later, and an app targeting .NET 8, .NET 10, or .NET 11. Use the SDK that matches your app's .NET target.

{{installLocalPreview}}

### Filter a native menu

Place this inside the layout of an existing WinUI page or window. Run the app, open **Document actions**, and type **duplicate** into the filter. Only **Save a copy** should remain.

```xml wrap
<DropDownButton
    xmlns:menus="using:JPSoftworks.DesktopToolkit.Controls"
    Content="Document actions">
    <DropDownButton.Flyout>
        <menus:SearchableMenuFlyout
            FilterBoxAutomationName="Find a document action">
            <MenuFlyoutItem Text="New document" />
            <MenuFlyoutItem
                Text="Save a copy"
                menus:SearchableMenuFlyout.Keywords="duplicate">
                <MenuFlyoutItem.Icon>
                    <SymbolIcon Symbol="Save" />
                </MenuFlyoutItem.Icon>
                <MenuFlyoutItem.KeyboardAccelerators>
                    <KeyboardAccelerator Key="S" Modifiers="Control,Shift" />
                </MenuFlyoutItem.KeyboardAccelerators>
            </MenuFlyoutItem>
            <MenuFlyoutItem Text="Print" />
        </menus:SearchableMenuFlyout>
    </DropDownButton.Flyout>
</DropDownButton>
```

Connect your handlers in the same way as a standard menu. See [Native commands](#native-commands) for a `Click` example.
