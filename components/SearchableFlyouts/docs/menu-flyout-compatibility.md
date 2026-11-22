# MenuFlyout compatibility

Two controls provide search over different item contracts. SearchableMenuFlyout
inherits MenuFlyout and retains native menu items. SearchableItemsFlyout is
the data-driven alternative that follows WinUI menu conventions.
The public item model is `SearchableFlyoutItem`. This avoids allocating a
native control and template for every entry in a large command collection.

## Native searchable menus

SearchableMenuFlyout accepts existing MenuFlyoutItem, ToggleMenuFlyoutItem,
RadioMenuFlyoutItem, SplitMenuFlyoutItem, MenuFlyoutSubItem, and separator markup.
Click, ICommand, check state, radio groups, native submenu pointer handling,
and accelerators stay on the original controls. Search adds a real TextBox at
both root and submenu levels. Its keyboard editing and matching helpers are shared
with the virtualized renderer.

The presenter keeps the native backdrop, popup shadow, and item templates. The search field
uses a solid WinUI theme fill by default and supports acrylic through `UseAcrylicBackdrop`. Its root uses the
`NativeMenuSearch.IsEnabled` attached property from `JPSoftworks.DesktopToolkit.Controls.Primitives`. A custom items
panel pins the search container within the native scroll viewport. Search applies
a temporary animation layer to Visibility rather than replacing application bindings.
On close, all masks and temporary MenuSearchItem containers are removed. During
opening and while open, Items includes these search/empty-state containers.

The native path does not recycle command controls. The virtualized path keeps
a data-only item model and recycles ListView containers. SearchableDropDown exposes
its selection flyout through the read-only DropDownFlyout property.

The following comparison describes the virtualized implementation.

## What is shared and what differs

| Area | Built-in MenuFlyout | SearchableItemsFlyout |
| --- | --- | --- |
| Flyout integration | FlyoutBase attachment, placement, dismissal, lifecycle | Inherited through Flyout; ContextFlyout, Button.Flyout, ShowAt and lifecycle work |
| Item contract | MenuFlyoutItemBase controls in Items | SearchableFlyoutItem data in Items or ItemsSource |
| Commands | ICommand, CommandParameter, routed Click | ICommand, CommandParameter, root ItemInvoked |
| Rendering | MenuFlyoutPresenter and native menu item templates | Bounded ListView, ItemsStackPanel and recycled rows |
| Overflow | Grows to available work area, then scrolls | MaxListHeight defaults to 320 DIPs, with native popup constraints applied |
| Row appearance | Native theme resources and input-dependent density | Native menu state brushes, narrow padding, corners and 32-DIP compact rows |
| Icons and checks | IconElement and separate shared gutters | Reusable IconSource or Glyph, with separate gutters stable during filtering |
| Grouping | MenuFlyoutSeparator | IsSeparator; empty dividers are removed after filtering |
| Item state | Visibility, IsEnabled, command CanExecute | IsVisible, IsEnabled, command CanExecute |
| Keyboard | Native menu keyboard handling | Up/Down cycles through search and results; list typing and editing return to search; Home/End, Enter/Space, Escape and mirrored submenu arrows work |
| Cascading menus | Native pointer tracking and hover behavior | 400-ms hover delay, keyboard/click opening, per-level filters, mirrored placement |
| Automation | Native menu peers and command patterns | Menu/MenuItem peers with Invoke, Toggle and ExpandCollapse; list semantics for dropdowns |
| Filtering | No built-in field | Anchored field, case-insensitive text/keyword matching and coalesced updates |
| Backdrop and shadow | System acrylic backdrop and default popup shadow in this SDK | Acrylic by default with a solid WinUI fill through `UseAcrylicBackdrop="False"`. The shadow follows the visible surface |

The native baseline was checked against the restored Windows App SDK 2.6.5
`generic.xaml` and the project API metadata. The demo includes an unmodified
built-in MenuFlyout alongside the custom control.

The "Built-in overflow" demo contains 250 native commands. On this SDK,
the popup grows until constrained by the available monitor work area and
scrolls the remainder. Its bounds can extend past the owning app window.
The live check found all 250 menu controls realized; scrolling did not
recycle them. The virtualized control keeps a bounded set of row containers
for its 10,008-command data source.

## ComboBox layout comparison

The demo places a built-in ComboBox beside SearchableDropDown with the same
Low/Medium/High choices and a configured width of 200 DIPs. The custom
dropdown uses native ComboBox row padding, a 3-DIP selection radius, and
the native accent pill style. Explicit row surfaces avoid an additional
ListViewItemPresenter inset.

The stock popup aligns the selected row over the closed control. The custom
popup opens beside its anchor and keeps the filter at that edge. This
placement difference preserves the stable search field during filtering.
The UI comparison checks rendered item surfaces; closed-control automation
rectangles include different focus areas and are recorded separately.

At 200% display scaling, the final UI run measured matching item surfaces:

| Measurement in DIPs | Filtered | Built-in |
| --- | --- | --- |
| Configured control width | 200 | 200 |
| Item surface width | 188 | 188 |
| Item surface height | 33 | 33 |
| Selection corner radius | 3 | 3 |

## Migration example

```xml
<Button Content="Actions">
    <Button.Flyout>
        <controls:SearchableItemsFlyout>
            <controls:SearchableFlyoutItem Text="Open" Glyph="&#xE8E5;" />
            <controls:SearchableFlyoutItem IsSeparator="True" />
            <controls:SearchableFlyoutItem Text="Pin" IsCheckable="True" />
        </controls:SearchableItemsFlyout>
    </Button.Flyout>
</Button>
```

The attachment point stays the same. Convert native items to the data model
and move Click handlers to ICommand or ItemInvoked. Keep collections and
item changes on the UI thread. IconSource supports font, symbol, bitmap,
and path sources without sharing one IconElement between visual parents.

## Remaining limits

- The class derives from Flyout, so it cannot be assigned to a MenuFlyout-typed
  variable or accept native MenuFlyoutItemBase instances unchanged.
- MenuFlyoutPresenterStyle and native item ControlTemplates are not compatible
  with ListView row templates. Native theme resource overrides still apply.
- KeyboardAcceleratorText is display text. Register actual KeyboardAccelerators in the
  owning app. Native access-key scopes and mnemonic/type-ahead behavior are
  not reproduced.
- Radio groups, split menu commands, and native subitem pointer-intent tracking
  are not implemented. Hover uses a simple delay rather than native pointer
  trajectory logic.
- Compact density is fixed rather than switching automatically for touch.
- The reserved native popup bounds include transparent space after filtering.
  Clicks in that space dismiss the menu; they do not pass through to content
  underneath. The field remains at the opening edge.
- UI virtualization recycles rows. Data remains in memory and filtering still
  scans it on the UI thread; this is not remote/data virtualization.
- Automation pattern checks do not replace manual Narrator, touch, pen,
  High Contrast, mixed-DPI, or multi-monitor verification.

## References

- [MenuFlyout API](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.menuflyout)
- [MenuFlyoutPresenter API](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.menuflyoutpresenter)
- [Menus and context menus](https://learn.microsoft.com/windows/apps/design/controls/menus)
- [Custom automation peers](https://learn.microsoft.com/windows/apps/design/accessibility/custom-automation-peers)
