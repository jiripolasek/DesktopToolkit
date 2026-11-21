// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using JPSoftworks.DesktopToolkit.Charts;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.ApplicationModel.DataTransfer;

namespace JPSoftworks.DesktopToolkit.Controls.Demo.Services;

internal sealed class ChartDetailsMenuProvider(Func<HierarchyChartNode, Action?>? getOpenFolder = null) : IChartContextMenuProvider
{
    public static ChartDetailsMenuProvider Instance { get; } = new();

    public FlyoutBase CreateFlyout(ChartContextMenuRequest request)
    {
        var menu = new MenuFlyout();
        if (request.Values is [{ Item: HierarchyChartNode node }])
        {
            if ((node.Children.Count > 0 || node.IsDeferred) && getOpenFolder?.Invoke(node) is { } openFolder)
            {
                var open = new MenuFlyoutItem { Text = "Open folder" };
                open.Click += (_, _) => openFolder();
                menu.Items.Add(open);
            }

            AddCopyItem("Copy folder name", node.Label);
        }

        AddCopyItem("Copy inspection details", request.Description);
        return menu;

        void AddCopyItem(string label, string text)
        {
            var item = new MenuFlyoutItem { Text = label };
            item.Click += (_, _) =>
            {
                var data = new DataPackage();
                data.SetText(text);
                Clipboard.SetContent(data);
            };
            menu.Items.Add(item);
        }
    }
}
