// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.ApplicationModel.WindowsAppRuntime;
using Windows.Foundation;

namespace JPSoftworks.DesktopToolkit.Controls;

internal static class MenuSearch
{
    internal static string[] Tokenize(string query)
    {
        return query.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
    }

    internal static bool Matches(string text, string keywords, string[] tokens)
    {
        return tokens.All(token =>
            text.Contains(token, StringComparison.OrdinalIgnoreCase) || keywords.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    internal static List<T> Filter<T>(IEnumerable<T> source, Func<T, bool> visible, Func<T, bool> separator, Func<T, bool> matches)
        where T : class
    {
        List<T> result = [];
        T? pendingSeparator = null;
        foreach (var item in source.Where(visible))
        {
            if (separator(item))
            {
                if (result.Count > 0)
                {
                    pendingSeparator = item;
                }
            }
            else if (matches(item))
            {
                if (pendingSeparator is not null)
                {
                    result.Add(pendingSeparator);
                    pendingSeparator = null;
                }

                result.Add(item);
            }
        }

        return result;
    }

    internal static int FindChoiceIndex(IReadOnlyList<SearchableFlyoutItem> choices, object? value, int preferredIndex)
    {
        if (value is null)
        {
            return -1;
        }

        var hasPreferred = preferredIndex >= 0 && preferredIndex < choices.Count;
        if (hasPreferred && ReferenceEquals(choices[preferredIndex].Tag, value))
        {
            return preferredIndex;
        }

        var equalIndex = hasPreferred && Equals(choices[preferredIndex].Tag, value) ? preferredIndex : -1;
        for (var index = 0; index < choices.Count; index++)
        {
            var candidate = choices[index].Tag;
            if (ReferenceEquals(candidate, value))
            {
                return index;
            }

            if (equalIndex < 0 && Equals(candidate, value))
            {
                equalIndex = index;
            }
        }

        return equalIndex;
    }

    internal static int FindInvokableIndex(IReadOnlyList<SearchableFlyoutItem> items, int start, int direction)
    {
        var count = items.Count;
        for (var offset = 0; offset < count; offset++)
        {
            var index = (int)((start + (long)offset * direction) % count);
            if (index < 0)
            {
                index += count;
            }

            if (items[index].CanInvoke)
            {
                return index;
            }
        }

        return -1;
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(UIElement))]
    internal static void SetAcrylicBackdrop(Border host, Border? themedBackground, bool enabled, bool rounded = false)
    {
        host.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
        if (themedBackground is not null)
        {
            themedBackground.Visibility = enabled ? Visibility.Collapsed : Visibility.Visible;
        }

        if (!enabled)
        {
            host.Child = null;
            return;
        }

        if (ReleaseInfo.Major < 2 || host.Child is not null)
        {
            return;
        }

        // Load the WinUI 2.x element only when its runtime is present.
        var corners = rounded ? " CornerRadius='{ThemeResource OverlayCornerRadius}'" : string.Empty;
        host.Child = (UIElement)XamlReader.Load(
            "<SystemBackdropElement xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'"
            + " SystemBackdrop='{StaticResource MenuFlyoutSystemBackdrop}' IsHitTestVisible='False'"
            + " AutomationProperties.AccessibilityView='Raw'"
            + corners
            + " />");
        host.ClearValue(Border.BackgroundProperty);
    }

    internal static Popup? FindPopup(FrameworkElement presenter)
    {
        return VisualTreeHelper.GetOpenPopupsForXamlRoot(presenter.XamlRoot).FirstOrDefault(popup => Equals(popup.Child, presenter));
    }

    internal static FilterBoxPlacement ResolvePlacement(Popup? popup, FrameworkElement surface, FrameworkElement anchor)
    {
        var placement = popup?.ActualPlacement;
        if (placement is PopupPlacementMode.Top or PopupPlacementMode.TopEdgeAlignedLeft or PopupPlacementMode.TopEdgeAlignedRight
            or PopupPlacementMode.LeftEdgeAlignedBottom or PopupPlacementMode.RightEdgeAlignedBottom)
        {
            return FilterBoxPlacement.Bottom;
        }

        if (placement is PopupPlacementMode.Bottom or PopupPlacementMode.BottomEdgeAlignedLeft or PopupPlacementMode.BottomEdgeAlignedRight
            or PopupPlacementMode.LeftEdgeAlignedTop or PopupPlacementMode.RightEdgeAlignedTop)
        {
            return FilterBoxPlacement.Top;
        }

        var origin = surface.TransformToVisual(anchor).TransformPoint(new Point());
        var middle = anchor.ActualHeight / 2;
        return Math.Abs(origin.Y - middle) <= Math.Abs(origin.Y + surface.ActualHeight - middle)
            ? FilterBoxPlacement.Top
            : FilterBoxPlacement.Bottom;
    }
}
