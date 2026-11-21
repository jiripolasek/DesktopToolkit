# Toast Windows

Show temporary WinUI 3 messages using the acrylic toast from PowerToys Command Palette. Toasts open without taking
foreground focus, dismiss automatically, and keep the timer paused while the pointer is over the toast.

Requires .NET 10 or .NET 11 and Windows App SDK 2.6.5 or later.

For a local preview, run `./eng/Build.ps1` from the Desktop Toolkit checkout in PowerShell.
Add the checkout's `artifacts/packages` directory to your NuGet sources and keep your regular sources enabled for dependencies.

Run the following command from your WinUI app project.

```powershell
dotnet add package JPSoftworks.DesktopToolkit.ToastWindows --version 0.1.0-preview.1
```

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

## License

Toast Windows is distributed under Apache-2.0. The package includes the Apache license and the
MIT notice for code adapted from [PowerToys Command Palette](https://github.com/microsoft/PowerToys)
in `licenses/PowerToys.LICENSE.txt`.
