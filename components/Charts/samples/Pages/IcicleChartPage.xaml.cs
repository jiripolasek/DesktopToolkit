// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using JPSoftworks.DesktopToolkit.Charts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace JPSoftworks.DesktopToolkit.Controls.Demo.Pages;

public sealed partial class IcicleChartPage : Page
{
    public IcicleChartPage()
    {
        this.InitializeComponent();
        this.FolderExample.Xaml = """
                                  <charts:IcicleChart x:Name="Folders" Height="360" Data="{x:Bind FolderData}"
                                      Orientation="Vertical" MaxDepth="3" ShowValueLabels="True" ValueFormat="0.#' GB'"
                                      NodeInvoked="OnFolderInvoked"
                                      AutomationProperties.Name="Workspace folder sizes by hierarchy level" />
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

    private void OnOrientationToggled(object sender, RoutedEventArgs args)
    {
        if (this.Folders is not null)
        {
            this.Folders.Orientation = ((ToggleSwitch)sender).IsOn ? Orientation.Horizontal : Orientation.Vertical;
        }
    }

    private void OnLoadPackageFolders(object sender, RoutedEventArgs args)
    {
        this.FolderNavigation.LoadPackages();
        this.LoadPackageFoldersButton.IsEnabled = false;
    }

    private void OnLevelsChanged(object sender, SelectionChangedEventArgs args)
    {
        if (this.Folders is not null && sender is ComboBox { SelectedIndex: >= 0 } picker)
        {
            this.Folders.MaxDepth = picker.SelectedIndex + 1;
        }
    }
}
