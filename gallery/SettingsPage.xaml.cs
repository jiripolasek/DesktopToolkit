// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.ComponentModel;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel;

namespace JPSoftworks.DesktopToolkit.Gallery;

public sealed partial class SettingsPage : Page
{
    private static readonly (ElementTheme Theme, string Label)[] ThemeOptions =
    [
        (ElementTheme.Default, "Use system setting"),
        (ElementTheme.Light, "Light"),
        (ElementTheme.Dark, "Dark"),
    ];

    private readonly GalleryDiagnostics _diagnostics;
    private readonly ILogger<SettingsPage> _logger;

    private Uri WebsiteUri { get; } = new("https://jiripolasek.com");

    private Uri RepositoryUri { get; } = new(GalleryCatalog.RepositoryUrl);

    private Uri NewIssueUri { get; } = new($"{GalleryCatalog.RepositoryUrl}/issues/new?template=bug_report.yml");

    public SettingsPage()
    {
        var services = ((App)Application.Current).Services;
        this._diagnostics = services.GetRequiredService<GalleryDiagnostics>();
        this._logger = services.GetRequiredService<ILogger<SettingsPage>>();
        this.InitializeComponent();
        this.InitializeAbout();

        var currentTheme = GalleryPreferences.Theme;
        this.ThemePicker.ItemsSource = ThemeOptions.Select(option => option.Label).ToArray();
        this.ThemePicker.SelectedIndex = Array.FindIndex(ThemeOptions, option => option.Theme == currentTheme);
        this.ThemePicker.SelectionChanged += this.OnThemeChanged;
    }

    private void InitializeAbout()
    {
        const int noPackage = unchecked((int)0x80073D54);
        try
        {
            this.AboutExpander.Header = Package.Current.DisplayName;
        }
        catch (Exception exception) when (exception.HResult == noPackage)
        {
            this.AboutExpander.Header = "JPSoftworks Desktop Toolkit Gallery";
        }

        var assembly = typeof(SettingsPage).Assembly;
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+', 2)[0];
        this.AboutVersion.Text = $"Version {version ?? assembly.GetName().Version?.ToString()}";
    }

    private void OnOpenLogFolder(object sender, RoutedEventArgs args)
    {
        try
        {
            this._diagnostics.OpenLogFolder();
            this.LogFolderError.IsOpen = false;
            this._logger.LogInformation("Opened log folder");
        }
        catch (Exception exception) when (exception is Win32Exception or IOException or UnauthorizedAccessException)
        {
            this.LogFolderError.IsOpen = true;
            this._logger.LogWarning(exception, "Could not open the log folder");
        }
    }

    private async void OnExportLogs(object sender, RoutedEventArgs args)
    {
        this.ExportLogsButton.IsEnabled = false;
        this.LogExportStatus.IsOpen = false;

        try
        {
            var reportPath = await Task.Run(() =>
                this._diagnostics.ExportLogs(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)));
            var fileName = Path.GetFileName(reportPath);
            this.LogExportStatus.Severity = InfoBarSeverity.Success;
            this.LogExportStatus.Title = "Error report saved";
            this.LogExportStatus.Message = $"Saved {fileName} to your desktop.";
            this._logger.LogInformation("Exported error report {FileName}", fileName);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            this.LogExportStatus.Severity = InfoBarSeverity.Warning;
            this.LogExportStatus.Title = "Couldn't export logs";
            this.LogExportStatus.Message = exception.Message;
            this._logger.LogWarning(exception, "Could not export the error report");
        }
        finally
        {
            this.ExportLogsButton.IsEnabled = true;
        }

        this.LogExportStatus.IsOpen = true;
    }

    private void OnThemeChanged(object sender, SelectionChangedEventArgs args)
    {
        if (this.ThemePicker.SelectedIndex >= 0)
        {
            GalleryPreferences.Theme = ThemeOptions[this.ThemePicker.SelectedIndex].Theme;
        }
    }
}
