// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace JPSoftworks.DesktopToolkit.Controls.SearchableFlyouts.DevApp;

/// <summary>
/// The application window. This hosts a Frame that displays pages. Add your
/// UI and logic to MainPage.xaml / MainPage.xaml.cs instead of here so you
/// can use Page features such as navigation events and the Loaded lifecycle.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();

        this.ExtendsContentIntoTitleBar = true;
        this.SetTitleBar(this.AppTitleBar);

        this.AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico"));
        this.RootFrame.Loaded += this.OnContentLoaded;
        this.RootFrame.ActualThemeChanged += (_, _) => this.UpdateCaptionColors();

        // Navigate the root frame to the main page on startup.
        this.RootFrame.Navigate(typeof(MainPage));
    }

    private void OnContentLoaded(object sender, RoutedEventArgs args)
    {
        this.RootFrame.Loaded -= this.OnContentLoaded;
        this.UpdateCaptionColors();
        var scale = this.RootFrame.XamlRoot.RasterizationScale;
        var displayArea = DisplayArea.GetFromWindowId(this.AppWindow.Id, DisplayAreaFallback.Nearest);
        var workArea = displayArea.WorkArea;
        var width = Math.Min((int)(800 * scale), workArea.Width);
        var height = Math.Min((int)(760 * scale), workArea.Height);
        this.AppWindow.MoveAndResize(
            new RectInt32(workArea.X + (workArea.Width - width) / 2, workArea.Y + (workArea.Height - height) / 2, width, height),
            displayArea);
    }

    private void UpdateCaptionColors()
    {
        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            var foreground = new Windows.UI.ViewManagement.AccessibilitySettings().HighContrast
                ?
                new Windows.UI.ViewManagement.UISettings().GetColorValue(Windows.UI.ViewManagement.UIColorType.Foreground)
                : this.RootFrame.ActualTheme == ElementTheme.Dark
                    ? Colors.White
                    : Colors.Black;
            this.AppWindow.TitleBar.ButtonForegroundColor = foreground;
            this.AppWindow.TitleBar.ButtonInactiveForegroundColor = foreground;
        }
    }
}
