// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;

namespace JPSoftworks.DesktopToolkit.ToastWindows.Samples;

public sealed partial class ToastPositionSettingsExample : Page
{
    private const string SettingKey = "ToastPosition";
    private static readonly IDictionary<string, object> Settings = OpenSettings();

    public ToastPositionSettingsExample()
    {
        this.InitializeComponent();
        this.PositionPicker.SelectedIndex = Settings.TryGetValue(SettingKey, out var value)
                                            && value is int index
                                            && index >= 0
                                            && index < this.PositionPicker.Items.Count
            ? index
            : 0;
        this.UpdateSystemSettingsLink();
        this.PositionPicker.SelectionChanged += this.OnPositionChanged;
    }

    private void OnPositionChanged(object sender, SelectionChangedEventArgs args)
    {
        if (this.PositionPicker.SelectedIndex >= 0)
        {
            Settings[SettingKey] = this.PositionPicker.SelectedIndex;
            this.UpdateSystemSettingsLink();
        }
    }

    private void UpdateSystemSettingsLink()
    {
        this.SystemSettingsLink.Visibility = this.PositionPicker.SelectedIndex == (int)ToastPosition.UseSystemSettings
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private static IDictionary<string, object> OpenSettings()
    {
        const int noPackage = unchecked((int)0x80073D54);
        try
        {
            return ApplicationData.Current.LocalSettings.Values;
        }
        catch (Exception exception) when (exception.HResult == noPackage)
        {
            return new Dictionary<string, object>();
        }
    }
}
