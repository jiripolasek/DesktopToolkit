# Searchable Flyouts

Add search to WinUI 3 menus, item flyouts, and dropdowns.

## Requirements

- Windows 10 version 2004 (build 19041) or later
- A WinUI 3 app targeting .NET 8 or later
- Windows App SDK 1.8.260317003 or later

The package includes one DLL and one set of compiled XAML per .NET version, both built for Windows SDK 19041.
The minimum WinUI package is `Microsoft.WindowsAppSDK.WinUI` 1.8.260224000, included in Windows App SDK 1.8.260317003.

Use the SDK appropriate for your app's target framework. The package ships assemblies for .NET 8, .NET 10, and .NET 11.
Consumer checks verified SDK 8.0.428 with Windows App SDK 2.6.5 and SDK 10.0.404 with Windows App SDK 1.8.260317003,
including trimmed and Native AOT publishes from a cold restore.

Building the library and development solution from source requires the .NET 11 SDK.

People running your finished application do not need a .NET SDK. Runtime prerequisites depend on how you deploy
the host application. Self-contained deployment includes the .NET runtime; framework-dependent deployment requires
the matching runtime. Windows App SDK initialization and deployment remain the host application's responsibility.

## Installation

For a local preview, run `./eng/Build.ps1` from the toolkit checkout and add `artifacts/packages` to your NuGet sources.
Keep your regular sources enabled for dependencies.

Run this command from your WinUI app project directory.

```powershell
dotnet add package JPSoftworks.DesktopToolkit.Controls.SearchableFlyouts --version 0.1.0-preview.2
```

## Usage

- `SearchableMenuFlyout` adds a filter to native `MenuFlyout` items and preserves their handlers.
- `SearchableItemsFlyout` displays `SearchableFlyoutItem` data and recycles rows for large lists.
- `SearchableDropDown` lets users filter a list and commit a single selection.

Import `using:JPSoftworks.DesktopToolkit.Controls` in XAML. The host app provides WinUI initialization and XamlControlsResources.

Command menus use acrylic by default. `SearchableDropDown` uses a solid WinUI theme fill by default
and supports acrylic through `UseAcrylicBackdrop="True"`.

### Native commands

```xml
<Button Content="Actions" xmlns:menus="using:JPSoftworks.DesktopToolkit.Controls">
    <Button.Flyout>
        <menus:SearchableMenuFlyout FilterBoxAutomationName="Find an action">
            <MenuFlyoutItem Text="Save" Click="OnSave" />
            <ToggleMenuFlyoutItem Text="Favorite" />
        </menus:SearchableMenuFlyout>
    </Button.Flyout>
</Button>
```

### Large collections

```xml
<menus:SearchableItemsFlyout xmlns:menus="using:JPSoftworks.DesktopToolkit.Controls"
    ItemsSource="{x:Bind ViewModel.Commands, Mode=OneWay}" />
```

Populate Commands with SearchableFlyoutItem objects. Use ICommand or ItemInvoked for execution.
KeyboardAcceleratorText is a display hint; the host registers shortcuts for virtualized commands.

### Single selection

```xml
<menus:SearchableDropDown xmlns:menus="using:JPSoftworks.DesktopToolkit.Controls"
    xmlns:models="using:MyApp.Models"
    ItemsSource="{x:Bind ViewModel.Projects}"
    SelectedItem="{x:Bind ViewModel.SelectedProject, Mode=TwoWay}"
    PlaceholderText="Choose a project" FilterPlaceholderText="Filter projects">
    <menus:SearchableDropDown.ItemTemplate>
        <DataTemplate x:DataType="models:Project">
            <StackPanel>
                <TextBlock Text="{x:Bind Name}" />
                <TextBlock Text="{x:Bind Folder}" Style="{StaticResource CaptionTextBlockStyle}" />
            </StackPanel>
        </DataTemplate>
    </menus:SearchableDropDown.ItemTemplate>
</menus:SearchableDropDown>
```

