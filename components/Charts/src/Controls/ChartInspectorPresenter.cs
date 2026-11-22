// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Globalization;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using VirtualKey = Windows.System.VirtualKey;

namespace JPSoftworks.DesktopToolkit.Charts;

internal readonly record struct ChartInspectionItem(Point Anchor, string Text, IReadOnlyList<ChartInspectionRow>? Rows = null)
{
    public ChartInspectionItem(Point anchor, IReadOnlyList<ChartInspectionRow> rows)
        : this(anchor, string.Join(Environment.NewLine, rows.Select(row => row.Text)), rows)
    {
    }

    public ChartInspectionItem WithSwatch(Brush? fill, Brush? stroke = null)
    {
        return this with { Rows = (ChartInspectionRow[])[new(this.Text, fill, stroke)] };
    }
}

internal sealed class ChartInspectorPresenter
{
    public const string PartName = "PART_Inspector";

    private readonly Control _owner;
    private readonly FrameworkElement _plot;
    private readonly Popup? _popup;
    private readonly ContentControl? _card;
    private readonly TextBlock? _fallback;
    private readonly Func<int> _count;
    private readonly Func<Point, int> _hitTest;
    private readonly Func<int, ChartInspectionItem?> _read;
    private readonly Func<int, ChartContextValue[]>? _readContext;
    private readonly Action<int>? _highlight;
    private readonly Func<int, bool>? _invoke;
    private readonly Func<int, VirtualKey, int>? _move;
    private readonly long _enabledToken;
    private readonly long _contentTemplateToken;
    private readonly DispatcherQueueTimer _hoverTimer;
    private readonly ChartContextMenuPresenter? _contextMenu;
    private IReadOnlyList<ChartInspectionRow>? _displayedRows;
    private ControlTemplate? _readoutTemplate;
    private ContentPresenter? _readout;
    private bool _readoutLocated;
    private ContentPresenter? _customPresenter;
    private ChartInspectionContent? _customContent;
    private ChartInspectorMotion? _cardMotion;
    private Point? _pointer;
    private int _selected = -1;
    private bool _isReadoutVisible;

    public int FocusedIndex => this._pointer is null ? this._selected : -1;

    private bool CanInspect => this._owner.IsEnabled && this._contextMenu?.IsOpen != true && ChartInspection.GetIsEnabled(this._owner);

    [WinRT.DynamicWindowsRuntimeCast(typeof(ContentControl))]
    public ChartInspectorPresenter(
        Control owner,
        FrameworkElement plot,
        Popup? popup,
        Func<int> count,
        Func<Point, int> hitTest,
        Func<int, ChartInspectionItem?> read,
        Action<int>? highlight = null,
        TextBlock? fallback = null,
        Func<int, bool>? invoke = null,
        Func<int, ChartContextValue[]>? readContext = null,
        Func<int, VirtualKey, int>? move = null)
    {
        this._owner = owner;
        this._plot = plot;
        this._popup = popup;
        this._card = popup?.Child as ContentControl;
        this._fallback = fallback;
        this._count = count;
        this._hitTest = hitTest;
        this._read = read;
        this._readContext = readContext;
        this._highlight = highlight;
        this._invoke = invoke;
        this._move = move;
        if (readContext is not null)
        {
            this._contextMenu = new ChartContextMenuPresenter(
                owner, plot, point =>
                {
                    var index = point is Point position ? this._hitTest(position) : this.FocusedIndex;
                    return index >= 0 && index < this._count() && this._read(index) is { } item
                        ? new ChartContextTarget(item, readContext(index))
                        : null;
                }, this.Clear);
        }

        this._hoverTimer = owner.DispatcherQueue.CreateTimer();
        this._hoverTimer.IsRepeating = false;
        this._hoverTimer.Tick += this.OnHoverDelayElapsed;
        this._enabledToken = owner.RegisterPropertyChangedCallback(ChartInspection.IsEnabledProperty, (_, _) => this.Clear());
        this._contentTemplateToken = owner.RegisterPropertyChangedCallback(
            ChartInspection.ContentTemplateProperty, (_, _) =>
            {
                this.ReleaseCustomReadout();
                if (this._isReadoutVisible)
                {
                    this.Refresh();
                }
            });
        this._owner.Unloaded += this.OnClear;
        this._owner.LostFocus += this.OnClear;
        this._owner.IsEnabledChanged += this.OnOwnerEnabledChanged;
        this._owner.KeyDown += this.OnKeyDown;
        this._plot.PointerEntered += this.OnPointer;
        this._plot.PointerMoved += this.OnPointer;
        this._plot.PointerExited += this.OnPointerExited;
        this._plot.Tapped += this.OnTapped;
    }

