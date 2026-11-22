// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI.ViewManagement;
using WinRT;

namespace JPSoftworks.DesktopToolkit.ToastWindows;

/// <summary>Renders desktop acrylic in the active state regardless of window focus.</summary>
public sealed partial class AlwaysActiveDesktopAcrylicBackdrop : SystemBackdrop
{
    /// <summary>Identifies the <see cref="Kind"/> dependency property.</summary>
    public static readonly DependencyProperty KindProperty = DependencyProperty.Register(
        nameof(Kind), typeof(DesktopAcrylicKind), typeof(AlwaysActiveDesktopAcrylicBackdrop),
        new PropertyMetadata(DesktopAcrylicKind.Default, OnKindChanged));

    private readonly Dictionary<ICompositionSupportsSystemBackdrop, BackdropTarget> _targets = new();

    /// <summary>Gets or sets the desktop acrylic material variant.</summary>
    public DesktopAcrylicKind Kind
    {
        [DynamicWindowsRuntimeCast(typeof(DesktopAcrylicKind))]
        get => (DesktopAcrylicKind)this.GetValue(KindProperty);
        set => this.SetValue(KindProperty, value);
    }

    internal bool CloseExternalLinkOnDisconnect { get; init; }

    /// <inheritdoc/>
    protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
    {
        base.OnTargetConnected(connectedTarget, xamlRoot);
        var state = new BackdropTarget();
        this._targets.Add(connectedTarget, state);

        if (DesktopAcrylicController.IsSupported())
        {
            var controller = new DesktopAcrylicController { Kind = this.Kind };
            state.Controller = controller;
            state.Configuration = new SystemBackdropConfiguration { IsInputActive = true };
            this.UpdateTheme(connectedTarget, xamlRoot, state);
            controller.SetSystemBackdropConfiguration(state.Configuration);
            controller.AddSystemBackdropTarget(connectedTarget);
        }
        else
        {
            var compositor = CompositionTarget.GetCompositorForCurrentThread().As<global::Windows.UI.Composition.Compositor>();
            state.Fallback = compositor.CreateColorBrush();
            this.UpdateTheme(connectedTarget, xamlRoot, state);
            connectedTarget.SystemBackdrop = state.Fallback;
        }
    }

    /// <inheritdoc/>
    protected override void OnDefaultSystemBackdropConfigurationChanged(ICompositionSupportsSystemBackdrop target, XamlRoot xamlRoot)
    {
        if (target is not null && this._targets.TryGetValue(target, out var state))
        {
            this.UpdateTheme(target, xamlRoot, state);
        }
    }

    /// <inheritdoc/>
    protected override void OnTargetDisconnected(ICompositionSupportsSystemBackdrop disconnectedTarget)
    {
        base.OnTargetDisconnected(disconnectedTarget);

        try
        {
            if (this._targets.Remove(disconnectedTarget, out var state))
            {
                if (state.Controller is { } controller)
                {
                    try
                    {
                        controller.RemoveSystemBackdropTarget(disconnectedTarget);
                    }
                    finally
                    {
                        controller.Dispose();
                    }
                }
                else if (state.Fallback is { } fallback)
                {
                    disconnectedTarget.SystemBackdrop = null;
                    fallback.Dispose();
                }
            }
        }
        finally
        {
            if (this.CloseExternalLinkOnDisconnect)
            {
                CloseExternalBackdropLink(disconnectedTarget);
            }
        }
    }

    private static void CloseExternalBackdropLink(ICompositionSupportsSystemBackdrop target)
    {
        if (target is not IWinRTObject runtimeTarget)
        {
            return;
        }

        var inspectable = new IInspectable(runtimeTarget.NativeObject);
        using var reference = inspectable.ObjRef;
        if (inspectable.GetRuntimeClassName(noThrow: true) == "Microsoft.UI.Content.ContentExternalBackdropLink")
        {
            // Close the link on the XAML thread before WinRT finalization.
            target.As<IDisposable>().Dispose();
        }
    }

    private void UpdateTheme(ICompositionSupportsSystemBackdrop target, XamlRoot xamlRoot, BackdropTarget state)
    {
        var defaults = this.GetDefaultSystemBackdropConfiguration(target, xamlRoot);
        if (state.Configuration is { } configuration)
        {
            configuration.Theme = defaults.Theme;
            configuration.IsHighContrast = defaults.IsHighContrast;
            configuration.HighContrastBackgroundColor = defaults.HighContrastBackgroundColor;
        }

        if (state.Fallback is { } fallback)
        {
            fallback.Color = defaults.IsHighContrast
                ?
                defaults.HighContrastBackgroundColor ?? new UISettings().GetColorValue(UIColorType.Background)
                : defaults.Theme == SystemBackdropTheme.Dark
                    ? Colors.Black
                    : Colors.White;
        }
    }

    [DynamicWindowsRuntimeCast(typeof(DesktopAcrylicKind))]
    private static void OnKindChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var backdrop = (AlwaysActiveDesktopAcrylicBackdrop)sender;
        foreach (var target in backdrop._targets.Values)
        {
            if (target.Controller is { } controller)
            {
                controller.Kind = (DesktopAcrylicKind)args.NewValue;
            }
        }
    }

    private sealed class BackdropTarget
    {
        public DesktopAcrylicController? Controller { get; set; }

        public SystemBackdropConfiguration? Configuration { get; set; }

        public global::Windows.UI.Composition.CompositionColorBrush? Fallback { get; set; }
    }
}
