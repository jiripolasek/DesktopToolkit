// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace JPSoftworks.DesktopToolkit.Controls;

internal enum KeyCharKind
{
    Text,
    Glyph,
    Windows,
    Office,
    Copilot,
}

/// <summary>Presents the label or symbol within a <see cref="KeyVisual"/> template.</summary>
public sealed partial class KeyCharPresenter : Control
{
    private KeyCharKind _kind;

    /// <summary>Identifies the <see cref="Content"/> dependency property.</summary>
    public static DependencyProperty ContentProperty { get; } = DependencyProperty.Register(
        nameof(Content), typeof(string), typeof(KeyCharPresenter), new PropertyMetadata(string.Empty));

    /// <summary>Gets or sets the text displayed by the presenter.</summary>
    public string Content
    {
        get => (string)this.GetValue(ContentProperty);
        set => this.SetValue(ContentProperty, value);
    }

    /// <summary>Creates a key label presenter.</summary>
    public KeyCharPresenter()
    {
        this.DefaultStyleKey = typeof(KeyCharPresenter);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        VisualStateManager.GoToState(this, this._kind.ToString(), false);
    }

    internal void Show(string content, KeyCharKind kind)
    {
        this.Content = content;
        this._kind = kind;
        VisualStateManager.GoToState(this, kind.ToString(), false);
    }
}
