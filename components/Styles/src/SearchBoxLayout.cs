// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace JPSoftworks.DesktopToolkit.Styles;

/// <summary>Positions the native query button in an AutoSuggestBox.</summary>
public sealed partial class SearchBoxLayout : DependencyObject
{
    /// <summary>Identifies the attached property that places the query button before the text.</summary>
    public static DependencyProperty UseLeadingQueryIconProperty { get; } = DependencyProperty.RegisterAttached(
        "UseLeadingQueryIcon", typeof(bool), typeof(SearchBoxLayout), new PropertyMetadata(false, OnPlacementChanged));

    private SearchBoxLayout() { }

    /// <summary>Gets whether the query button appears before the text.</summary>
    public static bool GetUseLeadingQueryIcon(DependencyObject element)
    {
        return (bool)element.GetValue(UseLeadingQueryIconProperty);
    }

    /// <summary>Sets whether the query button appears before the text.</summary>
    public static void SetUseLeadingQueryIcon(DependencyObject element, bool value)
    {
        element.SetValue(UseLeadingQueryIconProperty, value);
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(AutoSuggestBox))]
    private static void OnPlacementChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is not AutoSuggestBox searchBox)
        {
            return;
        }

        var useLeadingIcon = (bool)args.NewValue;
        if (useLeadingIcon)
        {
            searchBox.Loaded += OnLoaded;
            if (!searchBox.IsLoaded)
            {
                return;
            }
        }
        else
        {
            searchBox.Loaded -= OnLoaded;
        }

        UpdateLayout(searchBox, useLeadingIcon);
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(AutoSuggestBox))]
    private static void OnLoaded(object sender, RoutedEventArgs args)
    {
        UpdateLayout((AutoSuggestBox)sender, true);
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(TextBox))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(Button))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(ScrollViewer))]
    [WinRT.DynamicWindowsRuntimeCast(typeof(ContentControl))]
    private static void UpdateLayout(AutoSuggestBox searchBox, bool useLeadingIcon)
    {
        if (useLeadingIcon)
        {
            searchBox.ApplyTemplate();
        }

        if (GetTemplateGrid(searchBox) is not { } searchGrid || GetChild<TextBox>(searchGrid, "TextBox") is not { } textBox)
        {
            return;
        }

        if (useLeadingIcon)
        {
            textBox.ApplyTemplate();
        }

        // Rearrange only the stock WinUI template and retain its native buttons and visual states.
        if (GetTemplateGrid(textBox) is not { ColumnDefinitions.Count: 4 } fieldGrid
            || GetChild<Button>(fieldGrid, "QueryButton") is not { } queryButton
            || GetChild<Button>(fieldGrid, "DeleteButton") is not { } clearButton
            || GetChild<ScrollViewer>(fieldGrid, "ContentElement") is not { } editor
            || GetChild<ContentControl>(fieldGrid, "PlaceholderTextContentPresenter") is not { } placeholder)
        {
            return;
        }

        var stretch = new GridLength(1, GridUnitType.Star);
        fieldGrid.ColumnDefinitions[0].Width = useLeadingIcon ? GridLength.Auto : stretch;
        fieldGrid.ColumnDefinitions[1].Width = useLeadingIcon ? stretch : GridLength.Auto;
        Grid.SetColumn(queryButton, useLeadingIcon ? 0 : 2);
        Grid.SetColumn(clearButton, useLeadingIcon ? 2 : 1);
        Grid.SetColumn(editor, useLeadingIcon ? 1 : 0);
        Grid.SetColumn(placeholder, useLeadingIcon ? 1 : 0);
        Grid.SetColumnSpan(placeholder, useLeadingIcon ? 1 : 2);
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(Grid))]
    private static Grid? GetTemplateGrid(Control control)
    {
        return VisualTreeHelper.GetChildrenCount(control) > 0 ? VisualTreeHelper.GetChild(control, 0) as Grid : null;
    }

    private static T? GetChild<T>(Grid grid, string name)
        where T : FrameworkElement
    {
        return grid.Children.OfType<T>().FirstOrDefault(child => child.Name == name);
    }
}
