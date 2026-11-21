// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Windows.System;

namespace JPSoftworks.DesktopToolkit.Controls;

/// <summary>Displays a keyboard shortcut as a row of keycaps.</summary>
public sealed partial class ShortcutPresenter : UserControl
{
    private readonly Style _defaultKeycapStyle;
    private (VirtualKey Key, VirtualKeyModifiers Modifiers, KeyVisualState State)? _capturePreview;

    /// <summary>Identifies the <see cref="Shortcut"/> dependency property.</summary>
    public static DependencyProperty ShortcutProperty { get; } = DependencyProperty.Register(
        nameof(Shortcut), typeof(KeyboardShortcut), typeof(ShortcutPresenter), new PropertyMetadata(null, OnShortcutChanged));

    /// <summary>Identifies the <see cref="KeycapStyle"/> dependency property.</summary>
    public static DependencyProperty KeycapStyleProperty { get; } = DependencyProperty.Register(
        nameof(KeycapStyle), typeof(Style), typeof(ShortcutPresenter), new PropertyMetadata(null, OnKeycapStyleChanged));

    /// <summary>Gets or sets the shortcut to display. Null clears the presenter.</summary>
    public KeyboardShortcut? Shortcut
    {
        get => (KeyboardShortcut?)this.GetValue(ShortcutProperty);
        set => this.SetValue(ShortcutProperty, value);
    }

    /// <summary>Gets or sets one style for all keycaps in the chord. Null uses the standard keycap style.</summary>
    public Style? KeycapStyle
    {
        [WinRT.DynamicWindowsRuntimeCast(typeof(Style))]
        get => (Style?)this.GetValue(KeycapStyleProperty);
        set => this.SetValue(KeycapStyleProperty, value);
    }

    internal double KeySpacing
    {
        set => this.Keys.Spacing = value;
    }

    /// <summary>Creates a display-only shortcut presenter.</summary>
    [WinRT.DynamicWindowsRuntimeCast(typeof(Style))]
    public ShortcutPresenter()
    {
        this.InitializeComponent();
        this._defaultKeycapStyle = (Style)this.Resources["DefaultKeyVisualStyle"];
    }

    internal void SetCapturePreview(VirtualKey key, VirtualKeyModifiers modifiers, KeyVisualState state)
    {
        var preview = (key, modifiers, state);
        if (this._capturePreview != preview)
        {
            this._capturePreview = preview;
            this.UpdateKeys();
        }
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new ShortcutPresenterAutomationPeer(this);
    }

    private static void OnShortcutChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var presenter = (ShortcutPresenter)sender;
        presenter.UpdateKeys();
        if (string.IsNullOrEmpty(AutomationProperties.GetName(presenter))
            && FrameworkElementAutomationPeer.FromElement(presenter) is AutomationPeer peer)
        {
            var previousName = (args.OldValue as KeyboardShortcut)?.ToString() ?? string.Empty;
            var newName = presenter.Shortcut?.ToString() ?? string.Empty;
            if (previousName != newName)
            {
                peer.RaisePropertyChangedEvent(AutomationElementIdentifiers.NameProperty, previousName, newName);
            }
        }
    }

    private static void OnKeycapStyleChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((ShortcutPresenter)sender).UpdateKeys();
    }

    private void UpdateKeys()
    {
        var shortcut = this.Shortcut;
        var (key, modifiers, state) = this._capturePreview
                                      ?? (shortcut?.Key ?? VirtualKey.None, shortcut?.Modifiers ?? VirtualKeyModifiers.None,
                                          KeyVisualState.Normal);
        var style = this.KeycapStyle ?? this._defaultKeycapStyle;
        this.Keys.Children.Clear();
        foreach (var displayKey in KeyboardShortcut.GetKeys(key, modifiers))
        {
            var keycap = new KeyVisual { Content = displayKey, RenderKeyAsGlyph = true, State = state, Style = style };
            AutomationProperties.SetAccessibilityView(keycap, AccessibilityView.Raw);
            this.Keys.Children.Add(keycap);
        }
    }

    private sealed partial class ShortcutPresenterAutomationPeer(ShortcutPresenter owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetNameCore()
        {
            var name = base.GetNameCore();
            return string.IsNullOrEmpty(name) ? owner.Shortcut?.ToString() ?? string.Empty : name;
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Text;
        }

        protected override string GetClassNameCore()
        {
            return nameof(ShortcutPresenter);
        }
    }
}
