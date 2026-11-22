# Toast Windows

Show temporary in-app notifications in WinUI 3. Toasts use an acrylic backdrop, open without taking focus, and dismiss
automatically. Hovering over a toast pauses its dismissal timer.

## Requirements

- Windows 10 version 2004 (build 19041) or later
- A WinUI 3 app targeting .NET 8 or later
- Windows App SDK 2.6.5 or later

### .NET 10 trimmed publishing

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

## Installation

For a local preview, run `./eng/Build.ps1` from the toolkit checkout and add `artifacts/packages` to your NuGet sources.
Keep your regular sources enabled for dependencies.

Run this command from your WinUI app project directory.

```powershell
dotnet add package JPSoftworks.DesktopToolkit.ToastWindows --version 0.1.0-preview.2
```

## Usage

Create a `ToastWindow` for the host window and show a message.

```csharp
using JPSoftworks.DesktopToolkit.ToastWindows;

var toast = new ToastWindow(mainWindow.AppWindow.Id);
toast.ShowToast("Changes saved.");
```

Keep the window on the UI thread, reuse it for subsequent messages, and close it when the host window closes. A toast
can also display an icon, run an action from a button, and use a custom display duration.

## Position

The default, `ToastPosition.UseSystemSettings`, reads **Position of on-screen indicators** in Windows notification
settings each time the toast opens. If the registry value is absent or inaccessible, the toast opens at the bottom center.
Set `ToastWindow.Position` to `BottomCenter`, `TopLeft`, or `TopCenter` to override Windows. The gallery example shows
how to save that choice in the host app's settings.

## Window and backdrop helpers

`TransparentWindow` is a borderless WinUI window that stays out of the taskbar and Alt+Tab. Its `Show()` method opens
without activating the window. `AlwaysActiveDesktopAcrylicBackdrop` keeps desktop acrylic in the active visual state
when attached to a non-activating window or a `SystemBackdropElement`.
