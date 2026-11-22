// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;

namespace JPSoftworks.DesktopToolkit.Controls;

internal sealed class FlyoutItemEntry(SearchableFlyoutItem item, bool hasChecks, bool hasIcons, bool rightToLeft)
{
    public SearchableFlyoutItem Item { get; } = item;

    public DataTemplate? ChoiceTemplate { get; init; }

    public int PositionInSet { get; internal set; }

    public int SizeOfSet { get; internal set; }

    public GridLength CheckColumnWidth { get; } = new(hasChecks ? 28 : 0);

    public GridLength IconColumnWidth { get; } = new(hasIcons ? 28 : 0);

    public string SubmenuGlyph { get; } = item.HasChildren ? (rightToLeft ? "\uE76B" : "\uE76C") : string.Empty;

    public bool HasDefaultContent => !this.Item.IsSeparator && this.ChoiceTemplate is null;

    public bool HasTemplatedContent => !this.Item.IsSeparator && this.ChoiceTemplate is not null;

    public Visibility SeparatorVisibility => this.Item.IsSeparator ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility TextVisibility(string text)
    {
        return string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible;
    }

    public static Visibility CheckVisibility(bool isCheckable, bool isChecked)
    {
        return isCheckable && isChecked ? Visibility.Visible : Visibility.Collapsed;
    }

    public static Visibility GlyphVisibility(SearchableFlyoutItem item)
    {
        return item.IconSource is null && !string.IsNullOrEmpty(item.Glyph) ? Visibility.Visible : Visibility.Collapsed;
    }
}
