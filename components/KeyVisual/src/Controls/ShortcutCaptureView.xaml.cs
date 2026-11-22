// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Core;

namespace JPSoftworks.DesktopToolkit.Controls;

internal sealed partial class ShortcutCaptureView : UserControl
{
    internal event Action? DraftChanged;

    private readonly KeyboardShortcut? _defaultShortcut;
    private readonly bool _allowClear;
    private readonly bool _includeWindowsKey;
    private readonly ShortcutEditorStrings _strings;
    private VirtualKey _key;
    private VirtualKeyModifiers _modifiers;
    private VirtualKeyModifiers _heldModifiers;
    private KeyVisualState _draftState;

    internal KeyboardShortcut? Draft { get; private set; }

    private bool IsDraftEmpty => this._key == VirtualKey.None && this._modifiers == VirtualKeyModifiers.None;

    internal bool CanSave => this.Draft is not null || (this._allowClear && this.IsDraftEmpty);

    [WinRT.DynamicWindowsRuntimeCast(typeof(Style))]
    internal ShortcutCaptureView(
        KeyboardShortcut? shortcut,
        KeyboardShortcut? defaultShortcut,
        bool allowClear,
        bool includeWindowsKey,
        ShortcutEditorStrings strings)
    {
        this._defaultShortcut = defaultShortcut;
        this._allowClear = allowClear;
        this._includeWindowsKey = includeWindowsKey;
        this._strings = strings;
        this.InitializeComponent();
        AutomationProperties.SetName(this.CaptureSurface, strings.CaptureName);
        this.EmptyLabel.Text = strings.UnassignedText;
        this.ResetButton.Content = strings.UseDefaultButtonText;
        this.ClearButton.Content = strings.ClearButtonText;
        this.CapturePreview.KeycapStyle = (Style)this.Resources["CaptureKeycapStyle"];
        this.CapturePreview.KeySpacing = 8;
        this.CaptureInstructions.Text = includeWindowsKey
            ? strings.CaptureWithWindowsKeyInstructions
            : strings.CaptureInstructions;
        this.ResetButton.Visibility = defaultShortcut is null ? Visibility.Collapsed : Visibility.Visible;
        this.ClearButton.Visibility = allowClear ? Visibility.Visible : Visibility.Collapsed;
        this.SetDraft(shortcut);
    }

    internal void FocusCapture()
    {
        this.CaptureSurface.Focus(FocusState.Programmatic);
    }

    private void SetDraft(KeyboardShortcut? shortcut)
    {
        this._heldModifiers = VirtualKeyModifiers.None;
        this._key = shortcut?.Key ?? VirtualKey.None;
        this._modifiers = shortcut?.Modifiers ?? VirtualKeyModifiers.None;
        this.UpdateDraft();
    }

    internal bool CaptureKey(VirtualKey key, VirtualKeyModifiers modifiers)
    {
        if (!this.IsEnabled)
        {
            return false;
        }

        if (key == VirtualKey.Escape || (key == VirtualKey.Tab && modifiers is VirtualKeyModifiers.None or VirtualKeyModifiers.Shift))
        {
            return false;
        }

        var modifier = KeyboardShortcut.GetModifier(key);
        if (modifier != VirtualKeyModifiers.None)
        {
            this.UpdateHeldModifiers(modifiers | modifier);
            return true;
        }

        var wasShowingModifiers = this._heldModifiers != VirtualKeyModifiers.None;
        this._heldModifiers = VirtualKeyModifiers.None;
        modifiers = this.IncludeWindowsModifier(modifiers);
        if (key != this._key || modifiers != this._modifiers)
        {
            this._key = key;
            this._modifiers = modifiers;
            this.UpdateDraft();
        }
        else if (wasShowingModifiers)
        {
            this.RenderPreview();
        }

        return true;
    }

    internal void ReleaseKey(VirtualKey key, VirtualKeyModifiers modifiers)
    {
        if (this._heldModifiers != VirtualKeyModifiers.None && KeyboardShortcut.GetModifier(key) != VirtualKeyModifiers.None)
        {
            this.UpdateHeldModifiers(modifiers);
        }
    }

    private VirtualKeyModifiers IncludeWindowsModifier(VirtualKeyModifiers modifiers)
    {
        return this._includeWindowsKey ? modifiers | VirtualKeyModifiers.Windows : modifiers;
    }

    private void UpdateHeldModifiers(VirtualKeyModifiers modifiers)
    {
        if (this._heldModifiers != modifiers)
        {
            this._heldModifiers = modifiers;
            this.RenderPreview();
        }
    }

