// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;
using JPSoftworks.DesktopToolkit.Localization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Data;
using Microsoft.Windows.Globalization;

namespace JPSoftworks.DesktopToolkit;

internal static class WinUIResourceStrings
{
    private const int LanguageCheckIntervalMilliseconds = 100;
    private const int FailedSourceRetryMilliseconds = 1000;
    private const int MaxCachedDateFormats = 64;
    private static readonly object CacheLock = new();
    private static readonly ConditionalWeakTable<string, CompositeFormat?> CompositeFormats = new();
    private static readonly ConcurrentDictionary<string, bool> DateFormatValidity = new(StringComparer.Ordinal);
    private static readonly ConditionalWeakTable<FrameworkElement, Binding> DefaultNameBindings = new();

    private static Catalog? _catalog;
    private static ResourceCache? _cache;

    public static void ObserveChanges(DependencyObject owner, Action refresh)
    {
        var dispatcher = owner.DispatcherQueue;
        ToolkitLocalization.RegisterStringResourceListener(
            owner, () =>
            {
                if (dispatcher.HasThreadAccess)
                {
                    refresh();
                }
                else
                {
                    dispatcher.TryEnqueue(RefreshOnDispatcher);
                }
            });

        void RefreshOnDispatcher()
        {
            try
            {
                refresh();
            }
            catch (Exception error)
            {
                Trace.TraceError("Dispatched string resource refresh failed: {0}", error);
            }
        }
    }

    public static string Get(string name)
    {
        return TryGet(name) ?? name;
    }

    public static string? TryGet(string name)
    {
        return Resolve(GetCache(), new(name, CultureInfo.CurrentUICulture.Name))?.Text;
    }

    public static string GetOverride(string? text, string name)
    {
        return string.IsNullOrWhiteSpace(text) ? Get(name) : text;
    }

    public static string? GetPlaceholderOverride(string? text)
    {
        return text == string.Empty || !string.IsNullOrWhiteSpace(text) ? text : null;
    }

    public static void SetTextValue(DependencyObject owner, DependencyProperty property, string? value)
    {
        if (value is null)
        {
            owner.ClearValue(property);
        }
        else
        {
            owner.SetValue(property, value);
        }
    }

    public static void SetDefaultAutomationName(FrameworkElement element, string? name)
    {
        var property = AutomationProperties.NameProperty;
        var binding = element.GetBindingExpression(property)?.ParentBinding;
        if (DefaultNameBindings.TryGetValue(element, out var previous) && binding == previous)
        {
            if (name is null)
            {
                element.ClearValue(property);
                DefaultNameBindings.Remove(element);
                return;
            }

            if (Equals(previous.Source, name))
            {
                return;
            }
        }
        else if (name is null
                 || binding is not null
                 || element.ReadLocalValue(property) != DependencyProperty.UnsetValue
                 || !string.IsNullOrWhiteSpace(AutomationProperties.GetName(element))
                 || AutomationProperties.GetLabeledBy(element) is not null)
        {
            return;
        }

        // Replacing our binding with a local name gives the template or app ownership.
        var next = new Binding { Source = name, Mode = BindingMode.OneTime };
        element.SetBinding(property, next);
        DefaultNameBindings.AddOrUpdate(element, next);
    }

    public static string FormatDefault(string name, params object[] args)
    {
        return Format(null, name, args);
    }

    public static string Format(string? format, string name, params object[] args)
    {
        return FormatValue(format, name, args, FormatComposite);
    }

    public static string FormatDateTime(string? format, string name, DateTimeOffset value)
    {
        return FormatValue(format, name, value, FormatDate);
    }

    private static string? FormatDate(string pattern, CultureInfo culture, DateTimeOffset value)
    {
        if (!DateFormatValidity.TryGetValue(pattern, out var valid))
        {
            if (DateFormatValidity.Count >= MaxCachedDateFormats)
            {
                DateFormatValidity.Clear();
            }

            valid = DateFormatValidity.GetOrAdd(pattern, IsValidDateFormat);
        }

        if (!valid)
        {
            return null;
        }

        var calendar = culture.DateTimeFormat.Calendar;
        if ((value.DateTime < calendar.MinSupportedDateTime || value.DateTime > calendar.MaxSupportedDateTime)
            && UsesCalendarDate(pattern, culture.DateTimeFormat))
        {
            culture = CultureInfo.InvariantCulture;
        }

        return value.ToString(pattern, culture);
    }

