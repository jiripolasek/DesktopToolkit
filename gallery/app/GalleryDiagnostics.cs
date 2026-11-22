// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Globalization;
using System.IO.Compression;
using System.Text;
using Serilog;
using Serilog.Core;
using Windows.Storage;
using Windows.System;
using WinRT.Interop;

namespace JPSoftworks.DesktopToolkit.Gallery;

internal sealed class GalleryDiagnostics
{
    private string LogDirectory { get; } = GetLogDirectory();

    internal Logger CreateLogger()
    {
        return new LoggerConfiguration().Enrich.FromLogContext().WriteTo.File(
            Path.Combine(this.LogDirectory, "gallery-.log"),
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}",
            rollingInterval: RollingInterval.Day, fileSizeLimitBytes: 5 * 1024 * 1024, rollOnFileSizeLimit: true,
            retainedFileCountLimit: 14, shared: true).CreateLogger();
    }

    internal void WriteCrashReport(string source, Exception? exception, string? message)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var fileName = string.Create(CultureInfo.InvariantCulture, $"gallery-crash-{timestamp:yyyyMMdd-HHmmssfff}-{Guid.NewGuid():N}.txt");
        var report = new StringBuilder().AppendLine("JPSoftworks Desktop Toolkit Gallery")
            .AppendLine($"Version: {typeof(GalleryDiagnostics).Assembly.GetName().Version}").AppendLine($"Timestamp (UTC): {timestamp:O}")
            .AppendLine($"Source: {source}").AppendLine();

        if (!string.IsNullOrWhiteSpace(message))
        {
            report.AppendLine(message);
        }

        report.AppendLine(exception?.ToString());

        Directory.CreateDirectory(this.LogDirectory);
        File.WriteAllText(Path.Combine(this.LogDirectory, fileName), report.ToString());
    }

    internal async Task OpenLogFolderAsync(nint ownerWindowHandle)
    {
        var directory = Directory.CreateDirectory(this.LogDirectory);
        var folder = await StorageFolder.GetFolderFromPathAsync(directory.FullName);
        var options = new FolderLauncherOptions();
        InitializeWithWindow.Initialize(options, ownerWindowHandle);
        if (!await Launcher.LaunchFolderAsync(folder, options))
        {
            throw new IOException("Windows couldn't open the log folder.");
        }
    }

    internal async Task<string?> ExportLogsAsync(Func<Task<string?>> pickDestinationAsync)
    {
        var archivePath = Path.Combine(Path.GetTempPath(), $"gallery-export-{Guid.NewGuid():N}.tmp");
        try
        {
            await Task.Run(() => this.WriteLogArchive(archivePath));

            var destinationPath = await pickDestinationAsync();
            if (destinationPath is not null)
            {
                await Task.Run(() => SaveLogArchive(archivePath, destinationPath));
            }

            return destinationPath;
        }
        finally
        {
            DeleteTemporaryArchive(archivePath);
        }
    }

    private static void SaveLogArchive(string archivePath, string destinationPath)
    {
        var temporaryPath = Path.Join(Path.GetDirectoryName(destinationPath), $"gallery-export-{Guid.NewGuid():N}.tmp");
        try
        {
            File.Copy(archivePath, temporaryPath);
            File.Move(temporaryPath, destinationPath, overwrite: true);
        }
        finally
        {
            DeleteTemporaryArchive(temporaryPath);
        }
    }

    private static void DeleteTemporaryArchive(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Temporary file cleanup must not affect the export result.
        }
    }

    private void WriteLogArchive(string temporaryPath)
    {
        var files = Directory.Exists(this.LogDirectory)
            ? Directory.EnumerateFiles(this.LogDirectory, "gallery-*.log").Concat(
                Directory.EnumerateFiles(this.LogDirectory, "gallery-crash-*.txt"))
            : [];
        using var temporaryFile = File.Open(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var archive = new ZipArchive(temporaryFile, ZipArchiveMode.Create);
        var hasEntries = false;

        foreach (var path in files)
        {
            FileStream logFile;
            try
            {
                logFile = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            }
            catch (FileNotFoundException)
            {
                // A rolling log can disappear during export.
                continue;
            }

            using (logFile)
            using (var entry = archive.CreateEntry(Path.GetFileName(path)).Open())
            {
                logFile.CopyTo(entry);
            }

            hasEntries = true;
        }

        if (!hasEntries)
        {
            throw new IOException("There are no gallery logs or crash reports to export.");
        }
    }

    private static string GetLogDirectory()
    {
        const int noPackage = unchecked((int)0x80073D54);

        try
        {
            return Path.Combine(ApplicationData.Current.LocalFolder.Path, "Logs");
        }
        catch (Exception exception) when (exception.HResult == noPackage)
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JPSoftworks", "DesktopToolkit.Gallery", "Logs");
        }
    }
}
