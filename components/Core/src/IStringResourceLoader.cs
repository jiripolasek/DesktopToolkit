// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

namespace JPSoftworks.DesktopToolkit.Localization;

/// <summary>Supplies localized strings for Desktop Toolkit controls.</summary>
public interface IStringResourceLoader
{
    /// <summary>Gets a string by its catalog and resource key.</summary>
    /// <param name="catalogName">The resource catalog name without the .resw extension.</param>
    /// <param name="key">The resource key as written in the catalog.</param>
    /// <returns>The localized string, or null or whitespace to use the next resource source.</returns>
    string? GetString(string catalogName, string key);
}
