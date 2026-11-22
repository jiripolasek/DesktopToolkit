// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Windows.Storage;

namespace JPSoftworks.DesktopToolkit.Gallery;

internal static class GalleryPreferences
{
    public static event Action<ElementTheme>? ThemeChanged;

    private const string ThemeKey = "GalleryTheme";
    private static readonly IDictionary<string, object> Settings = OpenSettings();

    public static ElementTheme Theme
    {
        get => Settings.TryGetValue(ThemeKey, out var value)
               && value is string name
               && Enum.TryParse(name, out ElementTheme theme)
               && Enum.IsDefined(theme)
            ? theme
            : ElementTheme.Default;
        set
        {
            Settings[ThemeKey] = value.ToString();
            ThemeChanged?.Invoke(value);
        }
    }

    private static IDictionary<string, object> OpenSettings()
    {
        const int noPackage = unchecked((int)0x80073D54);
        try
        {
            return ApplicationData.Current.LocalSettings.Values;
        }
        catch (Exception exception) when (exception.HResult == noPackage)
        {
            return new Dictionary<string, object>();
        }
    }
}
