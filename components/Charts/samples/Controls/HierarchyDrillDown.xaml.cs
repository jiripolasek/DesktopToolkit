// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using JPSoftworks.DesktopToolkit.Charts;
using JPSoftworks.DesktopToolkit.Controls.Demo.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace JPSoftworks.DesktopToolkit.Controls.Demo.Controls;

[ContentProperty(Name = nameof(Chart))]
public sealed partial class HierarchyDrillDown : UserControl
{
    private static readonly ZoomFrame Rest = new(1, 0, 0, 1);
    private readonly UISettings _settings = new();
    private HierarchyChartData _data = FolderSizeSample.Summary;
    private string[] _path = [FolderSizeSample.Summary.Root.Key];
    private string[]? _destination;
    private Storyboard? _animation;
    private bool _inspectionWasEnabled;

    public Control? Chart
    {
        get => (Control?)this.ChartHost.Content;
        set
        {
            this.FinishNavigation();
            if (this.Chart is { } previous)
            {
                ChartContextMenu.SetProvider(previous, null);
            }

            if (this.Chart is TreemapChart previousTreemap)
            {
                previousTreemap.NodeInvoked -= this.OnNodeInvoked;
            }
            else if (this.Chart is IcicleChart previousIcicle)
            {
                previousIcicle.NodeInvoked -= this.OnNodeInvoked;
            }
            else if (this.Chart is SunburstChart previousSunburst)
            {
                previousSunburst.NodeInvoked -= this.OnNodeInvoked;
            }

            this.ChartHost.Content = value;
            if (value is TreemapChart treemap)
            {
                treemap.NodeInvoked += this.OnNodeInvoked;
            }
            else if (value is IcicleChart icicle)
            {
                icicle.NodeInvoked += this.OnNodeInvoked;
            }
            else if (value is SunburstChart sunburst)
            {
                sunburst.NodeInvoked += this.OnNodeInvoked;
            }

            if (value is not null)
            {
                ChartContextMenu.SetProvider(
                    value, new ChartDetailsMenuProvider(node => node.Key == this._path[^1] ? null : () => this.OpenFolder(node)));
            }

            this.UpdateChart();
        }
    }

    public HierarchyDrillDown()
    {
        this.InitializeComponent();
        this.UpdateTrail();
    }

    internal void LoadPackages()
    {
        this.FinishNavigation();
        this._data = FolderSizeSample.LoadPackages(this._data);
        this.UpdateChart();
    }

    private void OnNodeInvoked(object? sender, HierarchyNodeInvokedEventArgs args)
    {
        args.Handled = this.OpenFolder(args.Node);
    }

    private bool OpenFolder(HierarchyChartNode node)
    {
        if (this._destination is not null)
        {
            return true;
        }

        var root = this._data.FindNode(this._path[^1])!;
        var route = new List<string>();
        if (!FindRoute(root, node.Key, route) || route.Count == 0)
        {
            return false;
        }

        if (node.Children.Count == 0 && !node.IsDeferred)
        {
            route.RemoveAt(route.Count - 1);
        }

        if (route.Count == 0)
        {
            return false;
        }

        this.Navigate([.. this._path, .. route], route[^1], diving: true);
        return true;
    }

