// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;

namespace JPSoftworks.DesktopToolkit.Controls.Demo;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
#if SMOKE_TESTS
        var arguments = Environment.GetCommandLineArgs();
        if (arguments.Length == 3 && arguments[1] == "--smoke-test")
        {
            UnhandledException += (_, error) =>
            {
                System.IO.File.WriteAllText(arguments[2], System.Text.Json.JsonSerializer.Serialize(
                    new { Passed = false, Error = error.Exception.ToString(), error.Message }));
                Environment.Exit(1);
            };
        }
#endif
        this.InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        this._window = new MainWindow();
        this._window.Activate();
#if SMOKE_TESTS
        var arguments = Environment.GetCommandLineArgs();
        if (arguments.Length == 3 && arguments[1] == "--smoke-test")
        {
            _ = Diagnostics.PackageSmokeTests.RunAsync((MainWindow)_window, arguments[2]);
        }
#endif
    }
}
