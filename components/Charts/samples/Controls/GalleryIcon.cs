// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace JPSoftworks.DesktopToolkit.Controls.Demo.Controls;

public static class GalleryIcon
{
    public static readonly DependencyProperty TemplateProperty = DependencyProperty.RegisterAttached(
        "Template", typeof(DataTemplate), typeof(GalleryIcon), new PropertyMetadata(null, OnTemplateChanged));

    public static DataTemplate? GetTemplate(DependencyObject element)
    {
        return (DataTemplate?)element.GetValue(TemplateProperty);
    }

    public static void SetTemplate(DependencyObject element, DataTemplate? value)
    {
        element.SetValue(TemplateProperty, value);
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(PathIcon))]
    public static IconElement? CreateIcon(DataTemplate? template, double size)
    {
        if (template?.LoadContent() is not IconElement icon)
        {
            return null;
        }

        icon.Width = size;
        icon.Height = size;
        if (icon is PathIcon path)
        {
            var scale = size / 24;
            path.Data.Transform = new ScaleTransform { ScaleX = scale, ScaleY = scale };
        }

        return icon;
    }

    private static void OnTemplateChanged(DependencyObject element, DependencyPropertyChangedEventArgs args)
    {
        var size = element is NavigationViewItem ? 16d : 20d;
        if (CreateIcon(args.NewValue as DataTemplate, size) is not { } icon)
        {
            return;
        }

        switch (element)
        {
            case NavigationViewItem item:
                item.Icon = icon;
                break;
            case SettingsCard card:
                card.HeaderIcon = icon;
                break;
        }
    }
}
