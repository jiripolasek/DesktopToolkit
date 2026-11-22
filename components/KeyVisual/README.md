# KeyVisual

Display keycaps and let users view, record, and edit keyboard shortcuts.

See the [package guide](src/README.md) for requirements, installation, and usage.

## Examples

Open **Keyboard Shortcuts** in the Desktop Toolkit Gallery. The standalone app in [dev/](dev/) hosts the
[sample pages](samples/) for keycaps and shortcut editing. Run it from the repository root.

```powershell
dotnet run --project components/KeyVisual/dev -p:Platform=x64
```

## Development

- [src/](src/) contains the keycap, shortcut presenter, and shortcut editor controls.
- [tests/](tests/) contains the control checks that the toolkit build runs on .NET 8, .NET 10, and .NET 11.

See the [toolkit development guide](../../eng/README.md) for build prerequisites and shared commands.
