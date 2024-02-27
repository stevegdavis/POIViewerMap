
using POIViewerMap.Stores;

namespace POIViewerMap.Views;

public partial class AppUsagePage : ContentPage
{
    readonly IAppSettings appSettings;
	public AppUsagePage(IAppSettings appSettings)
	{
		InitializeComponent();
        this.appSettings = appSettings;
	}
    private void CheckBox_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        this.appSettings.ShowPopupAtStart = e.Value;
    }
    private async void Button_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}