// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using Serilog;
using Serilog.Core;
using Windows.Storage;

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

    internal void OpenLogFolder()
    {
        var folder = Directory.CreateDirectory(this.LogDirectory);
        using var process = Process.Start(new ProcessStartInfo(folder.FullName) { UseShellExecute = true });
    }

    internal string ExportLogs(string destinationDirectory)
    {
        if (string.IsNullOrEmpty(destinationDirectory))
        {
            throw new DirectoryNotFoundException("Windows couldn't find your desktop folder.");
        }

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var fileName = string.Create(
            CultureInfo.InvariantCulture, $"Desktop-Toolkit-Gallery-Report-{DateTime.Now:yyyyMMdd-HHmmss}-{suffix}.zip");
        var archivePath = Path.Combine(destinationDirectory, fileName);
        var temporaryPath = Path.ChangeExtension(archivePath, ".tmp");
        var files = Directory.GetFiles(this.LogDirectory, "gallery-*.log")
            .Concat(Directory.GetFiles(this.LogDirectory, "gallery-crash-*.txt"));

        var temporaryFile = File.Open(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        try
        {
            using (temporaryFile)
            using (var archive = new ZipArchive(temporaryFile, ZipArchiveMode.Create, leaveOpen: true))
            {
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

            File.Move(temporaryPath, archivePath);
            return archivePath;
        }
        catch
        {
            try
            {
                File.Delete(temporaryPath);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Keep the original export error.
            }

            throw;
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
