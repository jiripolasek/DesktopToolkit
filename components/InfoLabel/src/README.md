# InfoLabel

Place a small information icon beside a label, field, or heading to offer optional context. The native WinUI tooltip
opens on hover, keyboard focus, or press and hold.

Requires .NET 10 or .NET 11 and Windows App SDK 1.8.260317003 or later.

```xml
<StackPanel xmlns:info="using:JPSoftworks.DesktopToolkit.Controls"
            Orientation="Horizontal">
    <TextBlock Text="Refresh interval (seconds)" VerticalAlignment="Center" />
    <info:InfoLabel Description="Shorter intervals use more network data."
                    AutomationProperties.Name="About the refresh interval" />
</StackPanel>
```

`Description` is a bindable dependency property. It supplies the tooltip and the default automation help text.
Set `AutomationProperties.Name` to identify the adjacent content for screen reader users. The default name is
"More information."