`ItemTemplate` receives each original project in the open list. Set `ItemTextSelector` to
`item => ((Project)item).Name` to use its name for filtering, the closed selection, and the default automation name.
SelectionChanged uses WinUI's SelectionChangedEventArgs. AddedItems and RemovedItems contain the original choices.
For commands, SearchableItemsFlyout raises ItemInvoked with the original model in InvokedItem.

## Accessibility and localization

Give each menu trigger and dropdown a meaningful AutomationProperties.Name or LabeledBy.
SearchableFlyoutItem accepts AutomationName, AutomationHelpText, and AutomationId.
Search updates expose current status and send coalesced automation notifications without moving focus.
Arrow keys move between search and results; typing while a result is focused returns to search.

`FilterBoxAutomationName`, `FilterPlaceholderText`, and `NoResultsText` customize the filter on both flyout types and the dropdown.
The dropdown's `PlaceholderText` supplies a hint for an empty selection. Set `PlaceholderText=""` or `FilterPlaceholderText=""` to hide that hint.
Set the property to null or call `ClearValue` to restore its style or localized default. Filter hints are inherited by submenus.
Assign a `SearchableFlyoutStrings` instance from your application's localized resources to customize result messages,
list names, submenu labels, and keyboard help. Blank properties and invalid formats use the next available resource,
with embedded English as the fallback.
`MatchingCommandCountFormat` and `MatchingChoiceCountFormat` each cover all counts.
The first preview's singular and plural properties still accept explicit overrides and are deprecated.
A submenu without visible text uses its item's accessible name when available, or the parent filter's name.
Its format strings use the current culture and standard .NET format placeholders.

## Contracts and limits

Use the controls and mutate item collections on the UI thread. Filtering scans in-memory data.
The virtualized demo exercises 10,008 commands and 10,000 choices; no remote paging is provided.
The library supports trimming and Native AOT through generated WinRT bindings.
No TrimmerRootAssembly entry or application-level linker descriptor is required for these controls.
The native path does not virtualize. It temporarily inserts MenuSearchItem containers into Items while open.
Populate or rebuild native items in the Opening event or while the flyout is closed. Exclude the helper rows when inspecting Items during display.
Base custom presenter styles on the supplied MenuFlyoutPresenterStyle to retain its search field and scrolling behavior.

## Trimming and Native AOT

Enable PublishTrimmed or PublishAot in the host. AllowUnsafeBlocks is needed for generated WinRT bindings.
Keep custom WinRT-derived classes and WinRT interface implementations partial so CsWinRT can generate their bindings.
Use x:Bind and typed selectors for application data; the controls do not use reflection to discover properties.

```xml
<PropertyGroup>
    <PublishAot>true</PublishAot>
    <PublishTrimmed>true</PublishTrimmed>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <CsWinRTAotWarningLevel>3</CsWinRTAotWarningLevel>
</PropertyGroup>
```

If a trimmed .NET 10 publish reports `IL2081` in Windows SDK projection code,
add this property to your app's project before restore.

```xml
<PropertyGroup>
  <WindowsSdkPackageMinimumRevision>87</WindowsSdkPackageMinimumRevision>
</PropertyGroup>
```

This sets the minimum projection revision for your app's Windows target. If your SDK still selects an
older revision or your app already pins `WindowsSdkPackageVersion`, set that property to the matching
revision 87 or later, such as `10.0.19041.87` for Windows 19041. Use one projection setting per project.
Remove the workaround once your SDK selects a fixed projection by default.

The component includes scripts/Test-Package.ps1 to pack the library and publish a separate consumer
with all warnings treated as errors. It tests the NuGet integration without a project reference.
Run it with -Flavor Aot or -Flavor Trimmed, then exercise the published app with the UI scripts.
The default consumer targets .NET 10 x64 with Windows App SDK 2.6.5. Use TargetFramework, WindowsAppSdkVersion, and Architecture to test other combinations.
Pass `-WindowsSdkPackageMinimumRevision 87` when testing the workaround in a trimmed consumer.
Pass `-PackagePath` to validate an existing release candidate. For an unpackaged app, check that its published directory
includes its own generated `.pri` file so WinUI can load the application's theme resources.
Affected Windows App SDK versions need the [unpackaged publish workaround](https://github.com/microsoft/WindowsAppSDK/issues/6720).