    private void OnFolderTrailItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        if (this._destination is null && args.Index < this._path.Length - 1)
        {
            var depth = args.Index + 1;
            this.Navigate(this._path[..depth], this._path[depth], diving: false);
            this.Chart?.Focus(FocusState.Programmatic);
        }
    }

    private void Navigate(string[] destination, string anchor, bool diving)
    {
        if (!this._settings.AnimationsEnabled || this.Chart is not { ActualWidth: > 0, ActualHeight: > 0 } chart)
        {
            this.SetPath(destination);
            return;
        }

        this._destination = destination;
        this._inspectionWasEnabled = ChartInspection.GetIsEnabled(chart);
        ChartInspection.SetIsEnabled(chart, false);
        if (diving)
        {
            this.Animate(
                Rest, this.ZoomTo(anchor), 240, () =>
                {
                    this.SetPath(destination);
                    this.Animate(Rest with { Opacity = 0 }, Rest, 150, this.FinishNavigation);
                });
        }
        else
        {
            this.SetPath(destination);
            this.Animate(this.ZoomTo(anchor), Rest, 320, this.FinishNavigation);
        }
    }

    private void Animate(ZoomFrame from, ZoomFrame to, int milliseconds, Action completed)
    {
        var storyboard = new Storyboard();
        Add(this.ZoomTransform, nameof(this.ZoomTransform.ScaleX), from.Scale, to.Scale);
        Add(this.ZoomTransform, nameof(this.ZoomTransform.ScaleY), from.Scale, to.Scale);
        Add(this.ZoomTransform, nameof(this.ZoomTransform.TranslateX), from.X, to.X);
        Add(this.ZoomTransform, nameof(this.ZoomTransform.TranslateY), from.Y, to.Y);
        Add(
            this.ChartHost, nameof(this.Opacity), from.Opacity, to.Opacity,
            from.Opacity > to.Opacity ? EasingMode.EaseIn : EasingMode.EaseOut);
        storyboard.Completed += (_, _) =>
        {
            if (this._animation == storyboard)
            {
                this._animation = null;
                storyboard.Stop();
                completed();
            }
        };
        this._animation = storyboard;
        storyboard.Begin();

        void Add(DependencyObject target, string property, double start, double end, EasingMode easing = EasingMode.EaseInOut)
        {
            var animation = new DoubleAnimation
            {
                From = start,
                To = end,
                Duration = TimeSpan.FromMilliseconds(milliseconds),
                EnableDependentAnimation = true,
                EasingFunction = new CubicEase { EasingMode = easing },
            };
            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation, property);
            storyboard.Children.Add(animation);
        }
    }

    private ZoomFrame ZoomTo(string key)
    {
        var bounds = Rect.Empty;
        var found = this.Chart switch
        {
            TreemapChart treemap => treemap.TryGetTileBounds(key, out bounds),
            IcicleChart icicle => icicle.TryGetTileBounds(key, out bounds),
            SunburstChart sunburst => sunburst.TryGetTileBounds(key, out bounds),
            _ => false,
        };
        if (!found)
        {
            return Rest with { Opacity = 0 };
        }

        var width = this.ChartHost.ActualWidth;
        var height = this.ChartHost.ActualHeight;
        var scale = Math.Clamp(Math.Max(width / bounds.Width, height / bounds.Height), 1.15, 4);
        return new ZoomFrame(
            scale, width / 2 - scale * (bounds.X + bounds.Width / 2), height / 2 - scale * (bounds.Y + bounds.Height / 2), 0);
    }

    private void FinishNavigation()
    {
        if (this._destination is not { } destination)
        {
            return;
        }

        this._destination = null;
        var animation = this._animation;
        this._animation = null;
        animation?.Stop();
        if (!this._path.SequenceEqual(destination))
        {
            this.SetPath(destination);
        }

        if (this.Chart is { } chart)
        {
            ChartInspection.SetIsEnabled(chart, this._inspectionWasEnabled);
        }
    }

    private void SetPath(string[] path)
    {
        this._path = path;
        this.UpdateChart();
        this.UpdateTrail();
    }

    private void UpdateTrail()
    {
        this.FolderTrail.ItemsSource = this._path.Select(key => this._data.FindNode(key)!.Label).ToArray();
    }

    private void UpdateChart()
    {
        var snapshot = this._path.Length == 1 ? this._data : new HierarchyChartData(this._data.FindNode(this._path[^1])!);
        if (this.Chart is TreemapChart treemap)
        {
            treemap.Data = snapshot;
        }
        else if (this.Chart is IcicleChart icicle)
        {
            icicle.Data = snapshot;
        }
        else if (this.Chart is SunburstChart sunburst)
        {
            sunburst.Data = snapshot;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        this.FinishNavigation();
    }

    private void OnViewportSizeChanged(object sender, SizeChangedEventArgs args)
    {
        this.ViewportClip.Rect = new Rect(0, 0, args.NewSize.Width, args.NewSize.Height);
        this.FinishNavigation();
    }

    private static bool FindRoute(HierarchyChartNode root, string key, List<string> route)
    {
        if (root.Key == key)
        {
            return true;
        }

        foreach (var child in root.Children)
        {
            route.Add(child.Key);
            if (FindRoute(child, key, route))
            {
                return true;
            }

            route.RemoveAt(route.Count - 1);
        }

        return false;
    }

    private readonly record struct ZoomFrame(double Scale, double X, double Y, double Opacity);
}
