// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace JPSoftworks.DesktopToolkit.Charts;

internal enum ChartLegendSwatch
{
    Box,
    Line,
    Dot,
}

internal sealed class ChartLegendPresenter
{
    private readonly Control _owner;
    private readonly ChartLegendSource _source;
    private readonly ChartLegendPanel _panel = new();
    private readonly List<Entry> _entries = [];
    private ChartLegendSwatch _swatch;
    private int _hovered = -1;
    private bool _highlightEnabled;
    private DataTemplate? _itemTemplate;

    public Panel Root { get; }

    public ChartLegendPresenter(Control owner, ChartLegendSource source)
    {
        this._owner = owner;
        this._source = source;
        this.Root = owner is HeatmapChart ? new Grid() : this._panel;
    }

    public ScrollViewer CreateScrollViewer()
    {
        return new()
        {
            Content = this.Root,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            HorizontalScrollMode = ScrollMode.Disabled,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            IsTabStop = false,
        };
    }

    public bool Update(bool visible, Orientation autoOrientation)
    {
        visible &= this._source.Entries.Length > 0;
        this.Root.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        var orientation = ChartLegend.GetLayout(this._owner) switch
        {
            ChartLegendLayout.Horizontal => Orientation.Horizontal,
            ChartLegendLayout.Vertical => Orientation.Vertical,
            _ => autoOrientation,
        };
        this._highlightEnabled = visible && ChartLegend.GetHighlightOnHover(this._owner);
        if (!this._highlightEnabled)
        {
            this.ClearHover();
        }

        if (!visible)
        {
            return false;
        }

        var template = ChartLegend.GetItemTemplate(this._owner);
        if (this._source.Scale is { } scale)
        {
            this.Root.Children.Clear();
            this.Root.Children.Add(this.CreateScale(scale, orientation, template));
            return true;
        }

        if (this._swatch != this._source.Swatch)
        {
            this.SetCount(0);
            this._swatch = this._source.Swatch;
        }

        if (template != this._itemTemplate)
        {
            this._itemTemplate = template;
            foreach (var entry in this._entries)
            {
                this.ApplyItemTemplate(entry);
            }
        }

        this._panel.HorizontalAlignment = ChartLegend.GetAlignment(this._owner);
        var spacing = ChartLegend.GetItemSpacing(this._owner);
        this._panel.SetLayout(orientation, double.IsFinite(spacing) && spacing >= 0 ? spacing : 12);
        this.SetCount(this._source.Entries.Length);
        for (var index = 0; index < this._entries.Count; index++)
        {
            this.SetEntry(index, this._source.Entries[index]);
        }

        return true;
    }

    public void ClearHover()
    {
        this.SetHovered(-1);
    }

    private void SetCount(int count)
    {
        if (count == this._entries.Count)
        {
            return;
        }

        this.SetHovered(-1);
        for (var index = this._entries.Count - 1; index >= count; index--)
        {
            this._panel.Children.RemoveAt(index);
            this._entries.RemoveAt(index);
        }

        while (this._entries.Count < count)
        {
            var entry = this.CreateEntry(this._entries.Count);
            this._entries.Add(entry);
            this._panel.Children.Add(entry.Row);
        }
    }

    private void SetEntry(int index, ChartLegendEntry data)
    {
        var (name, fill, value, stroke, isStrokeDashed) = data;
        var entry = this._entries[index];
        entry.Mark.Fill = fill;
        if (entry.Mark is Line line)
        {
            line.Stroke = fill;
            if (entry.IsStrokeDashed != isStrokeDashed)
            {
                line.StrokeDashArray = isStrokeDashed ? CreateSeriesDashArray() : null;
                entry.IsStrokeDashed = isStrokeDashed;
            }
        }
        else
        {
            entry.Mark.Stroke = stroke;
            entry.Mark.StrokeThickness = stroke is null ? 0 : 1;
        }

        entry.Name.Text = name;
        entry.Name.Foreground = this._owner.Foreground;
        entry.Name.FontFamily = entry.Value.FontFamily = this._owner.FontFamily;
        entry.Name.FontSize = entry.Value.FontSize = this._owner.FontSize;
        // Center the swatch on the visible lettering within the text line box.
        entry.Mark.Translation = new(0, (float)(this._owner.FontSize / 12), 0);
        entry.Value.Foreground = this._owner.Foreground;
        this.SetValue(index, value);
    }

    private void SetValue(int index, string? value)
    {
        var entry = this._entries[index];
        var text = value ?? string.Empty;
        if (entry.Value.Text != text)
        {
            entry.Value.Text = text;
        }

        entry.Value.Visibility = value is null || entry.Presenter is not null ? Visibility.Collapsed : Visibility.Visible;
        entry.Item?.Update(entry.Name.Text, value, entry.Mark.Fill, entry.Outline, entry.IsStrokeDashed);
        var tooltip = value is null ? entry.Name.Text : $"{entry.Name.Text}: {value}";
        if (!Equals(ToolTipService.GetToolTip(entry.Row), tooltip))
        {
            ToolTipService.SetToolTip(entry.Row, tooltip);
        }
    }