    private static bool UsesCalendarDate(string pattern, DateTimeFormatInfo formats)
    {
        if (pattern.Length == 1)
        {
            // The other standard formats include a date or already use the invariant calendar.
            if (pattern[0] is not ('t' or 'T'))
            {
                return true;
            }

            pattern = pattern[0] == 't' ? formats.ShortTimePattern : formats.LongTimePattern;
        }

        var quote = '\0';
        for (var index = 0; index < pattern.Length; index++)
        {
            var token = pattern[index];
            if (token == '\\')
            {
                index++;
            }
            else if (quote != '\0')
            {
                if (token == quote)
                {
                    quote = '\0';
                }
            }
            else if (token is '\'' or '"')
            {
                quote = token;
            }
            else if (token is 'd' or 'M' or 'y' or 'g')
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsValidDateFormat(string pattern)
    {
        try
        {
            // Validate syntax independently of the tick value and the caller's calendar.
            _ = DateTimeOffset.UnixEpoch.ToString(pattern, CultureInfo.InvariantCulture);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string? FormatComposite(string pattern, CultureInfo culture, object[] values)
    {
        var parsed = CompositeFormats.GetValue(
            pattern, static text =>
            {
                try
                {
                    return CompositeFormat.Parse(text);
                }
                catch (FormatException)
                {
                    return null;
                }
            });
        if (parsed is null || parsed.MinimumArgumentCount > values.Length)
        {
            return null;
        }

        // The parsed-format overload leaves escaped braces untouched in literal-only patterns.
        return parsed.MinimumArgumentCount == 0 ? string.Format(culture, pattern, values) : string.Format(culture, parsed, values);
    }

    private static string FormatValue<T>(string? format, string name, T value, Func<string, CultureInfo, T, string?> formatter)
    {
        if (TryFormat(format, value, formatter) is { } formatted)
        {
            return formatted;
        }

        var cache = GetCache();
        var key = new ResourceKey(name, CultureInfo.CurrentUICulture.Name);
        while (Resolve(cache, key) is { } candidate)
        {
            if (TryFormat(candidate.Text, value, formatter) is { } result)
            {
                return result;
            }

            if (candidate.Source == ResourceSource.English)
            {
                return TryFormat(candidate.Text, value, formatter, invariant: true) ?? candidate.Text;
            }

            key = key with { StartAt = candidate.Source + 1 };
        }

        return name;
    }

    private static string? TryFormat<T>(string? format, T value, Func<string, CultureInfo, T, string?> formatter, bool invariant = false)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return null;
        }

        try
        {
            var result = formatter(format, invariant ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture, value);
            return string.IsNullOrWhiteSpace(result) ? null : result;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static ResolvedString? Resolve(ResourceCache cache, ResourceKey key)
    {
        var cached = cache.Strings.TryGetValue(key, out var previous);
        if (cached && (previous.RetryAfter == long.MaxValue || Environment.TickCount64 < previous.RetryAfter))
        {
            return previous;
        }

        var catalog = GetCatalog();
        if (catalog is null || !catalog.English.TryGetValue(key.Name, out var english))
        {
            return null;
        }

        var resolved = new ResolvedString(english, ResourceSource.English);
        var retryAfter = long.MaxValue;
        for (var source = key.StartAt; source < ResourceSource.English; source++)
        {
            try
            {
                var text = source switch
                {
                    ResourceSource.Custom => cache.Loader?.GetString(catalog.Name, key.Name),
                    ResourceSource.App => ReswStringResourceLoader.Default.GetString(catalog.Name, key.Name),
                    ResourceSource.AppLibrary => ReswStringResourceLoader.Default.GetString(catalog.LibraryMapName, key.Name),
                    ResourceSource.StandaloneLibrary => catalog.Standalone.GetString(catalog.LibraryMapName, key.Name),
                    _ => catalog.Standalone.GetString(catalog.Name, key.Name),
                };
                if (!string.IsNullOrWhiteSpace(text))
                {
                    resolved = new(text, source);
                    break;
                }
            }
            catch (Exception)
            {
                // Retry a transient failure on the next read, then throttle repeated failures.
                retryAfter = cached ? Environment.TickCount64 + FailedSourceRetryMilliseconds : long.MinValue;
            }
        }

        resolved = resolved with { RetryAfter = retryAfter };
        if (retryAfter == long.MaxValue)
        {
            cache.Strings[key] = resolved;
        }
        else if (cached)
        {
            cache.Strings.TryUpdate(key, resolved, previous);
        }
        else
        {
            cache.Strings.TryAdd(key, resolved);
        }

        return resolved;
    }

    private static ResourceCache GetCache()
    {
        var version = ToolkitLocalization.StringResourceVersion;
        var now = Environment.TickCount64;
        var cache = Volatile.Read(ref _cache);
        if (cache is not null && cache.Version == version && now < Volatile.Read(ref cache.NextLanguageCheck))
        {
            return cache;
        }

        lock (CacheLock)
        {
            cache = _cache;
            version = ToolkitLocalization.StringResourceVersion;
            now = Environment.TickCount64;
            if (cache is not null && cache.Version == version && now < cache.NextLanguageCheck)
            {
                return cache;
            }

            // The app language override has no change notification.
            var language = ReadAppLanguage(cache?.AppLanguage);
            var nextCheck = Environment.TickCount64 + LanguageCheckIntervalMilliseconds;
            if (cache is null || cache.Version != version || cache.AppLanguage != language)
            {
                cache = new(version, language, ToolkitLocalization.StringResourceLoader, nextCheck);
                Volatile.Write(ref _cache, cache);
            }
            else
            {
                Volatile.Write(ref cache.NextLanguageCheck, nextCheck);
            }

            return cache;
        }
    }

    private static string? ReadAppLanguage(string? previous)
    {
        try
        {
            var language = ApplicationLanguages.PrimaryLanguageOverride;
            return string.IsNullOrEmpty(language) ? string.Join(";", ApplicationLanguages.Languages) : language;
        }
        catch (Exception)
        {
            return previous;
        }
    }

    private static Catalog? GetCatalog()
    {
        try
        {
            return LazyInitializer.EnsureInitialized(ref _catalog, LoadCatalog);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static Catalog LoadCatalog()
    {
        var assembly = typeof(WinUIResourceStrings).Assembly;
        var resource = assembly.GetManifestResourceNames().Single(name =>
            name.StartsWith("JPSoftworks.DesktopToolkit.", StringComparison.Ordinal) && name.EndsWith(".resw", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(resource)!;
        var english = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in XDocument.Load(stream).Elements("root").Elements("data"))
        {
            if ((string?)entry.Attribute("name") is { } key && (string?)entry.Element("value") is { } value)
            {
                english.TryAdd(key, value);
            }
        }

        var name = Path.GetFileNameWithoutExtension(resource);
        var assemblyName = assembly.GetName().Name;
        var standalone = new ReswStringResourceLoader(Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.pri"));
        return new(name, $"{assemblyName}/{name}", standalone, english);
    }

    private sealed record Catalog(
        string Name,
        string LibraryMapName,
        ReswStringResourceLoader Standalone,
        Dictionary<string, string> English);

    private sealed class ResourceCache(long version, string? appLanguage, IStringResourceLoader? loader, long nextLanguageCheck)
    {
        public long NextLanguageCheck = nextLanguageCheck;

        public long Version { get; } = version;

        public string? AppLanguage { get; } = appLanguage;

        public IStringResourceLoader? Loader { get; } = loader;

        public ConcurrentDictionary<ResourceKey, ResolvedString> Strings { get; } = new();
    }

    private readonly record struct ResourceKey(string Name, string CultureName, ResourceSource StartAt = ResourceSource.Custom);

    private readonly record struct ResolvedString(string Text, ResourceSource Source, long RetryAfter = long.MaxValue);

    private enum ResourceSource
    {
        Custom,
        App,
        AppLibrary,
        StandaloneLibrary,
        Standalone,
        English,
    }
}
