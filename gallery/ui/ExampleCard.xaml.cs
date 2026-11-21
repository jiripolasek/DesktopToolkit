// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Windows.ApplicationModel.DataTransfer;
using WinUIEditor;

namespace JPSoftworks.DesktopToolkit.Gallery.UI;

[ContentProperty(Name = nameof(Example))]
public sealed partial class ExampleCard : UserControl
{
    private const double SidebarBreakpoint = 740;
    private const double SidebarWidth = 248;

    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
        nameof(Header), typeof(string), typeof(ExampleCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
        nameof(Description), typeof(string), typeof(ExampleCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ExampleProperty = DependencyProperty.Register(
        nameof(Example), typeof(object), typeof(ExampleCard), new PropertyMetadata(null));

    public static readonly DependencyProperty SampleControlsProperty = DependencyProperty.Register(
        nameof(SampleControls), typeof(object), typeof(ExampleCard), new PropertyMetadata(null, OnSidebarContentChanged));

    public static readonly DependencyProperty OptionsProperty = DependencyProperty.Register(
        nameof(Options), typeof(object), typeof(ExampleCard), new PropertyMetadata(null, OnSidebarContentChanged));

    public static readonly DependencyProperty PreviewVerticalAlignmentProperty = DependencyProperty.Register(
        nameof(PreviewVerticalAlignment), typeof(VerticalAlignment), typeof(ExampleCard), new PropertyMetadata(VerticalAlignment.Center));

    public static readonly DependencyProperty XamlProperty = DependencyProperty.Register(
        nameof(Xaml), typeof(string), typeof(ExampleCard), new PropertyMetadata(string.Empty, OnSourceChanged));

    public static readonly DependencyProperty CSharpProperty = DependencyProperty.Register(
        nameof(CSharp), typeof(string), typeof(ExampleCard), new PropertyMetadata(string.Empty, OnSourceChanged));

    private Type? _embeddedSourceType;
    private CodeEditorControl? _sourceEditor;
    private string? _embeddedXaml;
    private string? _embeddedCSharp;

    public string Header { get => (string)this.GetValue(HeaderProperty); set => this.SetValue(HeaderProperty, value); }

    public string Description { get => (string)this.GetValue(DescriptionProperty); set => this.SetValue(DescriptionProperty, value); }

    public object? Example { get => this.GetValue(ExampleProperty); set => this.SetValue(ExampleProperty, value); }

    public object? SampleControls { get => this.GetValue(SampleControlsProperty); set => this.SetValue(SampleControlsProperty, value); }

    public object? Options { get => this.GetValue(OptionsProperty); set => this.SetValue(OptionsProperty, value); }

    public VerticalAlignment PreviewVerticalAlignment
    {
        get => (VerticalAlignment)this.GetValue(PreviewVerticalAlignmentProperty);
        set => this.SetValue(PreviewVerticalAlignmentProperty, value);
    }

    public string Xaml { get => (string)this.GetValue(XamlProperty); set => this.SetValue(XamlProperty, value); }

    public string CSharp { get => (string)this.GetValue(CSharpProperty); set => this.SetValue(CSharpProperty, value); }

    private bool IsCSharpSelected => ReferenceEquals(this.SourceLanguage.SelectedItem, this.CSharpSourceItem);

    private string? XamlSource => string.IsNullOrWhiteSpace(this.Xaml) ? this._embeddedXaml : this.Xaml;

    private string? CSharpSource => string.IsNullOrWhiteSpace(this.CSharp) ? this._embeddedCSharp : this.CSharp;

    private string? SelectedSource => this.IsCSharpSelected ? this.CSharpSource : this.XamlSource;

    public ExampleCard()
    {
        this.InitializeComponent();
    }

    public void UseEmbeddedSource(Type previewType)
    {
        this._embeddedSourceType = previewType;
        this.UpdateSource();
    }

    public void ShowSource()
    {
        if (this.SourceExpander.Visibility == Visibility.Visible)
        {
            this.SourceExpander.IsExpanded = true;
            this.SourceExpander.StartBringIntoView();
        }
    }

    public void SetPreviewTheme(ElementTheme theme)
    {
        this.PreviewBorder.RequestedTheme = theme;
    }

    private string GetOptionsName(string title)
    {
        return $"Options for {title}";
    }

    private string GetSourceName(string title)
    {
        return $"Source code for {title}";
    }

    private Visibility GetTextVisibility(string text)
    {
        return string.IsNullOrWhiteSpace(text) ? Visibility.Collapsed : Visibility.Visible;
    }

    private static void OnSidebarContentChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((ExampleCard)sender).UpdateSidebarContent();
    }

    private void UpdateSidebarContent()
    {
        var hasControls = this.SampleControls is not null;
        var hasOptions = this.Options is not null;
        this.SampleControlsHost.Visibility = hasControls ? Visibility.Visible : Visibility.Collapsed;
        this.OptionsButton.Visibility = hasOptions ? Visibility.Visible : Visibility.Collapsed;
        this.SampleSidebar.Visibility = hasControls || hasOptions ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnCardSizeChanged(object sender, SizeChangedEventArgs args)
    {
        var stacked = args.NewSize.Width < SidebarBreakpoint;
        Grid.SetColumn(this.SampleSidebar, stacked ? 0 : 1);
        Grid.SetRow(this.SampleSidebar, stacked ? 1 : 0);
        this.SampleSidebar.Width = stacked ? double.NaN : SidebarWidth;
        this.SampleSidebar.BorderThickness = stacked ? new Thickness(0, 1, 0, 0) : new Thickness(1, 0, 0, 0);
    }

    private static void OnSourceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((ExampleCard)sender).UpdateSource();
    }

    private void OnSourceExpanding(Expander sender, ExpanderExpandingEventArgs args)
    {
        if (this._sourceEditor is not null)
        {
            return;
        }

        if (this._embeddedSourceType is { } sourceType)
        {
            this._embeddedXaml = ReadEmbeddedSource(sourceType, ".xaml");
            this._embeddedCSharp = ReadEmbeddedSource(sourceType, ".xaml.cs");
        }

        this._sourceEditor = new CodeEditorControl();
        AutomationProperties.SetName(this._sourceEditor, "Sample source code");
        this.SourceHost.Content = this._sourceEditor;
        this.UpdateSource();
    }

    private static string? ReadEmbeddedSource(Type sourceType, string extension)
    {
        using var stream = sourceType.Assembly.GetManifestResourceStream($"ExampleSource.{sourceType.Name}{extension}");
        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private void UpdateSource()
    {
        var hasSource = this._embeddedSourceType is not null || this.XamlSource is not null || this.CSharpSource is not null;
        this.SourceExpander.Visibility = hasSource ? Visibility.Visible : Visibility.Collapsed;
        this.PreviewBorder.CornerRadius = hasSource ? new CornerRadius(8, 8, 0, 0) : new CornerRadius(8);
        this.PreviewBorder.BorderThickness = hasSource ? new Thickness(1, 1, 1, 0) : new Thickness(1);

        if (this._sourceEditor is null)
        {
            return;
        }

        this.XamlSourceItem.Visibility = this.XamlSource is null ? Visibility.Collapsed : Visibility.Visible;
        this.CSharpSourceItem.Visibility = this.CSharpSource is null ? Visibility.Collapsed : Visibility.Visible;
        if (this.SourceLanguage.SelectedItem is null || this.SelectedSource is null)
        {
            this.SourceLanguage.SelectedItem = this.XamlSource is not null ? this.XamlSourceItem : this.CSharpSourceItem;
        }

        this.DisplaySource();
    }

    private void OnSourceLanguageChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        this.DisplaySource();
    }

    private void DisplaySource()
    {
        if (this._sourceEditor is null)
        {
            return;
        }

        var source = this.SelectedSource;
        var editor = this._sourceEditor.Editor;
        this._sourceEditor.HighlightingLanguage = source is null ? string.Empty : this.IsCSharpSelected ? "csharp" : "xml";
        editor.ReadOnly = false;
        editor.SetText(source ?? "Source code is unavailable.");
        editor.EmptyUndoBuffer();
        editor.ReadOnly = true;
        editor.FirstVisibleLine = 0;
        this._sourceEditor.Height = Math.Clamp(editor.LineCount * 18 + 16, 88, 360);

        this.CopySource.IsEnabled = source is not null;
        this.CopySource.Content = "Copy";
    }

    private void OnCopySource(object sender, RoutedEventArgs args)
    {
        if (this.SelectedSource is not { } source)
        {
            return;
        }

        var data = new DataPackage();
        data.SetText(source);
        try
        {
            Clipboard.SetContent(data);
            this.CopySource.Content = "Copied";
        }
        catch (COMException)
        {
            this.CopySource.Content = "Copy failed";
        }
    }
}
