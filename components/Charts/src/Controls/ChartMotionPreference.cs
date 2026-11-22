// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Windows.UI.ViewManagement;

namespace JPSoftworks.DesktopToolkit.Charts;

internal sealed class ChartMotionPreference
{
    private readonly FrameworkElement _owner;
    private readonly Action _finish;
    private readonly UISettings _settings = new();

    public bool AnimationsEnabled => this._settings.AnimationsEnabled;

    public ChartMotionPreference(FrameworkElement owner, Action finish)
    {
        this._owner = owner;
        this._finish = finish;
        owner.Loaded += this.OnLoaded;
        owner.Unloaded += this.OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
        {
            this._settings.AnimationsEnabledChanged += this.OnChanged;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
        {
            this._settings.AnimationsEnabledChanged -= this.OnChanged;
        }
    }

    private void OnChanged(UISettings sender, UISettingsAnimationsEnabledChangedEventArgs args)
    {
        this._owner.DispatcherQueue.TryEnqueue(() =>
        {
            if (this._owner.IsLoaded && !this.AnimationsEnabled)
            {
                this._finish();
            }
        });
    }
}
