// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;

namespace JPSoftworks.DesktopToolkit.Controls.SearchableFlyouts.DevApp;

internal sealed partial class MainWindow : Window
{
    private readonly MainPage _page = new();

    public MainWindow()
    {
        this.Title = "Filterable context menu";
        this.SystemBackdrop = new MicaBackdrop();
        this.AppWindow.SetIcon("Assets/AppIcon.ico");
        this._page.Loaded += this.OnContentLoaded;
        this.Content = this._page;
    }

    private void OnContentLoaded(object sender, RoutedEventArgs args)
    {
        this._page.Loaded -= this.OnContentLoaded;
        var scale = this._page.XamlRoot.RasterizationScale;
        var display = DisplayArea.GetFromWindowId(this.AppWindow.Id, DisplayAreaFallback.Nearest);
        var workArea = display.WorkArea;
        var width = Math.Min((int)(800 * scale), workArea.Width);
        var height = Math.Min((int)(760 * scale), workArea.Height);
        this.AppWindow.MoveAndResize(
            new RectInt32(workArea.X + (workArea.Width - width) / 2, workArea.Y + (workArea.Height - height) / 2, width, height),
            display);
    }
}
