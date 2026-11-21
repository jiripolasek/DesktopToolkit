// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

namespace JPSoftworks.DesktopToolkit.Gallery.UI;

public sealed partial class GalleryPageHeader : UserControl
{
    public event Action<GalleryLinkKind>? LocalLinkInvoked;

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(GalleryPageHeader), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
        nameof(Description), typeof(string), typeof(GalleryPageHeader), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register(
        nameof(Glyph), typeof(string), typeof(GalleryPageHeader), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ApiNamespaceProperty = DependencyProperty.Register(
        nameof(ApiNamespace), typeof(string), typeof(GalleryPageHeader), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ApiNotesProperty = DependencyProperty.Register(
        nameof(ApiNotes), typeof(string), typeof(GalleryPageHeader), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty PreviewThemeToggleVisibilityProperty = DependencyProperty.Register(
        nameof(PreviewThemeToggleVisibility), typeof(Visibility), typeof(GalleryPageHeader), new PropertyMetadata(Visibility.Visible));

    public string Title { get => (string)this.GetValue(TitleProperty); set => this.SetValue(TitleProperty, value); }

    public string Description { get => (string)this.GetValue(DescriptionProperty); set => this.SetValue(DescriptionProperty, value); }

    public string Glyph { get => (string)this.GetValue(GlyphProperty); set => this.SetValue(GlyphProperty, value); }

    public string ApiNamespace { get => (string)this.GetValue(ApiNamespaceProperty); set => this.SetValue(ApiNamespaceProperty, value); }

    public string ApiNotes { get => (string)this.GetValue(ApiNotesProperty); set => this.SetValue(ApiNotesProperty, value); }

    public Visibility PreviewThemeToggleVisibility
    {
        get => (Visibility)this.GetValue(PreviewThemeToggleVisibilityProperty);
        set => this.SetValue(PreviewThemeToggleVisibilityProperty, value);
    }

    public GalleryPageHeader()
    {
        this.InitializeComponent();
        this.ActualThemeChanged += (_, _) =>
        {
            if (this.PreviewThemeToggle.IsChecked == true)
            {
                this.UpdatePreviewTheme();
            }
        };
    }

    public void SetLinks(GalleryLink[] links)
    {
        var buttons = this.ResourceLinks.Children;
        buttons.Clear();
        var apiIndex = 0;
        foreach (var link in links)
        {
            var icon = new FontIcon { Glyph = GetLinkGlyph(link.Kind), FontSize = 14 };
            AutomationProperties.SetAccessibilityView(icon, AccessibilityView.Raw);
            var content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            content.Children.Add(icon);
            content.Children.Add(new TextBlock { Text = link.Label });
            var button = new Button { Content = content };
            AutomationProperties.SetName(button, link.Label);
            if (link.PackageId is { } packageId && link.Uri is { } packageUri)
            {
                button.Flyout = this.CreateNuGetFlyout(packageId, packageUri);
            }
            else if (link.Uri is { } uri)
            {
                button.Click += async (_, _) => await Launcher.LaunchUriAsync(uri);
            }
            else
            {
                button.Click += (_, _) => this.LocalLinkInvoked?.Invoke(link.Kind);
            }

            buttons.Add(button);
            if (link.Kind is GalleryLinkKind.Documentation or GalleryLinkKind.Styling)
            {
                apiIndex = buttons.Count;
            }
        }

        buttons.Insert(apiIndex, this.ApiInformationButton);
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(Style))]
    private Flyout CreateNuGetFlyout(string packageId, Uri packageUri)
    {
        var packageName = new TextBox
        {
            Header = "Package ID",
            Text = packageId,
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = new FontFamily("Consolas"),
        };
        var copyButton = new Button { Content = "Copy", MinWidth = 72, VerticalAlignment = VerticalAlignment.Bottom };
        copyButton.Click += (_, _) =>
        {
            var data = new DataPackage();
            data.SetText(packageId);
            try
            {
                Clipboard.SetContent(data);
                copyButton.Content = "Copied";
            }
            catch (COMException)
            {
                copyButton.Content = "Copy failed";
            }
        };
        var packageRow = new Grid { ColumnSpacing = 8 };
        packageRow.ColumnDefinitions.Add(new ColumnDefinition());
        packageRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(copyButton, 1);
        packageRow.Children.Add(packageName);
        packageRow.Children.Add(copyButton);

        var content = new StackPanel { Spacing = 8 };
        content.Children.Add(packageRow);
        content.Children.Add(new HyperlinkButton { Content = "View on NuGet.org", NavigateUri = packageUri });

        var flyout = new Flyout
        {
            Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft,
            FlyoutPresenterStyle = (Style)this.Resources["NuGetFlyoutPresenterStyle"],
            Content = content,
        };
        flyout.Opening += (_, _) => copyButton.Content = "Copy";
        return flyout;
    }

    private static string GetLinkGlyph(GalleryLinkKind kind)
    {
        return kind switch
        {
            GalleryLinkKind.SourceCode => "\uE943",
            GalleryLinkKind.Documentation => "\uE82D",
            GalleryLinkKind.NuGetPackage => "\uE7B8",
            GalleryLinkKind.Styling => "\uE790",
            _ => string.Empty,
        };
    }

    private void OnPreviewThemeToggled(object sender, RoutedEventArgs args)
    {
        this.UpdatePreviewTheme();
    }

    private void UpdatePreviewTheme()
    {
        var theme = this.PreviewThemeToggle.IsChecked != true ? ElementTheme.Default :
            this.ActualTheme == ElementTheme.Dark ? ElementTheme.Light : ElementTheme.Dark;
        for (DependencyObject? parent = this; parent is not null; parent = VisualTreeHelper.GetParent(parent))
        {
            if (parent is Page page)
            {
                SetExampleTheme(page, theme);
                return;
            }
        }
    }

    private static void SetExampleTheme(DependencyObject parent, ElementTheme theme)
    {
        if (parent is ExampleCard card)
        {
            card.SetPreviewTheme(theme);
            return;
        }

        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var index = 0; index < count; index++)
        {
            SetExampleTheme(VisualTreeHelper.GetChild(parent, index), theme);
        }
    }
}
