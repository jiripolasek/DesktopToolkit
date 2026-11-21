# Searchable Flyouts for WinUI 3

A C# WinUI 3 control library and demo for searchable menus and dropdowns.
The library targets .NET 10 LTS and .NET 11 in one NuGet package. Both targets support Windows 10
build 19041 or later with Windows App SDK 1.8.260317003 as their baseline.
The demo targets .NET 11 and Windows App SDK 2.6.5.
The package ID is `JPSoftworks.DesktopToolkit.Controls.SearchableFlyouts`. Import `JPSoftworks.DesktopToolkit.Controls` in XAML. It belongs to the same control family as Dock, PropertyGrid, and Charts.

## Source builds and NuGet consumption

Building this repository's solution requires the .NET 11 SDK selected by global.json. The solution builds both
library frameworks and the .NET 11 development apps; running the .NET 10 control tests also requires the .NET 10 runtime.

Consuming the NuGet package does not impose that repository SDK requirement. .NET 10 apps can use SDK 10
(verified with 10.0.404), and .NET 11 apps use SDK 11. WinUI 1.8 remains supported. The package handles its Windows
projection requirements automatically, without an app-side SDK projection override or a copy of this repository's
global.json. See [consumer requirements](src/README.md#requirements).

Installed-app users do not need an SDK. Their runtime requirements follow the host application's deployment.

## Build and run from source

```powershell
dotnet build ..\..\JPSoftworks.DesktopToolkit.slnx -p:Platform=x64
winapp run .\dev\JPSoftworks.DesktopToolkit.Controls.SearchableFlyouts.DevApp.csproj --arch x64 --detach
```

Open the solution in Visual Studio and select **JPSoftworks.DesktopToolkit.Controls.SearchableFlyouts.DevApp (Package)** for F5 deployment and debugging.
The project builds as MSIX. The Unpackaged profile launches the EXE without package identity and must
only be used with a build configured with WindowsPackageType=None. Using it with the default MSIX
build can fail in Windows App SDK deployment initialization with REGDB_E_CLASSNOTREG.
See [runtime initialization](https://learn.microsoft.com/windows/apps/windows-app-sdk/use-windows-app-sdk-run-time)
for the startup requirements of unpackaged applications.

Run the focused control cases on an x64 Windows desktop and verify the manifest and executable from an ARM64 solution build with these commands.

```powershell
dotnet run --project .\tests\SearchableFlyouts.ControlTests -f net10.0-windows10.0.19041 -p:Platform=x64
dotnet run --project .\tests\SearchableFlyouts.ControlTests -f net11.0-windows10.0.19041 -p:Platform=x64
.\scripts\Test-SolutionPlatform.ps1 -Configuration Release -Platform ARM64
```

## Library build and package files

The library project declares the supported frameworks and its five direct dependencies. The rest is separated by
purpose, so changing normal library settings does not require reading the packaging implementation.

| File | Purpose |
| --- | --- |
| src/JPSoftworks.DesktopToolkit.Controls.SearchableFlyouts.csproj | Frameworks, dependencies, and imports selecting the build policies |
| component.props | Component identity, version, and release metadata |
| ../../eng/WinUIPackage.props | WinUI library defaults, AOT analysis, package validation, README, XML documentation, and symbols |
| ../../Directory.Packages.props | Dependency versions and the minimum Windows SDK projection revision |
| src/pack/CompiledXaml.targets | Adds the compiled control XAML and consumer targets to each framework's package content |
| src/pack/WindowsSdkDependencies.targets | Generates Windows SDK dependency groups for .NET 10 consumers |
| src/pack/WindowsSdkDependencies.xslt | Rewrites only those dependency groups in the NuGet-generated manifest |
| src/buildTransitive/JPSoftworks.DesktopToolkit.Controls.SearchableFlyouts.targets | Shipped consumer logic that selects the projection and copies compiled XAML to app output |

A normal build compiles one library for .NET 10 and one for .NET 11. Packing then adds two things beyond standard
NuGet assembly packaging:

1. **Compiled XAML.** Consumers need the control XBF files at the same resource paths as the library. The package
   includes the files and a buildTransitive target for each framework. That target copies the resources into the
   consuming application's build and publish output.
2. **Windows SDK dependencies for .NET 10.** The library is compiled against Windows build 19041, but a consumer may
   target a newer Windows build. Each Windows build has its own Microsoft.Windows.SDK.NET.Ref projection package.
   The packing target asks the installed .NET SDK which stable projections meet the minimum revision, lets NuGet
   generate a base manifest, then transforms its dependency groups for those Windows targets. All groups share the
   same compiled library; they do not add more library target frameworks. The shipped target uses the matching group
   to select a sufficiently recent projection unless the consumer has explicitly chosen one.

The Windows SDK manifest transformation remains necessary for the supported .NET 10 SDK cold-restore path.
A standard-package experiment and the SDK-native revision-floor alternative are documented in
[the packaging investigation](docs/standard-packaging-investigation.md).

The files under src/pack run only while producing this package. The file under src/buildTransitive is shipped and
runs in consuming applications. These are separate responsibilities. The projection-manifest transformation is
component-specific packaging machinery; it is not required for every WinUI library.

Validate packaging with scripts/Test-Package.ps1. It restores the packed bytes into a standalone consumer, publishes
with Native AOT or trimming, and checks dependency selection and compiled resources. Merely building the library
does not exercise these packaging targets.

## Choose a flyout

| Control | Use it for | Items |
| --- | --- | --- |
| SearchableMenuFlyout | Ordinary application command menus | Native MenuFlyoutItemBase controls |
| SearchableItemsFlyout | Large data-driven command menus | SearchableFlyoutItem data and recycled rows |
| SearchableDropDown | A single selection from a searchable list | Strings, application objects, or SearchableFlyoutItem choices |

All three use the same token matching and filter editing behavior. The filter stays at the opening edge as the results shrink.

## SearchableMenuFlyout

Replace the outer MenuFlyout element and keep the native items and their handlers:

```xml
<Button Content="Actions" xmlns:controls="using:JPSoftworks.DesktopToolkit.Controls">
    <Button.Flyout>
        <controls:SearchableMenuFlyout>
            <MenuFlyoutItem Text="Open file" Click="OnOpenFile"
                controls:SearchableMenuFlyout.Keywords="browse document" />
            <ToggleMenuFlyoutItem Text="Pin" />
            <MenuFlyoutSeparator />
            <MenuFlyoutSubItem Text="Export as">
                <MenuFlyoutItem Text="PDF document" Click="OnExportPdf" />
                <MenuFlyoutItem Text="PNG image" Click="OnExportPng" />
            </MenuFlyoutSubItem>
        </controls:SearchableMenuFlyout>
    </Button.Flyout>
</Button>
```

This type derives from MenuFlyout. Native Click handlers, ICommand, toggles, radio
items, split items, keyboard accelerators, and submenu pointer handling remain
owned by WinUI. Search is independent at each level. Keywords is an attached property.
Filtering adds a temporary Visibility animation layer so existing Visibility bindings
and local values survive filtering and dismissal. No animated list transitions are used.

The search and empty-state containers are MenuSearchItem instances inserted while
open and removed on close. Code inspecting Items during an open menu should exclude
these containers. The Opening handler can clear and repopulate Items for the current context. This makes the text box part of native menu automation; a text box
placed only in the presenter template is omitted by the native menu automation peer.
A small layout panel keeps the field at the opening edge while the command rows scroll.

FilterText controls the root query. FilterBoxPlacement, FilterPlaceholderText, FilterBoxAutomationName,
NoResultsText, FlyoutWidth, MaxListHeight, ResetFilterOnOpen, IsResultCountVisible, Refresh(),
and FilterChanged configure the searchable surface. The defaults match the virtualized
menu: 280-DIP width, 320-DIP maximum command viewport, and a borderless 32-DIP field.
Native item density still follows WinUI's input-mode behavior.

Native commands are not virtualized. Use SearchableItemsFlyout for large collections.
Custom presenter styles must retain the search template and NativeMenuItemsPanel;
base them on the existing MenuFlyoutPresenterStyle or merge MenuStyles.xaml.
An upward-opening root menu keeps its popup window steady and draws the backdrop and
shadow on the visible menu surface. The sticky search field uses a solid WinUI theme
fill by default. Set `UseAcrylicBackdrop="True"` to give the search field an acrylic
backdrop at every menu level.

## SearchableItemsFlyout

Reference the SearchableFlyouts library project or install its NuGet package.
Import the JPSoftworks.DesktopToolkit.Controls namespace.
The flyout works with `FrameworkElement.ContextFlyout`, `Button.Flyout`,
and `ShowAt`.

```xml
<Border xmlns:controls="using:JPSoftworks.DesktopToolkit.Controls">
    <Border.ContextFlyout>
        <controls:SearchableItemsFlyout>
            <controls:SearchableItemsFlyout.Items>
                <controls:SearchableFlyoutItem Text="Open file" Keywords="browse document" />
                <controls:SearchableFlyoutItem Text="Copy path" Keywords="clipboard location" />
            </controls:SearchableItemsFlyout.Items>
        </controls:SearchableItemsFlyout>
    </Border.ContextFlyout>
</Border>
```

Bind `ItemsSource` to a collection of `SearchableFlyoutItem`, or populate `Items`.
Items support text, keywords, icon glyphs and `IconSource`, shortcut hints,
enabled/visible state, checkable state, `ICommand`, command parameters, tags,
separators, and child items. `Items` is also the default XAML content property.

The compact menu defaults use 32-DIP rows, a borderless search field, a
280-DIP menu width, and no visible heading or result counter.
`FilterBoxAutomationName` supplies the field's accessible name. `FilterPlaceholderText`
and `NoResultsText` customize its visible prompts.
Set `IsResultCountVisible="True"` to opt into the footer.

### Virtualized menu behavior and compatibility

`SearchableItemsFlyout` uses acrylic for the menu and its submenus by default.
Set `UseAcrylicBackdrop="False"` for a solid WinUI theme fill. `SearchableDropDown`
defaults to a solid fill and supports acrylic through `UseAcrylicBackdrop="True"`.
The virtualized surface uses desktop acrylic on Windows App SDK 2.x and the themed
in-app acrylic brush on 1.8.
Check and icon columns align across an entire level and keep their width as
filtering narrows the results. Checks and icons can appear on the same item.
Explicit row surfaces control the insets, so ListView presenter padding does
not add a second gutter. Item padding leaves room for the overlay scrollbar.
Dropdowns use the native ComboBox row padding, rounded selection, and accent pill.

Set `IsSeparator="True"` for a noninteractive group divider. Filtering removes
leading, trailing, and consecutive separators. `IsVisible="False"` hides an
item from both matching and navigation. Counts exclude separators.

The list exposes Menu/MenuItem automation semantics with Invoke, Toggle, or
ExpandCollapse patterns. Dropdowns retain list selection semantics.

This is a replacement at `FlyoutBase` attachment points, including
`ContextFlyout`, `Button.Flyout`, and `ShowAt`. It keeps a data model for UI
virtualization; existing native `MenuFlyoutItem` markup and routed `Click`
handlers require migration to `SearchableFlyoutItem` and `ICommand`/`ItemInvoked`.
See [the compatibility comparison](docs/menu-flyout-compatibility.md) for the
differences between both implementations and their limits.

### Anchor behavior

`FilterBoxPlacement` defaults to `Auto`. The actual native popup placement
determines the field position: a menu below the anchor has search at the
top; one above has search at the bottom. Submenus use their opening edge.

The popup reserves its layout space once per opening. The visible surface
shrinks away from the anchor as matches disappear, keeping the field's
screen coordinates fixed. Clicking unused space dismisses the flyout.
The popup can extend beyond the app window, like a native menu. The list
remains constrained and scrollable near the monitor work-area boundary.
Explicit `Top` and `Bottom` placement overrides remain available.

### Submenus

```csharp
var export = new SearchableFlyoutItem { Text = "Export as" };
export.Items.Add(new SearchableFlyoutItem { Text = "PDF document" });
export.Items.Add(new SearchableFlyoutItem { Text = "PNG image" });
menu.Items.Add(export);
```

Click a group or press Right from its row to open it. Each level has its own
filter and virtualized list. Filtering applies to the current level.
Left from a child list returns to the parent. A leaf invocation closes the
chain, executes its command, and raises `ItemInvoked` on the root menu.
Mouse hover opens a submenu after 400 ms without taking search focus. Moving
to a sibling command closes the child after the same delay. In right-to-left
layouts, placement, chevrons, and Left/Right navigation are mirrored.

### Input and updates

- Matching ignores case. Every whitespace-separated word must match the
  item's text or keywords; source order stays unchanged.
- Typing is coalesced over 70 ms. Arrow navigation and Enter flush pending
  filtering. Up/Down navigates, Home/End jumps, Enter invokes, and Space
  invokes from the list. Disabled commands are skipped.
- Up/Down in search enters the first/last enabled result. Moving past either
  end of the results returns to search, preserving the query and caret.
- Typing in the list moves focus to search and inserts the received text.
  Backspace/Delete edit the query, Ctrl+A selects it, and Ctrl+V pastes into it.
  These behaviors also apply to dropdowns and submenus.
- Item and content transitions are disabled so filtering updates rows immediately.
- Ctrl+F returns to search. Escape dismisses the current level.
- Search resets on reopening unless `ResetFilterOnOpen="False"`.
- Shortcut text is a hint; register actual accelerators in the owning app.
- Observable collection and item changes are observed while open.
  Call `Refresh()` after changing a plain collection in place.
  Update collections and item properties on the UI thread.

## SearchableDropDown

The dropdown composes the same flyout with a native `DropDownButton`.
It exposes ComboBox automation semantics, expand/collapse, and a read-only
UI Automation value. Its SelectionChanged event uses WinUI SelectionChangedEventArgs.

```csharp
var picker = new SearchableDropDown
{
    ItemsSource = new[] { "Low", "Medium", "High" },
    SelectedItem = "Medium",
    PlaceholderText = "Select sensitivity",
};

picker.SelectionChanged += (_, args) =>
{
    if (args.AddedItems.FirstOrDefault() is string value) { ApplySensitivity(value); }
};
```

`ItemsSource` accepts any `IEnumerable`; `Items` is an
`ObservableCollection<object>`. Selected items are the original source
objects. Bind `SelectedItem`, `SelectedIndex`, and `IsDropDownOpen` as
needed.

Strings and enums display directly. For custom objects, provide a selector:

```csharp
picker.ItemTextSelector = value => ((Project)value).Name;
picker.ItemsSource = projects;
```

`ItemTemplate` displays each original item in the open results list. A typed XAML
`DataTemplate` can present other properties while `ItemTextSelector` supplies the
search label, the closed selection text, and the default accessible name.

Use `SearchableFlyoutItem` choices to include icons and search keywords. The glyph
appears in both the dropdown and the selected value:

```xml
<controls:SearchableDropDown SelectedIndex="0">
    <controls:SearchableDropDown.Items>
        <controls:SearchableFlyoutItem Text="Documents" Glyph="&#xE8A5;" Keywords="text files" />
        <controls:SearchableFlyoutItem Text="Pictures" Glyph="&#xEB9F;" Keywords="photos images" />
    </controls:SearchableDropDown.Items>
</controls:SearchableDropDown>
```

With the dropdown closed, Up and Down select the previous or next choice and
wrap at either end. F4 opens the dropdown. Click a choice or press Enter to
commit the highlighted result. Escape cancels the pending selection.
Observable source changes refresh the choices. Call `Refresh()` for plain
collection or label changes.

`FilterPlaceholderText` changes the search prompt. `DropDownFlyout`
exposes further sizing and styling options. Its width follows the control
when opening. Choices do not execute menu commands.

## Virtualization and verification

`SearchableItemsFlyout` and `SearchableDropDown` use a bounded `ListView`
with an `ItemsStackPanel` and
recycled containers. Filtering replaces the view once rather than sending
a notification per result. `FilteredItemCount` and
`RealizedContainerCount` expose diagnostics.

This is UI virtualization: source data stays in memory and filtering scans
it on the UI thread. Remote or unbounded data sources are outside the current scope.

The demo includes 10,008 commands, a three-choice dropdown, a library dropdown
with glyph and `IconSource` icons, 10,000 workspace choices, submenus,
above/below anchors, RTL, light/dark previews, and a built-in menu reference.
The "Searchable native" button demonstrates native commands, toggles, radio groups,
split items, keyboard accelerators, and nested searchable submenus.
The "Built-in overflow" button opens 250 native commands for comparison.
The filtered and built-in sensitivity pickers use the same choices and width.

```powershell
.\scripts\Test-UI.ps1 -Target local -AppPid <process-id> -AppScope local
```

The UI batch checks filter coordinates while matches shrink, selection and
cancellation, keyboard input, commands, menu automation patterns, separators,
hover submenus, RTL navigation, virtualization, and theme captures. Results
and screenshots are saved under `artifacts`.

Run the native interaction checks with:

```powershell
.\scripts\Test-NativeSearch.ps1 -Target local -AppPid <PID> -AppScope local
```

## Accessibility and distribution

See [the package quickstart](src/README.md) for integration and localization.
See the [manual flyout scenarios](docs/accessibility-and-release.md#manual-flyout-scenarios) for Narrator, keyboard navigation, and focus restoration checks.

Run the dedicated checks with:

```powershell
.\scripts\Test-A11y.ps1 -Target local -AppPid <PID> -AppScope local
```

Build the local preview package with:

```powershell
dotnet pack .\src\JPSoftworks.DesktopToolkit.Controls.SearchableFlyouts.csproj -c Release -p:Platform=x64
```

Packages are written to the repository-level artifacts/packages directory. Nothing is published automatically.

Validate the local package in an isolated consumer for each .NET target and publish mode.

```powershell
$candidate = (Get-Item ..\..\artifacts\packages\JPSoftworks.DesktopToolkit.Controls.SearchableFlyouts.0.1.0-preview.1.nupkg).FullName
foreach ($tfm in 'net10.0-windows10.0.19041', 'net11.0-windows10.0.19041') {
    .\scripts\Test-Package.ps1 -PackagePath $candidate -TargetFramework $tfm -Flavor Aot
    .\scripts\Test-Package.ps1 -PackagePath $candidate -TargetFramework $tfm -Flavor Trimmed
}
```

Both modes publish with warnings treated as errors. The script reports the publish, manifest, and staging paths
for launching with winapp run, and records the exact package and binary hashes. No broad linker roots are required.

To verify the older consumer SDK instead of inheriting the repository's .NET 11 SDK, pass
-DotNetSdkVersion 10.0.404. The script pins that installed SDK only in the generated consumer, publishes from that
directory, and records the actual SDK version. Combine it with -WindowsAppSdkVersion 1.8.260317003 and
-ColdRestore to exercise the WinUI 1.8 first-restore path. No SDK is installed or changed on the machine.
