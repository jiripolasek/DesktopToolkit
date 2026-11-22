# Searchable Flyouts accessibility and test coverage

The package is JPSoftworks.DesktopToolkit.Controls.SearchableFlyouts.
Its public namespace is JPSoftworks.DesktopToolkit.Controls. The library targets .NET 8, .NET 10, and .NET 11
and Windows SDK 19041 with Windows App SDK 1.8.260317003. The sample targets .NET 11 with Windows App SDK 2.6.5.
Both require Windows 10 build 19041 or later.

## Changes from the prototype

- Extracted the reusable controls and XAML into a separate library with assembly-scoped resource URIs.
- Added debounced automation notifications for search results, including a meaningful empty result description.
  Messages use CurrentThenMostRecent so newer counts do not queue behind every keystroke.
- Added keyboard guidance and current result status to each filter's automation properties.
- Added custom item automation names, help text, IDs, and positions that exclude separators.
- Added ToggleState and ExpandCollapseState property-change notifications to virtualized menu peers.
- Kept native menu automation patterns and read-only dropdown Value/ExpandCollapse semantics.
- Kept native TextBox editing with a borderless style and an opaque focus fill for caret visibility.
- Restored the I-beam in windowed popups, scoped to the filter input island and released on exit or unload.
- Kept native disabled foreground brushes on rows.
- Observed dropdown source-item property changes and updated the selected value, icons, filtering,
  accessible descriptions, and enabled state. Observers and timers detach when unloaded.
- Added SearchableFlyoutStrings so the host can supply localized accessible labels and result formats.
- Generated AOT-safe WinRT bindings for control overrides and automation peers, with targeted projected-type metadata.
- Added a native accelerator default-action fallback after filtering, preserving native matching and original Invoked handlers.
- Restored the actual parent row when Escape dismisses an automation-opened submenu.

## Integration contract

All control construction, collection mutation, and menu item changes belong on the UI thread.
Command.CanExecuteChanged is marshaled to the control dispatcher.
SearchableDropDown accepts arbitrary objects; ItemTextSelector supplies their labels.
Property-change notifications are observed for loaded dropdown choices. Changes to non-observable
collections or items require Refresh(). Avoid sharing mutable UI IconSource objects across UI threads.

Give each trigger or dropdown an accessible name describing its purpose. A selected value is not a label.
AutomationProperties.LabeledBy can connect an external TextBlock to the dropdown.
Set SearchableFlyoutItem.AutomationName when the spoken name should differ from its visible text,
and AutomationHelpText for useful context. Icons and checks are decorative children of the item peer.
The virtualized path exposes Invoke for commands, Toggle for checkable leaves, and ExpandCollapse
for submenus. It deliberately does not expose SelectionItem for command menus. Dropdowns retain
ListView selection semantics.

The packaged XML documentation describes each SearchableFlyoutStrings format argument. Assign a new immutable
instance when changing language. User-facing prompts also remain configurable through FilterBoxAutomationName,
FilterPlaceholderText, NoResultsText, and the dropdown's PlaceholderText.

Search filters each submenu independently. Native keyboard and pointer behavior stays with WinUI;
virtualized menu navigation is implemented by the component. KeyboardAcceleratorText on virtualized items is a
visual/accessibility hint, not a registered accelerator.

The native implementation temporarily inserts MenuSearchItem objects into each Items collection.
Exclude those helpers when inspecting Items while open. Populate native item collections in the Opening event or while the flyout is closed.
Existing Visibility bindings are preserved through a temporary filtering animation layer; applications
with their own Visibility animations need to verify that composition. Presenter customizations must
retain the supplied parts and NativeMenuItemsPanel. These restrictions prevent claiming universal
MenuFlyout binary or behavioral compatibility.

## Verification

Automated scripts exercise the running WinUI app through Windows UI Automation:

- scripts/Test-UI.ps1 covers virtualized menus, dropdowns, keyboard editing, icons, state, geometry,
  menu patterns, native reference controls, light/dark captures, and large collections.
- scripts/Test-NativeSearch.ps1 covers native handlers, commands, toggles, radio/split items,
  nested search, bound visibility, native shortcuts, scroll anchoring, and light/dark captures.
- scripts/Test-A11y.ps1 covers external labels, read-only values, choice descriptions, live labels,
  custom menu names, keyboard guidance, empty states, toggle state, nested labels, and focus restoration.