    public void Detach()
    {
        this._contextMenu?.Detach();
        this.Clear();
        this._hoverTimer.Tick -= this.OnHoverDelayElapsed;
        this._owner.UnregisterPropertyChangedCallback(ChartInspection.IsEnabledProperty, this._enabledToken);
        this._owner.UnregisterPropertyChangedCallback(ChartInspection.ContentTemplateProperty, this._contentTemplateToken);
        this._owner.Unloaded -= this.OnClear;
        this._owner.LostFocus -= this.OnClear;
        this._owner.IsEnabledChanged -= this.OnOwnerEnabledChanged;
        this._owner.KeyDown -= this.OnKeyDown;
        this._plot.PointerEntered -= this.OnPointer;
        this._plot.PointerMoved -= this.OnPointer;
        this._plot.PointerExited -= this.OnPointerExited;
        this._plot.Tapped -= this.OnTapped;
    }

    public void Refresh(int? focusedIndex = null)
    {
        if (this._pointer is Point position)
        {
            this.InspectPointer(position, refresh: true);
        }
        else if (this._selected >= 0 && !this.TryShow(focusedIndex ?? this._selected, out _, refresh: true))
        {
            this.Clear();
        }
    }

    public void Clear()
    {
        this._pointer = null;
        this.Hide();
    }

    private void Hide()
    {
        this._hoverTimer.Stop();
        this._cardMotion?.Reset();
        this._isReadoutVisible = false;
        this.ReleaseCustomReadout();
        if (this._selected >= 0)
        {
            this._selected = -1;
            this._highlight?.Invoke(-1);
            if (this._fallback is not null && this._card is null)
            {
                this._fallback.Text = string.Empty;
            }
        }

        if (this._popup is not null)
        {
            this._popup.IsOpen = false;
        }
    }

    private void OnClear(object sender, RoutedEventArgs e)
    {
        this.Clear();
    }

