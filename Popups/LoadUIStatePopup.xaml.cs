using CommunityToolkit.Maui.Views;

namespace POIViewerMap.Popups;

public partial class LoadUIStatePopup : Popup
{
    public LoadUIStatePopup(string filename, string route)
    {
        InitializeComponent();
        this.POIFilenameLabel.Text = filename;
        this.RouteFilepathLabel.Text = route;
    }
    void OnOKButtonClicked(object? sender, EventArgs e) => CloseAsync();
}