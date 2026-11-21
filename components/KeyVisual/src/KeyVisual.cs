// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Windows.System;

namespace JPSoftworks.DesktopToolkit.Controls;

/// <summary>Displays a single key in a keyboard shortcut.</summary>
[ContentProperty(Name = nameof(Content))]
[TemplatePart(Name = PresenterPart, Type = typeof(KeyCharPresenter))]
[TemplateVisualState(Name = "Normal", GroupName = "CommonStates")]
[TemplateVisualState(Name = "Disabled", GroupName = "CommonStates")]
[TemplateVisualState(Name = "Invalid", GroupName = "CommonStates")]
[TemplateVisualState(Name = "Warning", GroupName = "CommonStates")]
public sealed partial class KeyVisual : Control
{
    private const string PresenterPart = "KeyPresenter";
    private KeyCharPresenter? _presenter;
    private string _label = string.Empty;

    /// <summary>Identifies the <see cref="Content"/> dependency property.</summary>
    public static DependencyProperty ContentProperty { get; } = DependencyProperty.Register(
        nameof(Content), typeof(object), typeof(KeyVisual), new PropertyMetadata(null, OnPresentationChanged));

    /// <summary>Identifies the <see cref="State"/> dependency property.</summary>
    public static DependencyProperty StateProperty { get; } = DependencyProperty.Register(
        nameof(State), typeof(KeyVisualState), typeof(KeyVisual), new PropertyMetadata(KeyVisualState.Normal, OnStateChanged));

    /// <summary>Identifies the <see cref="RenderKeyAsGlyph"/> dependency property.</summary>
    public static DependencyProperty RenderKeyAsGlyphProperty { get; } = DependencyProperty.Register(
        nameof(RenderKeyAsGlyph), typeof(bool), typeof(KeyVisual), new PropertyMetadata(false, OnPresentationChanged));

    /// <summary>Identifies the <see cref="RenderAsText"/> dependency property.</summary>
    public static DependencyProperty RenderAsTextProperty { get; } = DependencyProperty.Register(
        nameof(RenderAsText), typeof(bool), typeof(KeyVisual), new PropertyMetadata(false, OnPresentationChanged));

    /// <summary>Gets or sets a key label, a virtual key, or an integer Windows virtual-key code.</summary>
    public object? Content
    {
        get => this.GetValue(ContentProperty);
        set => this.SetValue(ContentProperty, value);
    }

    /// <summary>Gets or sets the keycap's validation state.</summary>
    public KeyVisualState State
    {
        get => (KeyVisualState)this.GetValue(StateProperty);
        set => this.SetValue(StateProperty, value);
    }

    /// <summary>Gets or sets whether supported keys such as arrows use symbols.</summary>
    public bool RenderKeyAsGlyph
    {
        get => (bool)this.GetValue(RenderKeyAsGlyphProperty);
        set => this.SetValue(RenderKeyAsGlyphProperty, value);
    }

    /// <summary>Gets or sets whether to show the text label for every key, including keys with icons.</summary>
    public bool RenderAsText
    {
        get => (bool)this.GetValue(RenderAsTextProperty);
        set => this.SetValue(RenderAsTextProperty, value);
    }

    /// <summary>Creates a keycap.</summary>
    public KeyVisual()
    {
        this.DefaultStyleKey = typeof(KeyVisual);
        this.RegisterPropertyChangedCallback(IsEnabledProperty, static (sender, _) => ((KeyVisual)sender).UpdateVisualState());
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        this._presenter = this.GetTemplateChild(PresenterPart) as KeyCharPresenter;
        this.UpdatePresentation();
        this.UpdateVisualState(useTransitions: false);
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new KeyVisualAutomationPeer(this);
    }

    private static void OnPresentationChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((KeyVisual)sender).UpdatePresentation();
    }

    private static void OnStateChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((KeyVisual)sender).UpdateVisualState();
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(VirtualKey))]
    private void UpdatePresentation()
    {
        var label = this.Content switch
        {
            int keyCode => GetKeyLabel((VirtualKey)keyCode),
            VirtualKey key => GetKeyLabel(key),
            object value => value.ToString() ?? string.Empty,
            _ => string.Empty,
        };

        var icon = (this.RenderAsText ? string.Empty : label) switch
        {
            "Windows" => KeyCharKind.Windows,
            "Copilot" => KeyCharKind.Copilot,
            "Office" => KeyCharKind.Office,
            _ => KeyCharKind.Text,
        };

        var glyph = this.RenderKeyAsGlyph && !this.RenderAsText ? GetGlyph(label) : null;
        this._presenter?.Show(glyph ?? label, glyph is null ? icon : KeyCharKind.Glyph);

        var oldLabel = this._label;
        this._label = label;
        if (oldLabel != label
            && string.IsNullOrEmpty(AutomationProperties.GetName(this))
            && FrameworkElementAutomationPeer.FromElement(this) is AutomationPeer peer)
        {
            peer.RaisePropertyChangedEvent(AutomationElementIdentifiers.NameProperty, oldLabel, label);
        }
    }

    internal static string GetKeyLabel(VirtualKey key)
    {
        return key switch
        {
            VirtualKey.LeftWindows or VirtualKey.RightWindows => "Windows",
            VirtualKey.LeftShift or VirtualKey.RightShift => "Shift",
            VirtualKey.LeftControl or VirtualKey.RightControl or VirtualKey.Control => "Ctrl",
            VirtualKey.LeftMenu or VirtualKey.RightMenu or VirtualKey.Menu => "Alt",
            VirtualKey.Back => "Backspace",
            >= VirtualKey.Number0 and <= VirtualKey.Number9 => ((int)key - (int)VirtualKey.Number0).ToString(),
            _ => key.ToString(),
        };
    }

    private static string? GetGlyph(string label)
    {
        return label switch
        {
            "Enter" => "\uE751",
            "Back" or "Backspace" => "\uE750",
            "Shift" => "\uE752",
            "Up" => "\uE74A",
            "Down" => "\uE74B",
            "Left" => "\uE72B",
            "Right" => "\uE72A",
            _ => null,
        };
    }

    private void UpdateVisualState(bool useTransitions = true)
    {
        VisualStateManager.GoToState(
            this, this.State switch
            {
                KeyVisualState.Error => "Invalid",
                KeyVisualState.Warning => "Warning",
                _ => this.IsEnabled ? "Normal" : "Disabled",
            }, useTransitions);
    }

    private sealed partial class KeyVisualAutomationPeer(KeyVisual owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetNameCore()
        {
            var name = base.GetNameCore();
            return string.IsNullOrEmpty(name) ? owner._label : name;
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Text;
        }

        protected override string GetClassNameCore()
        {
            return nameof(KeyVisual);
        }
    }
}
