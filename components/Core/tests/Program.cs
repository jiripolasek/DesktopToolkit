// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using System.Reflection;
using System.Runtime.CompilerServices;
using JPSoftworks.DesktopToolkit.Localization;

string[] shippedFriends =
[
    "JPSoftworks.DesktopToolkit.Controls.Charts",
    "JPSoftworks.DesktopToolkit.Controls.DateTime",
    "JPSoftworks.DesktopToolkit.Controls.InfoLabel",
    "JPSoftworks.DesktopToolkit.Controls.KeyVisual",
    "JPSoftworks.DesktopToolkit.Controls.SearchableFlyouts",
];
var grants = typeof(ToolkitLocalization).Assembly.GetCustomAttributes<InternalsVisibleToAttribute>()
    .Select(attribute => attribute.AssemblyName);
var missing = shippedFriends.Except(grants, StringComparer.OrdinalIgnoreCase).ToArray();
if (missing.Length > 0)
{
    Console.Error.WriteLine($"Core no longer grants internal access to {string.Join(", ", missing)}.");
    return 1;
}

Console.WriteLine($"Verified Core friend access for all {shippedFriends.Length} shipped control assemblies.");
ListenerFailuresAreIsolated();
Console.WriteLine("PASS ListenerFailuresAreIsolated");
return 0;

static void ListenerFailuresAreIsolated()
{
    var owners = new[] { new object(), new object() };
    var calls = new int[owners.Length];
    var failNext = true;
    for (var i = 0; i < owners.Length; i++)
    {
        var index = i;
        ToolkitLocalization.RegisterStringResourceListener(owners[i], () =>
        {
            calls[index]++;
            if (failNext)
            {
                failNext = false;
                throw new InvalidOperationException("Expected listener failure.");
            }
        });
    }

    var previous = ToolkitLocalization.StringResourceLoader;
    var loader = new TestLoader();
    try
    {
        ToolkitLocalization.StringResourceLoader = loader;
        if (ToolkitLocalization.StringResourceLoader != loader || calls is not [1, 1])
        {
            throw new InvalidOperationException("Replacing the loader did not notify both listeners.");
        }

        failNext = true;
        ToolkitLocalization.InvalidateStringResources();
        if (calls is not [2, 2])
        {
            throw new InvalidOperationException("Invalidation did not retry both listeners after a failure.");
        }
    }
    finally
    {
        failNext = false;
        ToolkitLocalization.StringResourceLoader = previous;
        GC.KeepAlive(owners);
    }
}

file sealed class TestLoader : IStringResourceLoader
{
    public string? GetString(string catalogName, string key) => null;
}
