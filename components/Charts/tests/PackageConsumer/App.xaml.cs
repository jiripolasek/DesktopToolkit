// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace ChartsPackageConsumer;

public sealed partial class App : Application
{
    public App()
    {
        UnhandledException += (_, args) => File.WriteAllText(Environment.GetCommandLineArgs()[1], args.Exception.ToString());
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var window = new Window { Title = "Charts package check" };
        window.AppWindow.Resize(new SizeInt32(600, 620));
        window.Activate();
        _ = VerifyAsync(window, Environment.GetCommandLineArgs()[1]);
    }

    private static async Task VerifyAsync(Window window, string report)
    {
        try
        {
            for (var iteration = 0; iteration < 2; iteration++)
            {
                var page = new ChartPage();
                window.Content = page;
                await page.VerifyAsync();
                window.Content = null;
                await Task.Delay(50);
            }

            File.WriteAllText(report, "Passed");
            Environment.Exit(0);
        }
        catch (Exception error)
        {
            File.WriteAllText(report, error.ToString());
            Environment.Exit(1);
        }
    }
}