Run Test-UI first on a fresh demo launch. It checks the initial 10,008 commands, Medium sensitivity,
and Documents selection before changing them. Each UI script accepts `-OutputDirectory` for its reports
and screenshots. The default is the repository's artifacts directory. A UIA result does not prove what Narrator speaks,
actual contrast-theme rendering, or Windows text-scale behavior. Record the exact app build and target
with each run. Keep infrastructure failures separate from control assertion failures.

## Trimming and Native AOT gate

Trimming and Native AOT are required supported configurations. The library sets IsAotCompatible and
keeps CsWinRT level 3 diagnostics enabled. It uses partial WinRT classes and targeted
DynamicWindowsRuntimeCast metadata, without TrimmerRootAssembly entries or IL warning suppressions.

Run scripts/Test-Package.ps1 -Flavor Aot and -Flavor Trimmed. Each run creates a unique local package
and a separate consumer with no project reference, then publishes with warnings treated as errors.
Staging defaults to the temporary directory and can be changed with OutputDirectory. The default
consumer is .NET 10 x64 with Windows App SDK 2.6.5. TargetFramework, WindowsAppSdkVersion, and Architecture are configurable.
The report records package and executable hashes, compiled XAML resources, and runtime payload checks.
The AOT output must contain neither the managed app assembly nor CoreCLR; a native PE header alone
cannot distinguish Native AOT from a normal .NET apphost. Run the UI batches against the published app.

## Verified on 2026-11-17

The following matrix covers preview.7:

| Consumer | Publish | Runtime checks |
| --- | --- | --- |
| .NET 10 x64 Native AOT | Zero warnings with warnings as errors; no managed app DLL or CoreCLR | 15 native menu and 10 accessibility cases passed |
| .NET 10 x64 trimmed JIT | Zero warnings with warnings as errors | 15 native menu and 10 accessibility cases passed |
| .NET 11 x64 Native AOT | Zero warnings with warnings as errors; no managed app DLL or CoreCLR | Publish check only for the final revision |

The consumers reference a packed NuGet and include all five compiled XAML resources.
Reports and package hashes are under artifacts/final-validation. The placeholder uses
TextFillColorTertiaryBrush, which retains system text colors in High Contrast, and the
shared filter preserves native text editing and restores the I-beam in windowed popups.
Actual High Contrast rendering remains a manual check.

Preview.8 replaces the filter subclass with a native TextBox and shared editing helpers.
Its borderless style uses an opaque focused fill for caret visibility, while the menu retains acrylic.
The I-beam is applied to the filter input island and restored on exit or unload.
The final .NET 10 x64 Native AOT consumer published with trimming and warnings as errors;
all 15 native-menu and 10 accessibility cases passed. Real pointer checks confirmed the I-beam
on first entry in both variants and arrow restoration over rows. The .NET 11 Debug build
also passed without warnings. Reports are under artifacts/filter-feedback-validation.

The broader 27-case virtualized-menu/dropdown batch passed on an earlier AOT build
(artifacts/ui-021a2f991db04a41939a92db2dda67b4). Its final rerun was interrupted and is not
reported as a preview.8 pass.

## Verified on 2026-11-18

Preview.9 uses `JPSoftworks.DesktopToolkit.Controls.SearchableFlyouts` as its package ID, assembly name, and XAML namespace.
The .NET 11 demo and its project reference built for x64 and ARM64 with warnings treated as errors.
Separate .NET 10 x64 applications consumed the packed library and published with Native AOT and trimmed JIT.
Both contained the five compiled XAML resources. The NuGet includes XML documentation for the public contract.
In Windows Sandbox, the x64 Native AOT app passed 30 virtualized menu and dropdown checks, 15 native menu checks,
and 10 UI Automation accessibility checks. The 10,008-command menu realized 14 containers, and the 10,000-choice
dropdown realized 11. ARM64 verification covered compilation.

A separate WinUI app loaded all three controls alongside `DockWorkspaceView`, `PropertyGrid`, and `LiveAreaGraph`
from the Dock, PropertyGrid, and Charts packages. It merged the Dock and PropertyGrid theme dictionaries specified
by those packages. The same app verified nested items declared in XAML and original object identity in the dropdown's
`SelectionChangedEventArgs` when selecting, replacing, and clearing a choice.

