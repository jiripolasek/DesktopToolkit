// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.IO;
using System.Runtime.InteropServices;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;

namespace JPSoftworks.DesktopToolkit.Controls.Demo.Controls;

internal static class ChartStylingGuide
{
    private const string StylingHeading = "\n## Styling and theming\n";
    private const string ExampleFence = "\n```xml\n";
    private static readonly Lazy<Dictionary<string, Section>> Sections = new(ReadSections);

    public static Section? Get(string key)
    {
        return Sections.Value.GetValueOrDefault(key);
    }

    public static ScrollViewer CreateView(Section guide)
    {
        var content = new StackPanel { Spacing = 12 };
        if (guide.Xaml is { } example)
        {
            var code = new TextBox
            {
                AcceptsReturn = true,
                IsReadOnly = true,
                IsTabStop = false,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                FontFamily = new FontFamily("Consolas"),
                Text = example,
            };
            AutomationProperties.SetName(code, "XAML style example");
            var copy = new Button { Content = "Copy XAML" };
            copy.Click += (_, _) => CopyExample(copy, example);
            var sourceHeader = new Grid { ColumnSpacing = 12 };
            sourceHeader.ColumnDefinitions.Add(new ColumnDefinition());
            sourceHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var title = new TextBlock
            {
                Text = "XAML style", FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(copy, 1);
            sourceHeader.Children.Add(title);
            sourceHeader.Children.Add(copy);
            content.Children.Add(sourceHeader);
            content.Children.Add(code);
        }

        AddEntries(content, guide);
        if (Get("Shared styling") is { } shared)
        {
            content.Children.Add(new TextBlock { Text = "Shared styling", FontWeight = FontWeights.SemiBold });
            AddEntries(content, shared);
        }

        return new ScrollViewer { Content = content, MaxHeight = 440 };
    }

    private static void AddEntries(StackPanel content, Section guide)
    {
        foreach (var entry in guide.Entries)
        {
            var text = new TextBlock { TextWrapping = TextWrapping.Wrap, IsTextSelectionEnabled = true };
            if (entry.StartsWith("**", StringComparison.Ordinal)
                && entry.IndexOf("** ", 2, StringComparison.Ordinal) is > 2 and var headingEnd)
            {
                text.Inlines.Add(new Run { Text = entry[2..headingEnd], FontWeight = FontWeights.SemiBold });
                text.Inlines.Add(new LineBreak());
                text.Inlines.Add(new Run { Text = PlainText(entry[(headingEnd + 3)..]) });
            }
            else
            {
                text.Text = PlainText(entry);
            }

            content.Children.Add(text);
        }
    }

    private static string PlainText(string markdown)
    {
        return markdown.Replace("`", string.Empty).Replace("**", string.Empty);
    }

    private static void CopyExample(Button button, string example)
    {
        var data = new DataPackage();
        data.SetText(example);
        try
        {
            Clipboard.SetContent(data);
            button.Content = "Copied";
        }
        catch (COMException)
        {
            button.Content = "Copy failed";
        }
    }

    private static Dictionary<string, Section> ReadSections()
    {
        var sections = new Dictionary<string, Section>(StringComparer.Ordinal);
        using var stream = typeof(ChartStylingGuide).Assembly.GetManifestResourceStream("ChartsGuide.md");
        if (stream is null)
        {
            return sections;
        }

        using var reader = new StreamReader(stream);
        var markdown = reader.ReadToEnd().ReplaceLineEndings("\n");
        var start = markdown.IndexOf(StylingHeading, StringComparison.Ordinal);
        if (start < 0)
        {
            return sections;
        }

        start += StylingHeading.Length;
        var end = markdown.IndexOf("\n## ", start, StringComparison.Ordinal);
        var group = markdown[start..(end >= 0 ? end : markdown.Length)];
        foreach (var block in group.Split("\n### ", StringSplitOptions.None).Skip(1))
        {
            var titleEnd = block.IndexOf('\n');
            var fence = block.IndexOf(ExampleFence, StringComparison.Ordinal);
            var prose = block[(titleEnd + 1)..(fence >= 0 ? fence : block.Length)];
            var entries = prose.Split('\n').Where(line => line.StartsWith("- ", StringComparison.Ordinal)).Select(line => line[2..])
                .ToArray();
            string? example = null;
            if (fence >= 0)
            {
                var codeStart = fence + ExampleFence.Length;
                var codeEnd = block.IndexOf("\n```", codeStart, StringComparison.Ordinal);
                example = codeEnd >= 0 ? block[codeStart..codeEnd] : null;
            }

            sections.Add(block[..titleEnd], new Section(entries, example));
        }

        return sections;
    }

    internal sealed record Section(string[] Entries, string? Xaml);
}
