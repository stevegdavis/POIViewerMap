using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Projections;
using Mapsui.Tiling;
using Mapsui.UI;
using Mapsui.UI.Maui;
using Microsoft.Maui.Controls;
using POIViewerMap.ViewModels;
using MyMap = Mapsui.Map;

namespace POIViewerMap.Views;

public partial class MapViewPage : ContentPage
{
    public MapViewPage(MapViewPageViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
        vm.Map.Layers.Add(OpenStreetMap.CreateTileLayer());
        var extent = GetLimitsOfStroud();
        vm.Map.Home = n => n.NavigateTo(extent);
        vm.Map.RotationLock = true;
        mapView.Map = vm.Map;
        mapView.IsZoomButtonVisible = true;
        mapView.IsMyLocationButtonVisible = true;
        mapView.IsNorthingButtonVisible = true;
        var mapControl = new Mapsui.UI.Maui.MapControl();
        mapControl.Navigator.CenterOn(SphericalMercator.FromLonLat(-2.2539759, 51.7476017).ToMPoint());
        vm.Map.RotationLock = true;
        // From GPS - not windows TODO iOS
        if (DeviceInfo.Current.Platform == DevicePlatform.Android)
            GetCurrentDeviceLocation();
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if(DeviceInfo.Current.Version.Major >= 11)
            WeakReferenceMessenger.Default.Send(new FullScreenMessage("HideOsNavigationBar"));
    }
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (DeviceInfo.Current.Version.Major >= 11)
            WeakReferenceMessenger.Default.Send(new NormalScreenMessage("NormalNavigationBar"));
    }
    private async void GetCurrentDeviceLocation()
    {
        var request = new GeolocationRequest(GeolocationAccuracy.Best);
        var location = await Geolocation.GetLocationAsync(request, new CancellationToken());
        if (location != null)
        {
            mapView.MyLocationLayer.UpdateMyLocation(new Mapsui.UI.Maui.Position(location.Latitude, location.Longitude));
        }
    }
    private MRect GetLimitsOfStroud()
    {
        var (minX, minY) = SphericalMercator.FromLonLat(-2.1488, 51.79797);
        var (maxX, maxY) = SphericalMercator.FromLonLat(-2.3434, 51.65957);
        return new MRect(minX, minY, maxX, maxY);
    }
}
public class FullScreenMessage : ValueChangedMessage<object>
{
    public FullScreenMessage(object r) : base(r)
    {
    }
}

public class NormalScreenMessage : ValueChangedMessage<object>
{
    public NormalScreenMessage(object r) : base(r)
    {
    }
}