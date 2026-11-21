// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml.Controls;

namespace JPSoftworks.DesktopToolkit.Controls.SearchableFlyouts.Samples;

public sealed partial class SearchableItemsFlyoutExample : Page
{
    public SearchableItemsFlyoutExample()
    {
        this.InitializeComponent();
        this.CommandMenu.ItemsSource = CreateCommands().ToArray();
    }

    private static IEnumerable<SearchableFlyoutItem> CreateCommands()
    {
        yield return new() { Text = "New file", Glyph = "\uE8A5", Keywords = "create document" };
        yield return new() { Text = "Open file", Glyph = "\uE8E5", Keywords = "browse document" };
        yield return new() { Text = "Save", Glyph = "\uE74E", Keywords = "write document" };
        yield return new() { Text = "Copy path", Glyph = "\uE8C8", Keywords = "clipboard location" };
        yield return new() { Text = "Pin to favorites", Glyph = "\uE734", IsCheckable = true, Keywords = "bookmark star" };
        yield return new() { IsSeparator = true };
        yield return new() { Text = "Export selection", Glyph = "\uEDE1", IsEnabled = false };
        yield return new()
        {
            Text = "Export as",
            Glyph = "\uEDE1",
            Keywords = "format output",
            Items =
            {
                new() { Text = "PDF document", Keywords = "portable print" },
                new() { Text = "PNG image", Keywords = "picture bitmap" },
                new() { Text = "SVG vector", Keywords = "scalable graphic" },
            },
        };
        yield return new() { IsSeparator = true };
        for (var index = 1; index <= 10_000; index++)
        {
            yield return new() { Text = $"Open recent document {index:D5}", Glyph = "\uE8A5", Keywords = "history" };
        }
    }

    private void OnItemInvoked(SearchableItemsFlyout sender, FlyoutItemInvokedEventArgs args)
    {
        var item = args.InvokedItem;
        this.CommandResult.Text = item.IsCheckable ? $"{item.Text} {(item.IsChecked ? "enabled" : "disabled")}" : $"{item.Text} selected";
    }
}
