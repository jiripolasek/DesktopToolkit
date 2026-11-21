// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Serilog;

namespace JPSoftworks.DesktopToolkit.Gallery;

public partial class App : Application
{
    private readonly GalleryDiagnostics _diagnostics;
    private readonly ServiceProvider _services;
    private readonly ILogger<App> _logger;
    private MainWindow? _window;

    internal IServiceProvider Services => this._services;

    public App()
    {
        this._diagnostics = new GalleryDiagnostics();
        this._services = new ServiceCollection().AddSingleton(this._diagnostics)
            .AddLogging(builder => builder.AddSerilog(this._diagnostics.CreateLogger(), dispose: true))
            .AddSingleton(services => new MainWindow(services.GetRequiredService<ILogger<MainWindow>>())).BuildServiceProvider();
        this._logger = this._services.GetRequiredService<ILogger<App>>();

        this.UnhandledException += this.OnXamlUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += this.OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += this.OnUnobservedTaskException;
        this._logger.LogInformation("Gallery starting. Version {Version}", typeof(App).Assembly.GetName().Version);

        this.InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        if (this._window is null)
        {
            this._window = this._services.GetRequiredService<MainWindow>();
            this._window.Closed += this.OnWindowClosed;
        }

        this._window.Activate();
    }

    private void OnXamlUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs args)
    {
        this.TryWriteCrashReport("WinUI (XAML)", args.Exception, args.Message);
        this._logger.LogCritical(args.Exception, "Unhandled XAML exception: {Message}", args.Message);
    }

    private void OnDomainUnhandledException(object sender, System.UnhandledExceptionEventArgs args)
    {
        var exception = args.ExceptionObject as Exception;
        if (args.IsTerminating)
        {
            this.TryWriteCrashReport(".NET runtime", exception);
        }

        this._logger.LogCritical(exception, "Unhandled runtime exception. Terminating {IsTerminating}", args.IsTerminating);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        this._logger.LogError(args.Exception, "Unobserved task exception");
    }

    private void TryWriteCrashReport(string source, Exception? exception, string? message = null)
    {
        try
        {
            this._diagnostics.WriteCrashReport(source, exception, message);
        }
        catch (Exception reportException)
        {
            this._logger.LogError(reportException, "Could not write the crash report");
        }
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        this.UnhandledException -= this.OnXamlUnhandledException;
        AppDomain.CurrentDomain.UnhandledException -= this.OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException -= this.OnUnobservedTaskException;
        this._logger.LogInformation("Gallery closed");
        this._services.Dispose();
    }
}
