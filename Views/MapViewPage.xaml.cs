using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Projections;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using POIViewerMap.DataClasses;
using POIViewerMap.Helpers;
using POIViewerMap.Resources.Strings;
using System.Reflection;

namespace POIViewerMap.Views;

public partial class MapViewPage : ContentPage
{
    private string Filepath;
    private string FullFilepath;
    static string drinkingwaterStr = null;
    static string campsiteStr = null;
    static string bicycleshopStr = null;
    static string supermarketStr = null;
    static string bicyclerepairstationStr = null;
    private List<POIData> pois = new();
    private static Location myCurrentLocation;

    public MapViewPage()
	{
		InitializeComponent();
        var assembly = typeof(App).GetTypeInfo().Assembly;
        var assemblyName = assembly.GetName().Name;
        using var drinkingwater = assembly.GetManifestResourceStream($"{assemblyName}.Resources.Images.waterlightblue.svg");
        if (drinkingwater != null)
        {
            using StreamReader reader = new(drinkingwater!);
            drinkingwaterStr = reader.ReadToEnd();
        }
        using var campsite = assembly.GetManifestResourceStream($"{assemblyName}.Resources.Images.camping.svg");
        if (campsite != null)
        {
            using StreamReader reader = new(campsite!);
            campsiteStr = reader.ReadToEnd();
        }
        using var bicycleshop = assembly.GetManifestResourceStream($"{assemblyName}.Resources.Images.bicycle.svg");
        if (bicycleshop != null)
        {
            using StreamReader reader = new(bicycleshop!);
            bicycleshopStr = reader.ReadToEnd();
        }
        using var bicyclerepairstation = assembly.GetManifestResourceStream($"{assemblyName}.Resources.Images.spanner.svg");
        if (bicyclerepairstation != null)
        {
            using StreamReader reader = new(bicyclerepairstation!);
            bicyclerepairstationStr = reader.ReadToEnd();
        }
        using var supermarket = assembly.GetManifestResourceStream($"{assemblyName}.Resources.Images.shopping-cart.svg");
        if (supermarket != null)
        {
            using StreamReader reader = new(supermarket!);
            supermarketStr = reader.ReadToEnd();
        }
        mapView.Map.Layers.Add(OpenStreetMap.CreateTileLayer());
        var extent = GetLimitsOfStroud();
        mapView.Map.Home = n => n.NavigateTo(extent);
        mapView.Map.RotationLock = true;
        mapView.Map = mapView.Map;
        mapView.IsZoomButtonVisible = true;
        mapView.IsMyLocationButtonVisible = true;
        mapView.IsNorthingButtonVisible = true;
        var mapControl = new Mapsui.UI.Maui.MapControl();
        mapView.PinClicked += OnPinClicked;
        mapView.MapClicked += OnMapClicked;
        mapControl.Navigator.CenterOn(SphericalMercator.FromLonLat(-2.2539759, 51.7476017).ToMPoint());
        // From GPS - not windows TODO iOS
        if (DeviceInfo.Current.Platform == DevicePlatform.Android)
            GetCurrentDeviceLocation();
    }
    private void OnMapClicked(object sender, MapClickedEventArgs e)
    {
        foreach (var pin in mapView.Pins)
        {
            pin.HideCallout();
        }
    }
    private void OnPinClicked(object sender, PinClickedEventArgs e)
    {
        if (e.Pin != null)
        {
            if (e.NumOfTaps == 2)
            {
                // Hide Pin when double click
                e.Pin.IsVisible = false;
            }
            if (e.NumOfTaps == 1)
                if (e.Pin.Callout.IsVisible)
                    e.Pin.HideCallout();
                else
                    e.Pin.ShowCallout();
        }
        e.Handled = true;
    }
    private string GetPOIString(POIType poi)
    {
        switch(poi) 
        {
            case POIType.DrinkingWater:
                return "Drinking Water";
            case POIType.Campsite:
                return "Campsite";
            case POIType.BicycleShop:
                return "Bicycle Shop";
            case POIType.BicycleRepairStation:
                return "Bicycle Repair Station";
            case POIType.Supermarket:
                return "Supermarket/Convenience Store";
        }
        return string.Empty;
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
    async void BrowseButton_Clicked(object sender, EventArgs e)
    {
        pois.Clear();
        await BrowsePOIs();
        this.POITypeLabel.Text = AppResource.POIsLoadingMsg;
        pois = await ReadPOIs.ReadAysnc(FullFilepath);
        await PopulateMapAsync(pois);
        this.POITypeLabel.Text = GetPOIString(pois.Count > 0 ? pois[0].POI : POIType.Unknown);
    }
    private async Task BrowsePOIs()
    {
        var customFileType = new FilePickerFileType(
             new Dictionary<DevicePlatform, IEnumerable<string>>
             {
                    { DevicePlatform.iOS, new[] { "public.my.osm.extension" } }, // UTType values
                    { DevicePlatform.Android, new[] { "text/plain" } }, // MIME type
                    { DevicePlatform.WinUI, new[] { ".txt" } }, // file extension
                    { DevicePlatform.Tizen, new[] { "*/*" } },
                    { DevicePlatform.macOS, new[] { "txt" } }, // UTType values
             });

        PickOptions options = new()
        {
            PickerTitle = "Please select a TXT file",
            FileTypes = customFileType,
        };
        await PickAndShow(options);
    }
    public async Task<FileResult> PickAndShow(PickOptions options)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(options);
            if (result != null)
            {
                this.Filepath = result.FileName;
                this.FullFilepath = result?.FullPath;
                this.FilepathLabel.Text = result?.FileName;
            }
            return result;
        }
        catch (Exception ex)
        {
            // The user canceled or something went wrong
        }
        return null;
    }
    private async void GetCurrentDeviceLocation()
    {
        var request = new GeolocationRequest(GeolocationAccuracy.Best);
        myCurrentLocation = await Geolocation.GetLocationAsync(request, new CancellationToken());
        if (myCurrentLocation != null)
        {
            mapView.MyLocationLayer.UpdateMyLocation(new Mapsui.UI.Maui.Position(myCurrentLocation.Latitude, myCurrentLocation.Longitude));
        }
    }
    private MRect GetLimitsOfStroud()
    {
        var (minX, minY) = SphericalMercator.FromLonLat(-2.1488, 51.79797);
        var (maxX, maxY) = SphericalMercator.FromLonLat(-2.3434, 51.65957);
        return new MRect(minX, minY, maxX, maxY);
    }
    private async Task PopulateMapAsync(List<POIData> pois)
    {
        await Task.Factory.StartNew(delegate
        {
            foreach (var pin in mapView.Pins)
            {
                pin.HideCallout();
            }
            mapView.Pins.Clear();
            var myLocation = new Location(mapView.MyLocationLayer.MyLocation.Latitude, mapView.MyLocationLayer.MyLocation.Longitude);
            foreach (var poi in pois)
            {
                var distance = (int)Location.CalculateDistance(poi.Latitude, 
                                                                poi.Longitude, 
                                                                new Location(mapView.MyLocationLayer.MyLocation.Latitude, mapView.MyLocationLayer.MyLocation.Longitude), 
                                                                DistanceUnits.Kilometers);
                if (distance > Convert.ToDouble(this.EntryDistance.Text))
                    continue;
                var space = string.Empty;
                if (!String.IsNullOrEmpty(poi.Subtitle))
                {
                    space = "\r";
                }
                var myPin = new Pin(mapView)
                {
                    Position = new Position(poi.Latitude, poi.Longitude),
                    Type = PinType.Svg,
                    Label = $"{poi.Title}\r{poi.Subtitle}{space}Distance: {((int)distance)}km",
                    Address = "",
                    Svg = GetPOIIcon(poi),// eg. drinkingwaterStr,
                    Scale = 0.03988F
                };

                //myPin.HideCallout();
                myPin.Callout.TitleTextAlignment = TextAlignment.Start;
                myPin.Callout.ArrowHeight = 15;
                myPin.Callout.TitleFontSize = 15;
                mapView.Pins.Add(myPin);
            }
        });
    }
    private string GetPOIIcon(POIData poi)
    {
        switch(poi.POI)
        {
            case POIType.DrinkingWater: return drinkingwaterStr;
            case POIType.Campsite: return campsiteStr;
            case POIType.BicycleShop: return bicycleshopStr;
            case POIType.Supermarket: return supermarketStr;
            case POIType.BicycleRepairStation: return bicyclerepairstationStr;
        }
        return string.Empty;
    }

    private async void RefreshButton_Clicked(object sender, EventArgs e)
    {
        Platforms.KeyboardHelper.HideKeyboard();
        if (pois.Count > 0)
            await PopulateMapAsync(pois);
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