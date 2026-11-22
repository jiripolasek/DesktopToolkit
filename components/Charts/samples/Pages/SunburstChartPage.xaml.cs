// ------------------------------------------------------------
// Copyright (c) Jiri Polasek. All rights reserved.
// ------------------------------------------------------------

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace JPSoftworks.DesktopToolkit.Controls.Demo.Pages;

public sealed partial class SunburstChartPage : Page
{
    public SunburstChartPage()
    {
        this.InitializeComponent();
        this.FolderExample.Xaml = """
                                  <charts:SunburstChart x:Name="Folders" Height="400" Data="{x:Bind FolderData}"
                                      MaxDepth="3" StartAngle="-90" ValueFormat="0.#' GB'"
                                      NodeInvoked="OnFolderInvoked" AutomationProperties.Name="Workspace folder sizes" />
                                  """;
        this.FolderExample.CSharp = """
                                    public HierarchyChartData FolderData { get; } = new(
                                        new HierarchyChartNode("workspace", "Workspace",
                                            new("projects", "Projects", new("web", "Web app", 26), new("mobile", "Mobile app", 19)),
                                            HierarchyChartNode.CreateDeferredBranch("packages", "Packages", 40)));

                                    private void OnFolderInvoked(object sender, HierarchyNodeInvokedEventArgs args)
                                    {
                                        if (args.Node.Children.Count > 0 && args.Node != Folders.Data?.Root)
                                        {
                                            args.Handled = true;
                                            Folders.Data = new HierarchyChartData(args.Node);
                                        }
                                    }
                                    """;
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
