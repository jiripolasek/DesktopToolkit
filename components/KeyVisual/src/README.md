# KeyVisual

`KeyVisual` displays a keycap in a WinUI 3 app. Compose keycaps with text to describe keyboard shortcuts.

```xml
<StackPanel xmlns:keys="using:JPSoftworks.DesktopToolkit.Controls"
            Orientation="Horizontal" Spacing="6">
    <keys:KeyVisual Content="Ctrl" />
    <TextBlock Text="+" VerticalAlignment="Center" />
    <keys:KeyVisual Content="Up" RenderKeyAsGlyph="True" />
</StackPanel>
```

Set `Content` to a label, a `Windows.System.VirtualKey` value, or an integer Windows virtual-key code.
`RenderKeyAsGlyph` renders the arrow, Enter, Backspace, and Shift keys as symbols. Windows, Office, and Copilot
keycaps use their logos. Change `State` to `Warning` or `Error` to highlight a keycap.

Merge the following dictionary into the relevant XAML resources to use the named styles.

```xml
<ResourceDictionary Source="ms-appx:///JPSoftworks.DesktopToolkit.Controls.KeyVisual/Themes/KeyVisualStyles.xaml" />
```

| Style | Typical use |
| --- | --- |
| `DefaultKeyVisualStyle` | Standard 26 DIP keycaps |
| `SmallKeyVisualStyle` | Compact 22 DIP keycaps in help and dense rows |
| `LargeKeyVisualStyle` | 48 DIP keycaps with 18 DIP labels for capture surfaces |
| `TextOnlyKeyVisualStyle` | Unframed, text-only shortcut hints in command lists |
| `SubtleKeyVisualStyle` | Transparent keycap chrome |
| `AccentKeyVisualStyle` | Highlighted keys |

The keycap sizes use square minimums. Longer labels can grow horizontally. `RenderAsText` displays the label
for keys that ordinarily show a logo or glyph.

The control exposes its key label to UI Automation and stays out of the tab order. Set
`AutomationProperties.Name` to override the label. Pair validation colors with text that explains the state.

## Display a shortcut

Bind a `KeyboardShortcut` to `ShortcutPresenter.Shortcut` to show the whole chord in a command list or help view.
The presenter orders the modifiers and primary key, and UI Automation reads them as a single label.

```xml
<keys:ShortcutPresenter Shortcut="{x:Bind ViewModel.OpenShortcut}"
                        KeycapStyle="{StaticResource SmallKeyVisualStyle}" />
```

`KeycapStyle` applies to every keycap and defaults to the standard style when unset. A null shortcut clears
the display. Use `KeyVisual` for a standalone key such as an unmodified arrow.

## Edit a shortcut

`ShortcutEditor` opens a dialog and previews the assigned shortcut with keycaps. Bind its `Shortcut` property
two-way to a `KeyboardShortcut?` in your view model. The control writes the new value when the user selects Save.
If another `ContentDialog` is open, `EditAsync()` returns `false` and preserves the shortcut. Close the host dialog before opening the shortcut editor.

```xml
<keys:ShortcutEditor Shortcut="{x:Bind ViewModel.ActivationShortcut, Mode=TwoWay}"
                     DefaultShortcut="{x:Bind ViewModel.DefaultActivationShortcut}"
                     AllowClear="True" />
```

```csharp
var shortcut = new KeyboardShortcut(VirtualKey.Space, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);
```

The value stores a `Windows.System.VirtualKey` and `VirtualKeyModifiers`. Set `DefaultShortcut` to offer
**Use default**. Set `AllowClear` to offer **None**, which saves an unassigned shortcut as `null`. Tab and
Shift+Tab move focus, and Escape cancels the dialog.