    private Entry CreateEntry(int index)
    {
        var row = new Grid { ColumnSpacing = 6, Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent), };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition());
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Shape mark = this._swatch switch
        {
            ChartLegendSwatch.Dot => new Ellipse { Width = 8, Height = 8 },
            ChartLegendSwatch.Line => new Line
            {
                Width = 20,
                Height = 3,
                X1 = 1,
                X2 = 19,
                Y1 = 1.5,
                Y2 = 1.5,
                StrokeThickness = 2,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            },
            _ => new Rectangle { Width = 10, Height = 10, RadiusX = 2, RadiusY = 2 },
        };
        mark.VerticalAlignment = VerticalAlignment.Center;
        mark.HighContrastAdjustment = ElementHighContrastAdjustment.None;
        var name = MakeLabel();
        var value = MakeLabel();
        value.Opacity = 0.76;
        Grid.SetColumn(name, 1);
        Grid.SetColumn(value, 2);
        row.Children.Add(mark);
        row.Children.Add(name);
        row.Children.Add(value);
        row.PointerEntered += (_, _) =>
        {
            if (this._highlightEnabled)
            {
                this.SetHovered(index);
            }
        };
        row.PointerExited += (_, _) =>
        {
            if (this._hovered == index)
            {
                this.SetHovered(-1);
            }
        };
        row.Unloaded += (_, _) =>
        {
            if (this._hovered == index)
            {
                this.SetHovered(-1);
            }
        };
        var entry = new Entry(row, mark, name, value);
        if (this._itemTemplate is not null)
        {
            this.ApplyItemTemplate(entry);
        }

        return entry;
    }

    private void ApplyItemTemplate(Entry entry)
    {
        var useDefault = this._itemTemplate is null;
        entry.Mark.Visibility = entry.Name.Visibility = useDefault ? Visibility.Visible : Visibility.Collapsed;
        if (useDefault)
        {
            entry.Value.Visibility = entry.Item?.ValueText is null ? Visibility.Collapsed : Visibility.Visible;
            if (entry.Presenter is { } existing)
            {
                entry.Row.Children.Remove(existing);
                entry.Presenter = null;
                entry.Item = null;
            }

            return;
        }

        var value = entry.Item?.ValueText ?? (entry.Value.Visibility == Visibility.Visible ? entry.Value.Text : null);
        entry.Value.Visibility = Visibility.Collapsed;
        entry.Item ??= new ChartLegendItem();
        entry.Item.Update(entry.Name.Text, value, entry.Mark.Fill, entry.Outline, entry.IsStrokeDashed);
        if (entry.Presenter is null)
        {
            entry.Presenter = new ContentPresenter { Content = entry.Item };
            Grid.SetColumnSpan(entry.Presenter, 3);
            entry.Row.Children.Add(entry.Presenter);
        }

        entry.Presenter.ContentTemplate = this._itemTemplate;
    }

    private void SetHovered(int index)
    {
        if (this._hovered != index)
        {
            this._hovered = index;
            this._source.SetHovered(this, index);
        }
    }

    private StackPanel CreateScale(ChartLegendScale scale, Orientation orientation, DataTemplate? template)
    {
        var ramp = new StackPanel { Orientation = orientation, Spacing = 3, VerticalAlignment = VerticalAlignment.Center };
        foreach (var entry in this._source.Entries)
        {
            if (template is not null)
            {
                var item = new ChartLegendItem();
                item.Update(entry.Name, null, entry.Fill, entry.Stroke);
                ramp.Children.Add(new ContentPresenter { Content = item, ContentTemplate = template });
                continue;
            }

            var swatch = new Border
            {
                Width = 11,
                Height = 11,
                CornerRadius = new CornerRadius(2),
                Background = entry.Fill,
                BorderThickness = new Thickness(entry.Stroke is null ? 0 : 1),
                BorderBrush = entry.Stroke,
                HighContrastAdjustment = ElementHighContrastAdjustment.None,
            };
            ToolTipService.SetToolTip(swatch, entry.Name);
            ramp.Children.Add(swatch);
        }

        var key = new StackPanel { Orientation = orientation, Spacing = 6, HorizontalAlignment = ChartLegend.GetAlignment(this._owner), };
        key.Children.Add(this.CreateScaleLabel(scale.Minimum));
        key.Children.Add(ramp);
        if (scale.Maximum is { } maximum)
        {
            key.Children.Add(this.CreateScaleLabel(maximum));
        }

        return key;
    }

    private TextBlock CreateScaleLabel(string text)
    {
        return new()
        {
            Text = text,
            Foreground = this._owner.Foreground,
            FontFamily = this._owner.FontFamily,
            FontSize = this._owner.FontSize,
            VerticalAlignment = VerticalAlignment.Center,
        };
    }

    internal static DoubleCollection CreateSeriesDashArray()
    {
        return [3, 2];
    }

    private static TextBlock MakeLabel()
    {
        return new()
        {
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxLines = 1,
        };
    }

    private sealed record Entry(Grid Row, Shape Mark, TextBlock Name, TextBlock Value)
    {
        public Brush? Outline => this.Mark is Line ? null : this.Mark.Stroke;

        public bool IsStrokeDashed { get; set; }

        public ChartLegendItem? Item { get; set; }

        public ContentPresenter? Presenter { get; set; }
    }
}
