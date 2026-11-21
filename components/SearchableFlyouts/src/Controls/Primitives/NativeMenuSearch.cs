// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace JPSoftworks.DesktopToolkit.Controls.Primitives;

/// <summary>Connects a native menu presenter template to its owning <see cref="SearchableMenuFlyout"/>.</summary>
public sealed partial class NativeMenuSearch : DependencyObject
{
    private static readonly ConditionalWeakTable<FrameworkElement, NativeMenuSearchSession> Sessions = new();

    /// <summary>Identifies the IsEnabled attached property for the searchable presenter template.</summary>
    public static DependencyProperty IsEnabledProperty { get; } = DependencyProperty.RegisterAttached(
        "IsEnabled", typeof(bool), typeof(NativeMenuSearch), new PropertyMetadata(false, OnIsEnabledChanged));

    private NativeMenuSearch() { }

    /// <summary>Gets whether the presenter template root participates in menu filtering.</summary>
    /// <param name="element">The element whose attached value is read.</param>
    /// <returns>True if search is enabled on this template root.</returns>
    public static bool GetIsEnabled(DependencyObject element)
    {
        return (bool)element.GetValue(IsEnabledProperty);
    }

    /// <summary>Enables or disables search on the root of a searchable menu presenter template.</summary>
    /// <param name="element">The root element of the presenter template.</param>
    /// <param name="value">True to attach search when the element is loaded.</param>
    public static void SetIsEnabled(DependencyObject element, bool value)
    {
        element.SetValue(IsEnabledProperty, value);
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
    private static void OnIsEnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is not FrameworkElement root)
        {
            return;
        }

        root.Loaded -= OnLoaded;
        root.Unloaded -= OnUnloaded;
        if ((bool)args.NewValue)
        {
            root.Loaded += OnLoaded;
            root.Unloaded += OnUnloaded;
            if (root.IsLoaded)
            {
                QueueAttach(root);
            }
        }
        else
        {
            Detach(root);
        }
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
    private static void OnLoaded(object sender, RoutedEventArgs args)
    {
        QueueAttach((FrameworkElement)sender);
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
    private static void OnUnloaded(object sender, RoutedEventArgs args)
    {
        Detach((FrameworkElement)sender);
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
    internal static void EnsureAttached(FrameworkElement presenter)
    {
        if (GetIsEnabled(presenter))
        {
            QueueAttach(presenter);
            return;
        }

        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(presenter); index++)
        {
            if (VisualTreeHelper.GetChild(presenter, index) is FrameworkElement child)
            {
                EnsureAttached(child);
            }
        }
    }

    private static void QueueAttach(FrameworkElement root)
    {
        root.DispatcherQueue.TryEnqueue(
            Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                if (root.IsLoaded && GetIsEnabled(root))
                {
                    Attach(root);
                }
            });
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(MenuFlyoutPresenter))]
    private static void Attach(FrameworkElement root)
    {
        if (Sessions.TryGetValue(root, out var existing))
        {
            if (!existing.IsDisposed)
            {
                return;
            }

            Sessions.Remove(root);
        }

        for (DependencyObject? parent = root; parent is not null; parent = VisualTreeHelper.GetParent(parent))
        {
            if (parent is MenuFlyoutPresenter presenter && SearchableMenuFlyout.FindOwner(presenter) is { IsOpen: true } owner)
            {
                var helpers = presenter.Items.OfType<MenuSearchItem>();
                var header = helpers.FirstOrDefault(item => item.IsFilter);
                var empty = helpers.FirstOrDefault(item => !item.IsFilter);
                if (header is null || empty is null)
                {
                    return;
                }

                var session = new NativeMenuSearchSession(owner, presenter, root, header, empty);
                Sessions.Add(root, session);
                return;
            }
        }
    }

    private static void Detach(FrameworkElement root)
    {
        if (Sessions.TryGetValue(root, out var session))
        {
            session.Dispose();
            Sessions.Remove(root);
        }
    }
}
