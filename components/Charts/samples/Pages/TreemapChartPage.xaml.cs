// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using JPSoftworks.DesktopToolkit.Charts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace JPSoftworks.DesktopToolkit.Controls.Demo.Pages;

public sealed partial class TreemapChartPage : Page
{
    public TreemapChartPage()
    {
        this.InitializeComponent();
        this.FolderExample.Xaml = """
                                  <charts:TreemapChart x:Name="Folders" Height="360" Data="{x:Bind FolderData}"
                                      ValueFormat="0.#' GB'" MaxDepth="3"
                                      TileSpacing="4" BranchPadding="10" TileCornerRadius="5"
                                      ShowValueLabels="True" ValueLabelOpacity="0.83"
                                      NodeInvoked="OnFolderInvoked"
                                      AutomationProperties.Name="Workspace folder sizes in gigabytes" />
                                  """;
        this.FolderExample.CSharp = """
                                    public HierarchyChartData FolderData { get; } = new(
                                        new HierarchyChartNode("workspace", "Workspace",
                                            new("projects", "Projects", new("web", "Web app", 26), new("mobile", "Mobile app", 19)),
                                            HierarchyChartNode.CreateDeferredBranch("packages", "Packages", 40),
                                            new("tools", "Tools", 8)));

                                    private void OnFolderInvoked(object sender, HierarchyNodeInvokedEventArgs args)
                                    {
                                        if (args.Node.Children.Count > 0 && args.Node != Folders.Data?.Root)
                                        {
                                            // Animate toward TryGetTileBounds before swapping the snapshot.
                                            args.Handled = true;
                                            Folders.Data = new HierarchyChartData(args.Node);
                                        }
                                    }
                                    """;
    }

    private void OnLayoutChanged(object sender, SelectionChangedEventArgs args)
    {
        if (this.Folders is not null)
        {
            this.Folders.Layout = ((ComboBox)sender).SelectedIndex switch
            {
                1 => TreemapLayouts.SliceAndDice,
                2 => TreemapLayouts.Binary,
                _ => null,
            };
        }
    }

    private void OnLevelsChanged(object sender, SelectionChangedEventArgs args)
    {
        if (this.Folders is not null && sender is ComboBox { SelectedIndex: >= 0 } picker)
        {
            this.Folders.MaxDepth = picker.SelectedIndex + 1;
        }
    }

    private void OnLoadPackageFolders(object sender, RoutedEventArgs args)
    {
        this.FolderNavigation.LoadPackages();
        this.LoadPackageFoldersButton.IsEnabled = false;
    }
}
