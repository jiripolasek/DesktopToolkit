// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Collections.Concurrent;
using Microsoft.Windows.ApplicationModel.Resources;

namespace JPSoftworks.DesktopToolkit.Localization;

internal sealed class ReswStringResourceLoader
{
    private readonly string? _priFilePath;
    private readonly ConcurrentDictionary<string, ResourceMap> _catalogs = new(StringComparer.Ordinal);
    private ResourceMap? _mainMap;

    internal static ReswStringResourceLoader Default { get; } = new();

    internal ReswStringResourceLoader(string? priFilePath = null)
    {
        this._priFilePath = priFilePath;
    }

    internal string? GetString(string catalogName, string key)
    {
        if (!this._catalogs.TryGetValue(catalogName, out var map))
        {
            var mainMap = LazyInitializer.EnsureInitialized(ref this._mainMap, this.OpenMainMap);
            map = mainMap.TryGetSubtree(catalogName);
            if (map is null)
            {
                return null;
            }

            this._catalogs.TryAdd(catalogName, map);
        }

        return map.TryGetValue(key.Replace('.', '/'))?.ValueAsString;
    }

    private ResourceMap OpenMainMap()
    {
        var manager = this._priFilePath is null ? new ResourceManager() : new ResourceManager(this._priFilePath);
        return manager.MainResourceMap;
    }
}