## Preview.12 compatibility

Preview.12 contained one architecture-neutral assembly for `net8.0-windows10.0.17763`, its XML documentation,
and the five compiled XAML resources. Both the native and virtualized menus use a themed brush on
Windows App SDK 1.8 and a bounded desktop backdrop on 2.x.

The following unpackaged x64 apps referenced this exact NuGet on a Windows 11 machine.
The Windows SDK column identifies the target used to compile each app. The Windows App SDK column
identifies the NuGet reference. These apps used the installed x64 Windows App Runtime.

| Consumer | Windows SDK target | Windows App SDK | Execution |
| --- | --- | --- | --- |
| .NET 8 | 17763 | 1.8.260317003 | All six control cases passed in trimmed JIT and Native AOT |
| .NET 8 | 19041 | 2.6.5 | All six passed in JIT |
| .NET 9 | 19041 | 2.6.5 | All six passed in JIT |
| .NET 10 | 19041 | 1.8.260317003 | All six passed in JIT |
| .NET 10 | 26100 | 2.6.5 | All six passed in ten consecutive Native AOT runs |
| .NET 11 | 26100 | 2.6.5 | All six passed in JIT |

`EqualChoicesKeepTheirSourceIndex` checks source identity when equal entries are moved and replaced.
`RefreshPreservesHighlightedChoice` covers a pending selection during collection updates.
`ChildChangesKeepSubmenuOpen` covers updates inside an expanded submenu. `AppendingChoicesScalesLinearly`
measured 301 text-selector calls for 300 additions and an immediate selection, down from 45,151.
`OpeningCanRebuildNativeMenu` replaces the native items in the Opening handler on successive openings.
`MenuUsesRuntimeBackdrop` checks the visual selected by each runtime.

The .NET 11 solution builds passed `SolutionTargetsRequestedArchitecture` for Debug and Release on x64
and ARM64. Each check reads the manifest and the executable's PE machine type. A separate .NET 10
package consumer cross-published to an ARM64 Native AOT executable. NuGet rejected a plain .NET 8
project and a .NET 7 Windows project.

A separate .NET 10 app loaded the candidate alongside the Dock, PropertyGrid, and Charts NuGets.
Its XAML constructed all three flyout controls, a nested command, `DockWorkspaceView`, `PropertyGrid`,
and `LiveAreaGraph`. Its dropdown reported the original source objects when selecting, replacing, and clearing a choice.

In Windows Sandbox, the .NET 10 x64 Native AOT consumer passed all 30 menu and dropdown checks on a fresh
launch and all 10 accessibility checks. The native menu suite passed 13 of 15 on the first run. Both
keyboard submenu checks passed on replay. The 10,008-command menu and the 10,000-choice dropdown each
realized 11 containers. The package hash and individual results are in `artifacts/review-fixes/preview.12-verification.json`.

## Preview.13 compatibility

Preview.13 targeted `net8.0-windows10.0.19041`. It raised the minimum OS to Windows 10 version 2004 (build 19041).

With the .NET 8 SDK, NuGet restored preview.13 for Windows target 19041 and returned `NU1202` for
targets 17763 and 18362. On Windows 11 x64, a 19041-targeted Native AOT consumer passed all six
control tests using Windows App SDK 1.8.260317003. A trimmed consumer published all five compiled XAML
resources. The generated demo manifest declares 19041 for both Windows device families.

## Preview.16 compatibility

Preview.16 targets `net10.0-windows10.0.19041` and `net11.0-windows10.0.19041`.
Each framework has its own assembly, XML documentation, resource index, source XAML, and compiled XAML.
The SDK checks binary API compatibility between the two assemblies during pack.

The following x64 package consumers passed all six control tests in both Native AOT and trimmed JIT
on Windows 11 build 26300. NuGet selected the matching assembly, resource index, and build targets
for each framework. All five compiled XAML files matched the corresponding files in the package.

| Target | .NET SDK | Windows App SDK |
| --- | --- | --- |
| .NET 10 | 10.0.404 | 1.8.260317003 |
| .NET 11 | 11.0.100 | 2.6.5 |

Preview.16 required an app-side projection override for trimmed JIT on SDK 10.0.404 because
Windows SDK projection 10.0.19041.57 emitted IL2081. Preview.17 resolves this in the package.