Capture runs while the shortcut box has focus and receives keys delivered by Windows. Set
`IncludeWindowsKey="True"` on the editor to add the Windows modifier to newly captured chords.
The saved shortcut and **Use default** value retain the modifiers supplied by the app.
Assigned shortcuts use accent keycaps. Set `PreviewKeycapStyle` to override them, for example with
`{StaticResource DefaultKeyVisualStyle}` for neutral keycaps. Set it to `null` to restore the accent style.
The dialog uses large keycaps for the live preview. Hold modifiers to preview them, then press a main key to
stage the chord. Releasing the modifiers alone restores the staged shortcut. Validation feedback is shown
alongside the staged shortcut.

### Restrict shortcuts and handle conflicts

Set `ValidateShortcut` to return a message for a restricted chord or `null` to allow it. The editor shows the
message and disables Save. Set `FindConflict` to return the existing assignment or `null` if the chord is free.

```csharp
var print = new KeyboardShortcut(VirtualKey.P, VirtualKeyModifiers.Control);
editor.ValidateShortcut = shortcut => shortcut == print ? "Ctrl+P is reserved for printing." : null;
editor.FindConflict = shortcut =>
{
    var owner = bindings.FindOwner(shortcut);
    return owner is null ? null : new ShortcutConflict(owner.Id, owner.DisplayName,
        owner.CanBeReplaced ? () => bindings.TryReassign(shortcut, owner.Id, "open-launcher") : null);
};
```

A conflict with a replacement callback offers **Replace**. A conflict without one displays the owner and blocks
saving. Give each assignment a stable, unique `OwnerId` so the editor can verify the user is confirming the same
assignment. The editor rechecks both callbacks on submission and asks for confirmation again if the replacement
target changes.

The host owns shortcut registration. Its replacement callback must verify the expected owner and transfer the
shortcut atomically, returning `true` only after success. Returning `false` keeps the dialog open and preserves
the editor's saved value. Clearing or keeping the current assignment bypasses validation and conflict lookup.

### Check and replace through IPC

Use `ValidateShortcutAsync` and `FindConflictAsync` when validation or conflict lookup requires I/O. Validation
must pass before conflict lookup begins. Set `TryReplaceAsync` on the conflict to perform the reassignment
asynchronously. Each async callback takes precedence over its synchronous counterpart.

```csharp
editor.ValidateShortcutAsync = (shortcut, cancellationToken) =>
    policy.GetRestrictionAsync(shortcut, cancellationToken);

editor.FindConflictAsync = async (shortcut, cancellationToken) =>
{
    var owner = await bindings.FindOwnerAsync(shortcut, cancellationToken);
    if (owner is null)
    {
        return null;
    }

    return new ShortcutConflict(owner.Id, owner.DisplayName)
    {
        TryReplaceAsync = owner.CanBeReplaced
            ? () => bindings.TryReassignAsync(shortcut, owner.Id, "open-launcher")
            : null,
    };
};
```

The editor keeps capturing while a check is pending and disables submission until the current result arrives.
Validation and conflict lookup share a token that is canceled when the draft changes or the dialog closes.
The editor ignores late responses and rechecks the current combination on submission. After the user confirms
a replacement, the editor holds the confirmed draft until the callback reports the transaction's outcome.

The gallery's **Async shortcut checks** sample uses simulated IPC delays.

## Requirements

- Windows 10 version 2004 (build 19041) or later
- A WinUI 3 app using the Windows App SDK 1.8 or later and targeting .NET 10 or .NET 11

## License

KeyVisual is distributed under Apache-2.0. The package includes the Apache license and the upstream
MIT notice in `licenses/PowerToys.LICENSE.txt`.

Adapted from [Microsoft PowerToys KeyVisual](https://github.com/microsoft/PowerToys/tree/main/src/common/Common.UI.Controls/Controls/KeyVisual).
The editor adapts the [Command Palette shortcut editor](https://github.com/microsoft/PowerToys/tree/main/src/modules/cmdpal/Microsoft.CmdPal.UI/Controls/ShortcutControl).
