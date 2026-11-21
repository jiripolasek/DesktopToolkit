// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using JPSoftworks.DesktopToolkit.Gallery.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace JPSoftworks.DesktopToolkit.Controls.Demo.Controls;

public sealed partial class SamplePageHeader : UserControl
{
    internal static readonly GalleryLink Documentation = GalleryLink.ForComponentDocumentation("charts");
    internal static readonly GalleryLink Source = GalleryLink.ForComponentSource("Charts");
    internal static readonly GalleryLink NuGetPackage = GalleryLink.ForNuGetPackage("JPSoftworks.DesktopToolkit.Controls.Charts");

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(SamplePageHeader), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
        nameof(Description), typeof(string), typeof(SamplePageHeader), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ApiNotesProperty = DependencyProperty.Register(
        nameof(ApiNotes), typeof(string), typeof(SamplePageHeader), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty StylingControlProperty = DependencyProperty.Register(
        nameof(StylingControl), typeof(string), typeof(SamplePageHeader),
        new PropertyMetadata(null, (sender, _) => ((SamplePageHeader)sender).UpdateLinks()));

    public string Title { get => (string)this.GetValue(TitleProperty); set => this.SetValue(TitleProperty, value); }

    public string Description { get => (string)this.GetValue(DescriptionProperty); set => this.SetValue(DescriptionProperty, value); }

    public string ApiNotes { get => (string)this.GetValue(ApiNotesProperty); set => this.SetValue(ApiNotesProperty, value); }

    public string? StylingControl
    {
        get => (string?)this.GetValue(StylingControlProperty);
        set => this.SetValue(StylingControlProperty, value);
    }

    public string? StylingExample { get; set; }

    public SamplePageHeader()
    {
        this.InitializeComponent();
        this.UpdateLinks();
        this.PageHeader.LocalLinkInvoked += this.OnLocalLinkInvoked;
    }

    private void UpdateLinks()
    {
        this.PageHeader.SetLinks(
            string.IsNullOrEmpty(this.StylingControl)
                ? [Documentation, Source, NuGetPackage]
                : [Documentation, new(GalleryLinkKind.Styling, "Styling", null), Source, NuGetPackage]);
    }

    private Page? FindPage()
    {
        for (DependencyObject? parent = this; parent is not null; parent = VisualTreeHelper.GetParent(parent))
        {
            if (parent is Page page)
            {
                return page;
            }
        }

        return null;
    }

    private async void OnLocalLinkInvoked(GalleryLinkKind kind)
    {
        if (kind == GalleryLinkKind.Styling && this.StylingControl is { } control && ChartStylingGuide.Get(control) is { } guide)
        {
            var preview = this.StylingExample is { } name ? this.FindPage()?.FindName(name) as ExampleCard : null;
            var stylingDialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = $"{control} styling",
                Content = ChartStylingGuide.CreateView(guide),
                CloseButtonText = "Close",
                PrimaryButtonText = preview is null ? string.Empty : "Go to preview",
            };
            if (await stylingDialog.ShowAsync() == ContentDialogResult.Primary)
            {
                preview?.StartBringIntoView();
            }
        }
    }
}
