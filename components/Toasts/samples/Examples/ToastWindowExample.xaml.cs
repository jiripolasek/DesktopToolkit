// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace JPSoftworks.DesktopToolkit.ToastWindows.Samples;

public sealed partial class ToastWindowExample : Page
{
    private ToastWindow? _toast;

    public ToastWindowExample()
    {
        this.InitializeComponent();
    }

    public FrameworkElement TakeSampleControls()
    {
        this.SampleLayout.Children.Remove(this.SampleOptions);
        return this.SampleOptions;
    }

    private static bool HasMessage(string message)
    {
        return !string.IsNullOrWhiteSpace(message);
    }

    private void OnShowToast(object sender, RoutedEventArgs args)
    {
        this.ShowPreview();
    }

    private void OnShowActionToast(object sender, RoutedEventArgs args)
    {
        this.ShowPreview(
            new ToastAction(
                "View report", () =>
                {
                    this.ActionStatus.Text = "Report opened.";
                    this.ActionStatus.Visibility = Visibility.Visible;
                }, new FontIconSource { Glyph = "\uE8A7" }));
    }

    private void ShowPreview(ToastAction? action = null)
    {
        this.ActionStatus.Visibility = Visibility.Collapsed;
        this._toast ??= new ToastWindow(this.XamlRoot.ContentIslandEnvironment.AppWindowId);
        this._toast.Position = (ToastPosition)this.PositionPicker.SelectedIndex;
        if (this._toast.Content is FrameworkElement content)
        {
            content.RequestedTheme = this.ActualTheme;
        }

        var icon = this.IconPicker.SelectedIndex switch
        {
            1 => new FontIconSource { Glyph = "\uE73E" },
            2 => new FontIconSource { Glyph = "\uE946" },
            3 => new FontIconSource { Glyph = "\uE8A5" },
            _ => null,
        };
        this._toast.ShowToast(this.MessageBox.Text.Trim(), icon, action, action is null ? null : TimeSpan.FromSeconds(5));
    }

    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        this._toast?.Close();
        this._toast = null;
    }
}