    private void UpdateDraft()
    {
        this.Draft = KeyboardShortcut.IsValid(this._key, this._modifiers) ? new KeyboardShortcut(this._key, this._modifiers) : null;
        if (this.DraftChanged is { } draftChanged)
        {
            draftChanged();
        }
        else
        {
            this.RenderDraft();
        }
    }

    internal void RenderDraft(string? feedback = null, KeyVisualState feedbackState = KeyVisualState.Normal)
    {
        var isInvalid = this._key != VirtualKey.None && this.Draft is null;
        const VirtualKeyModifiers altGr = VirtualKeyModifiers.Control | VirtualKeyModifiers.Menu;
        var warnAltGr = this.Draft is not null && (this._modifiers & (altGr | VirtualKeyModifiers.Windows)) == altGr;
        this._draftState = feedback is not null ? feedbackState :
            isInvalid ? KeyVisualState.Error :
            warnAltGr ? KeyVisualState.Warning : KeyVisualState.Normal;
        var message = feedback ?? (isInvalid ? this._strings.CaptureInstructions : null);
        if (warnAltGr && this._draftState != KeyVisualState.Error)
        {
            var altGrWarning = this._strings.AltGrWarning;
            message = message is null ? altGrWarning : $"{message}\n{altGrWarning}";
        }

        this.ValidationMessage.Text = message ?? string.Empty;
        this.ClearButton.IsEnabled = !this.IsDraftEmpty;
        this.RenderPreview();
    }

    private void RenderPreview()
    {
        var isPartial = this._heldModifiers != VirtualKeyModifiers.None;
        var key = isPartial ? VirtualKey.None : this._key;
        var modifiers = isPartial ? this.IncludeWindowsModifier(this._heldModifiers) : this._modifiers;
        var isEmpty = !isPartial && this.IsDraftEmpty;
        this.CapturePreview.SetCapturePreview(key, modifiers, isPartial ? KeyVisualState.Normal : this._draftState);
        this.EmptyLabel.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
        AutomationProperties.SetHelpText(this.CaptureSurface, isEmpty ? this.EmptyLabel.Text : KeyboardShortcut.Format(key, modifiers));
        this.ValidationMessage.Visibility = !isPartial && this.ValidationMessage.Text.Length != 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void OnCaptureKeyDown(object sender, KeyRoutedEventArgs args)
    {
        if (this.CaptureKey(args.OriginalKey, GetCurrentModifiers()))
        {
            args.Handled = true;
        }
    }

    private void OnCaptureKeyUp(object sender, KeyRoutedEventArgs args)
    {
        this.ReleaseKey(args.OriginalKey, GetCurrentModifiers());
    }

    private void OnCaptureLostFocus(object sender, RoutedEventArgs args)
    {
        this.UpdateHeldModifiers(VirtualKeyModifiers.None);
    }

    private static VirtualKeyModifiers GetCurrentModifiers()
    {
        var modifiers = VirtualKeyModifiers.None;
        if (IsDown(VirtualKey.Control))
        {
            modifiers |= VirtualKeyModifiers.Control;
        }

        if (IsDown(VirtualKey.Menu))
        {
            modifiers |= VirtualKeyModifiers.Menu;
        }

        if (IsDown(VirtualKey.Shift))
        {
            modifiers |= VirtualKeyModifiers.Shift;
        }

        if (IsDown(VirtualKey.LeftWindows) || IsDown(VirtualKey.RightWindows))
        {
            modifiers |= VirtualKeyModifiers.Windows;
        }

        return modifiers;
    }

    private static bool IsDown(VirtualKey key)
    {
        return (InputKeyboardSource.GetKeyStateForCurrentThread(key) & CoreVirtualKeyStates.Down) != 0;
    }

    private void OnCaptureTapped(object sender, TappedRoutedEventArgs args)
    {
        this.FocusCapture();
    }

    internal void ResetDraft()
    {
        if (this._defaultShortcut is not null)
        {
            this.SetDraft(this._defaultShortcut);
        }
    }

    internal void ClearDraft()
    {
        if (this._allowClear)
        {
            this.SetDraft(null);
        }
    }

    private void OnResetClick(object sender, RoutedEventArgs args)
    {
        this.ResetDraft();
    }

    private void OnClearClick(object sender, RoutedEventArgs args)
    {
        this.ClearDraft();
    }
}

internal sealed partial class ShortcutCaptureSurface : ContentControl
{
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new FrameworkElementAutomationPeer(this);
    }
}