Strict API compatibility validation passed. Both assemblies declare AOT and trimming compatibility
and have the same 138 XML documentation entries. A .NET 10 app also loaded SearchableFlyouts
alongside Dock, PropertyGrid, and Charts and exercised dropdown selection. NuGet rejected .NET 9
and Windows target 18362 with NU1202. The Release ARM64 cross-build passed manifest and binary
architecture checks. Runtime coverage on ARM64 and on Windows build 19041 remains open.

## Preview.17 Windows SDK projection

On .NET 10, each Windows target has a NuGet dependency on the matching revision 87 targeting pack.
This makes the projection available on the first restore. The .NET SDK still selects the assemblies
and analyzers. Newer SDK defaults and explicit app choices take precedence.

On SDK 10.0.404, the trimmed x64 sample published from an empty NuGet cache with warnings as errors,
all five compiled XAML resources, and the default app settings. Its dependency manifest recorded
10.0.19041.87. All six control tests passed for each Windows target (19041, 20348, 22000, 22621,
and 26100) on the same Windows 11 x64 host. An explicit 10.0.19041.57 pin was honored in Native AOT.

## Preview.18 library targets

The library compiles its DLL and XAML once for .NET 10 and once for .NET 11, both targeting
Windows SDK 19041. The NuGet manifest retains the Windows-specific .NET 10 dependency groups
so each app can restore its matching Windows SDK projection.

A trimmed x64 app targeting Windows 26100 on SDK 10.0.404 published from an empty cache with
warnings as errors. It used the `net10.0-windows10.0.19041` library, the 10.0.26100.87 runtime
projection, and all five compiled XAML resources.

## Preview.19 projection selection

Pack generates the .NET 10 dependency groups from the packing SDK's Windows projection catalog.
It includes stable entries for Windows 19041 and later with projection revision 87 or later.
The app's Windows target selects the dependency on its first restore. Repacking with a newer SDK
can add a Windows target or update a projection revision while both library assemblies target 19041.

## Preview.20 dropdown item templates

All seven control tests passed on x64 for .NET 10 and .NET 11, including switching the template
on an open, filtered dropdown and invoking the original source item. Three dropdown accessibility
checks passed in Windows Sandbox. The .NET 10 x64 sample published with Native AOT from a cold
restore of the package, using projection 10.0.19041.87 and all five compiled XAML resources.

## Verify the current preview

Use the package script to verify the archive hash, selected assembly, runtime projection, and XAML.

```powershell
$candidate = (Get-Item ../../artifacts/packages/JPSoftworks.DesktopToolkit.Controls.SearchableFlyouts.0.1.0-preview.2.nupkg).FullName
./scripts/Test-Package.ps1 -PackagePath $candidate -Flavor Aot -TargetFramework net10.0-windows10.0.19041.0 -WindowsAppSdkVersion 1.8.260317003
./scripts/Test-Package.ps1 -PackagePath $candidate -Flavor Aot -TargetFramework net11.0-windows10.0.26100.0
./scripts/Test-Package.ps1 -PackagePath $candidate -Flavor Trimmed -TargetFramework net10.0-windows10.0.26100.0 -WindowsAppSdkVersion 1.8.260317003 -ColdRestore
```

## Manual flyout scenarios

- Open a menu with the keyboard and filter it down to one result, then none. With Narrator, listen for the result count, empty state, and command name. Each should be spoken once, with the command name audible in full.
- Move through enabled, disabled, and checkable commands. Open a nested menu and press Escape. Check that Narrator announces the toggle state and that focus returns to the parent item.
- Open a dropdown, change the highlighted choice, and cancel. Repeat and commit the choice. Check the selected value and the focus returned to the trigger in each case.

The [toolkit release guide](../../../eng/RELEASING.md#accessibility-and-platform-verification) covers display and platform verification for all components.

## References

- [Custom automation peers](https://learn.microsoft.com/windows/apps/design/accessibility/custom-automation-peers)
- [ComboBox automation contract](https://learn.microsoft.com/windows/win32/winauto/uiauto-supportcomboboxcontroltype)
- [Automation notification processing](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.automation.peers.automationnotificationprocessing)
- [Keyboard accessibility](https://learn.microsoft.com/windows/apps/design/accessibility/keyboard-accessibility)
- [High Contrast themes](https://learn.microsoft.com/windows/apps/design/accessibility/high-contrast-themes)
