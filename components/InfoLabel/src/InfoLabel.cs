// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace JPSoftworks.DesktopToolkit.Controls;

/// <summary>Displays an information icon with a tooltip for nearby content.</summary>
public sealed partial class InfoLabel : Control
{
    /// <summary>Identifies the <see cref="Description"/> dependency property.</summary>
    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
        nameof(Description), typeof(string), typeof(InfoLabel), new PropertyMetadata(null, OnDescriptionChanged));

    /// <summary>Gets or sets the tooltip text and default automation help text.</summary>
    public string? Description
    {
        get => (string?)this.GetValue(DescriptionProperty);
        set => this.SetValue(DescriptionProperty, value);
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

    private static void OnDescriptionChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var description = args.NewValue as string;
        ToolTipService.SetToolTip(sender, string.IsNullOrWhiteSpace(description) ? null : description);
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
            return string.IsNullOrWhiteSpace(name) ? "More information" : name;
        }

        protected override string GetHelpTextCore()
        {
            var helpText = base.GetHelpTextCore();
            return string.IsNullOrWhiteSpace(helpText) ? owner.Description ?? string.Empty : helpText;
        }
    }
}
