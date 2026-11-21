// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using JPSoftworks.DesktopToolkit.Gallery.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace JPSoftworks.DesktopToolkit.Gallery;

public sealed partial class ControlPage : Page
{
    internal GalleryComponent Component { get; private set; } = null!;

    public ControlPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs args)
    {
        base.OnNavigatedTo(args);
        this.Component = GalleryCatalog.Components.Single(component => component.Id == (string)args.Parameter);
        this.PageHeader.Title = this.Component.Title;
        this.PageHeader.Description = this.Component.Description;
        this.PageHeader.Glyph = this.Component.Glyph;
        this.PageHeader.ApiNamespace = this.Component.ApiNamespace;
        this.PageHeader.ApiNotes = this.Component.ApiNotes;
        this.PageHeader.SetLinks(this.Component.Links);
        this.ExampleList.Children.Clear();
        foreach (var example in this.Component.Examples)
        {
            var preview = example.CreatePreview();
            var card = new ExampleCard
            {
                Header = example.Title,
                Description = example.Description,
                Example = preview,
                SampleControls = example.TakeSampleControls?.Invoke(preview),
            };
            card.UseEmbeddedSource(preview.GetType());
            this.ExampleList.Children.Add(card);
        }
    }
}
