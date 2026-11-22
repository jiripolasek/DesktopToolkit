// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace JPSoftworks.DesktopToolkit.Controls;

/// <summary>Displays an information icon that provides help for nearby content.</summary>
public sealed partial class InfoLabel : Control
{
    /// <summary>Identifies the <see cref="Description"/> dependency property.</summary>
    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
        nameof(Description), typeof(string), typeof(InfoLabel), new PropertyMetadata(null, OnHelpChanged));

    /// <summary>Identifies the <see cref="DescriptionContent"/> dependency property.</summary>
    public static readonly DependencyProperty DescriptionContentProperty = DependencyProperty.Register(
        nameof(DescriptionContent), typeof(object), typeof(InfoLabel), new PropertyMetadata(null, OnHelpChanged));

    /// <summary>Gets or sets the plain-text fallback and default automation help text.</summary>
    public string? Description
    {
        get => (string?)this.GetValue(DescriptionProperty);
        set => this.SetValue(DescriptionProperty, value);
    }

    /// <summary>Gets or sets the formatted help content.</summary>
    /// <remarks>When this property is <see langword="null"/>, the icon displays <see cref="Description"/>.</remarks>
    public object? DescriptionContent
    {
        get => this.GetValue(DescriptionContentProperty);
        set => this.SetValue(DescriptionContentProperty, value);
    }

    /// <summary>Initializes an information label.</summary>
    public InfoLabel()
    {
        this.DefaultStyleKey = typeof(InfoLabel);
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new InfoLabelAutomationPeer(this);
    }

    private static void OnHelpChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var label = (InfoLabel)sender;
        var description = label.Description;
        var content = label.DescriptionContent ?? (string.IsNullOrWhiteSpace(description) ? null : description);
        ToolTipService.SetToolTip(label, content);
    }

    private sealed partial class InfoLabelAutomationPeer(InfoLabel owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore()
        {
            return nameof(InfoLabel);
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Text;
        }

        protected override string GetNameCore()
        {
            var name = base.GetNameCore();
            return string.IsNullOrWhiteSpace(name) ? WinUIResourceStrings.Get("InfoLabelName") : name;
        }

        protected override string GetHelpTextCore()
        {
            var helpText = base.GetHelpTextCore();
            return string.IsNullOrWhiteSpace(helpText) ? owner.Description ?? string.Empty : helpText;
        }
    }
}