    private void OnOwnerEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        this.Clear();
    }

    private void OnPointer(object sender, PointerRoutedEventArgs e)
    {
        if (this.CanInspect && e.Pointer.PointerDeviceType != PointerDeviceType.Touch)
        {
            this.InspectPointer(e.GetCurrentPoint(this._plot).Position);
        }
    }

    private void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (this._pointer.HasValue)
        {
            this.Clear();
        }
    }

    private void OnTapped(object sender, TappedRoutedEventArgs e)
    {
        if (e.Handled || (!this.CanInspect && this._invoke is null))
        {
            return;
        }

        var inspect = this.CanInspect && e.PointerDeviceType == PointerDeviceType.Touch;
        if (inspect || this._invoke is not null)
        {
            var index = this._hitTest(e.GetPosition(this._plot));
            if (inspect)
            {
                if ((index == this._selected && this._isReadoutVisible) || !this.TryShow(index, out _, refresh: true))
                {
                    this.Clear();
                }
                else
                {
                    this._pointer = null;
                    this._owner.Focus(FocusState.Pointer);
                }
            }

            e.Handled = this.TryInvoke(index);
        }
    }

    private void InspectPointer(Point position, bool refresh = false)
    {
        this._pointer = position;
        if (!this.TryShow(this._hitTest(position), out _, refresh, waitForHover: true))
        {
            this.Hide();
        }
    }

    private void OnHoverDelayElapsed(DispatcherQueueTimer sender, object args)
    {
        if (!this.CanInspect || this._pointer is not Point position)
        {
            return;
        }

        if (this._hitTest(position) != this._selected)
        {
            this.InspectPointer(position);
        }
        else if (!this.TryShow(this._selected, out _, refresh: true))
        {
            this.Hide();
        }
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Handled || !this.CanInspect || this._owner.FocusState == FocusState.Unfocused)
        {
            return;
        }

        if (e.Key == VirtualKey.Enter && this.TryInvoke(this.FocusedIndex))
        {
            e.Handled = true;
            return;
        }

        if (e.Key == VirtualKey.Escape && this._selected >= 0)
        {
            this.Clear();
            e.Handled = true;
            return;
        }

        var isArrow = e.Key is VirtualKey.Left or VirtualKey.Right or VirtualKey.Up or VirtualKey.Down;
        if (isArrow && this._move is not null)
        {
            e.Handled = this.TrySelectFromKeyboard(this._move(this._selected, e.Key)) || this._selected >= 0;
            return;
        }

        var count = this._count();
        var step = e.Key is VirtualKey.Left or VirtualKey.Up or VirtualKey.End ? -1 : 1;
        var start = e.Key switch
        {
            VirtualKey.Home => 0,
            VirtualKey.End => count - 1,
            VirtualKey.Left or VirtualKey.Up => this._selected < 0 ? count - 1 : this._selected - 1,
            VirtualKey.Right or VirtualKey.Down => this._selected + 1,
            _ => -1,
        };
        for (var index = start; index >= 0 && index < count; index += step)
        {
            if (this.TrySelectFromKeyboard(index))
            {
                e.Handled = true;
                return;
            }
        }

        e.Handled = this._selected >= 0 && isArrow;
    }

    private bool TrySelectFromKeyboard(int index)
    {
        if (!this.TryShow(index, out var description, refresh: true))
        {
            return false;
        }

        this._pointer = null;
        FrameworkElementAutomationPeer.FromElement(this._owner)?.RaiseNotificationEvent(
            AutomationNotificationKind.ActionCompleted, AutomationNotificationProcessing.MostRecent, description, "ChartInspection");
        return true;
    }

    private bool TryInvoke(int index)
    {
        if (index < 0 || index >= this._count() || !this._owner.IsEnabled || this._invoke?.Invoke(index) != true)
        {
            return false;
        }

        this.Clear();
        return true;
    }

    private bool TryShow(int index, out string description, bool refresh = false, bool waitForHover = false)
    {
        description = string.Empty;
        if (index < 0 || index >= this._count())
        {
            return false;
        }

        if (!refresh && index == this._selected)
        {
            return true;
        }

        if (this._read(index) is not ChartInspectionItem item || this._plot.XamlRoot is not { } root)
        {
            return false;
        }

        description = item.Text;
        var selectionChanged = this._selected != index;
        this._selected = index;
        this._highlight?.Invoke(index);
        var delay = ChartInspection.GetHoverDelay(this._owner);
        if (waitForHover && !this._isReadoutVisible && delay > TimeSpan.Zero)
        {
            if (selectionChanged || !this._hoverTimer.IsRunning)
            {
                this._hoverTimer.Stop();
                this._hoverTimer.Interval = delay;
                this._hoverTimer.Start();
            }

            return true;
        }

        this._hoverTimer.Stop();
        this._isReadoutVisible = true;
        if (this._popup is not null && this._card is not null)
        {
            this.ShowCard(root, index, item);
        }
        else if (this._fallback is not null)
        {
            this._fallback.Text = description;
            this._fallback.Visibility = Visibility.Visible;
        }

        return true;
    }

    private void ShowCard(XamlRoot root, int index, ChartInspectionItem item)
    {
        var popup = this._popup!;
        var card = this._card!;
        const double margin = 8;
        const double gap = 10;
        var bounds = root.Size;
        if (bounds.Width <= 2 * margin || bounds.Height <= 2 * margin)
        {
            return;
        }

        this.UpdateReadout(card, index, item);
        card.Measure(new Size(bounds.Width - 2 * margin, bounds.Height - 2 * margin));
        var size = card.DesiredSize;
        var anchor = this._plot.TransformToVisual(null).TransformPoint(item.Anchor);
        var origin = popup.TransformToVisual(null).TransformPoint(new Point());
        var opensRight = anchor.X + gap + size.Width <= bounds.Width - margin;
        var left = opensRight ? anchor.X + gap : anchor.X - gap - size.Width;
        var target = new Point(left - origin.X, anchor.Y - size.Height / 2 - origin.Y);
        var limits = new Rect(
            margin - origin.X, margin - origin.Y, Math.Max(0, bounds.Width - size.Width - 2 * margin),
            Math.Max(0, bounds.Height - size.Height - 2 * margin));
        this._cardMotion ??= new ChartInspectorMotion(
            this._owner, position =>
            {
                popup.HorizontalOffset = position.X;
                popup.VerticalOffset = position.Y;
            });
        this._cardMotion.MoveTo(target, limits, opensRight);
        popup.IsOpen = true;
    }

    [WinRT.DynamicWindowsRuntimeCast(typeof(StackPanel))]
    private void UpdateReadout(ContentControl card, int index, ChartInspectionItem item)
    {
        if (!Equals(card.Content, item.Text))
        {
            card.Content = item.Text;
        }

        if (!this._readoutLocated || card.Template != this._readoutTemplate)
        {
            card.ApplyTemplate();
            this._readoutTemplate = card.Template;
            this._readout = FindReadout(card);
            this._readoutLocated = true;
        }

        if (ChartInspection.GetContentTemplate(this._owner) is { } template)
        {
            this._customContent ??= new ChartInspectionContent();
            this._customContent.Update(item.Text, item.Rows, this._readContext?.Invoke(index));
            this._customPresenter ??= new ContentPresenter { Content = this._customContent, ContentTemplate = template };
            if (this._readout is { } customHost)
            {
                customHost.Content = this._customPresenter;
            }
            else
            {
                card.Content = this._customPresenter;
            }

            this._displayedRows = null;
            return;
        }

        if (this._readout is not { } readout)
        {
            return;
        }

        if (card.ContentTemplate is null && item.Rows is { } rows && rows.Any(row => row.Fill is not null || row.Stroke is not null))
        {
            if (readout.Content is not StackPanel || this._displayedRows is null || !rows.SequenceEqual(this._displayedRows))
            {
                readout.Content = CreateReadout(rows, card);
                this._displayedRows = rows;
            }
        }
        else
        {
            readout.Content = item.Text;
            this._displayedRows = null;
        }
    }

    private static ContentPresenter? FindReadout(DependencyObject root)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is ContentPresenter { Name: "PART_InspectorReadout" } presenter)
            {
                return presenter;
            }

            if (FindReadout(child) is { } nested)
            {
                return nested;
            }
        }

        return null;
    }

    private void ReleaseCustomReadout()
    {
        if (this._customPresenter is not null)
        {
            if (this._readout is { } readout && ReferenceEquals(readout.Content, this._customPresenter))
            {
                readout.Content = this._card?.Content;
            }
            else if (this._card is { } card && ReferenceEquals(card.Content, this._customPresenter))
            {
                card.Content = null;
            }
        }

        this._customContent = null;
        this._customPresenter = null;
    }

    private static StackPanel CreateReadout(IReadOnlyList<ChartInspectionRow> rows, ContentControl card)
    {
        var content = new StackPanel { Spacing = 3 };
        foreach (var row in rows)
        {
            var label = new TextBlock
            {
                Text = row.Text,
                TextWrapping = TextWrapping.Wrap,
                Foreground = card.Foreground,
                FontSize = card.FontSize,
                FontFamily = card.FontFamily,
            };
            if (row.Fill is null && row.Stroke is null)
            {
                content.Children.Add(label);
                continue;
            }

            var line = new Grid { ColumnSpacing = 6 };
            line.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            line.ColumnDefinitions.Add(new ColumnDefinition());
            line.Children.Add(
                new Rectangle
                {
                    Width = 8,
                    Height = 8,
                    RadiusX = 2,
                    RadiusY = 2,
                    Fill = row.Fill,
                    Stroke = row.Stroke,
                    StrokeThickness = row.Stroke is null ? 0 : 1,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 4, 0, 0),
                    HighContrastAdjustment = ElementHighContrastAdjustment.None,
                });
            Grid.SetColumn(label, 1);
            line.Children.Add(label);
            content.Children.Add(line);
        }

        return content;
    }

    internal static string FormatValue(double value, string? format = null, string fallback = "G6")
    {
        try
        {
            return value.ToString(format ?? fallback, CultureInfo.CurrentCulture);
        }
        catch (FormatException)
        {
            return value.ToString(fallback, CultureInfo.CurrentCulture);
        }
    }
}
