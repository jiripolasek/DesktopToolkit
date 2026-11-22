// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace JPSoftworks.DesktopToolkit.Localization;

/// <summary>Configures the string resource loader shared by Desktop Toolkit controls.</summary>
public static class ToolkitLocalization
{
    private static readonly ConditionalWeakTable<object, Action> StringResourceListeners = new();
    private static IStringResourceLoader? _stringResourceLoader;
    private static long _stringResourceVersion;

    /// <summary>Gets or sets the loader used before the built-in RESW lookup.</summary>
    /// <remarks>Each assignment invalidates cached strings. Set it to null to use only the built-in sources.</remarks>
    public static IStringResourceLoader? StringResourceLoader
    {
        get => Volatile.Read(ref _stringResourceLoader);
        set
        {
            Volatile.Write(ref _stringResourceLoader, value);
            InvalidateStringResources();
        }
    }

    internal static long StringResourceVersion => Volatile.Read(ref _stringResourceVersion);

    internal static void RegisterStringResourceListener(object owner, Action refresh) => StringResourceListeners.Add(owner, refresh);

    /// <summary>Invalidates cached strings and refreshes controls observing the resources.</summary>
    /// <remarks>A listener that fails can retry on the next invalidation.</remarks>
    public static void InvalidateStringResources()
    {
        Interlocked.Increment(ref _stringResourceVersion);
        foreach (var listener in StringResourceListeners)
        {
            try
            {
                listener.Value();
            }
            catch (Exception error)
            {
                Trace.TraceError("String resource listener failed: {0}", error);
            }
        }
    }
}
