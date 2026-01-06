using CommunityToolkit.Maui.Views;

namespace POIViewerMap.Popups;
public partial class AppUsagePopup : Popup
{
    public bool ShowPopupAtStartup { get; set; } = true;
    public AppUsagePopup()
    {
        InitializeComponent();
    }
    void OnOKButtonClicked(object? sender, EventArgs e) => CloseAsync(new CancellationToken());// this.ShowPopupAtStartup);
    private void ShowAppUsage_CheckedChanged(object sender, CheckedChangedEventArgs e) => this.ShowPopupAtStartup = !e.Value;
}
