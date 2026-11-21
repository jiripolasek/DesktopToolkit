## Quick start

Show **Windows + Shift + Up** as keycaps in an existing WinUI&nbsp;3 app.

![Windows, Shift, and up-arrow keycaps in the Gallery's Keycap styles sample.](../../../assets/quick-starts/key-visual.png)

**App requirements.** Windows 10 version 2004 (build 19041) or later, Windows App SDK 1.8 or later, and an app targeting .NET 8, .NET 10, or .NET 11. Use the SDK that matches your app's .NET target.

{{installLocalPreview}}

### Display the keycaps

Place this inside the layout of an existing WinUI page or window, then run the app. You should see three keycaps, with glyphs for the Windows, Shift, and Up keys.

```xml wrap
<StackPanel
    xmlns:keys="using:JPSoftworks.DesktopToolkit.Controls"
    Orientation="Horizontal"
    Spacing="6">
    <keys:KeyVisual Content="Windows" />
    <TextBlock Text="+" VerticalAlignment="Center" />
    <keys:KeyVisual Content="Shift" RenderKeyAsGlyph="True" />
    <TextBlock Text="+" VerticalAlignment="Center" />
    <keys:KeyVisual Content="Up" RenderKeyAsGlyph="True" />
</StackPanel>
```

To work with whole chords, see [Display a shortcut](#display-a-shortcut) or [Edit a shortcut](#edit-a-shortcut).
