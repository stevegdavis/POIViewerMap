using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Projections;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using POIViewerMap.DataClasses;
//using POIViewerMap.ViewModels;
using POIViewerMap.Helpers;
using ReactiveUI;
using System.Reactive.Linq;
using System.Reflection;

namespace POIViewerMap.Views;

public partial class MapViewPage : ContentPage
{
    private string Filepath;
    private string FullFilepath;
    private string POIName = $"{POIType.DrinkingWater}";
    static string drinkingwaterStr = null;
    static string campsiteStr = null;
    static string bicycleshopStr = null;
    static string supermarketStr = null;
    private bool IsPOIIndexChanged = false;
    private List<POIData> pois = new();
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
        using var campsite = assembly.GetManifestResourceStream($"{assemblyName}.Resources.Images.campsite.svg");
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
        mapControl.Navigator.CenterOn(SphericalMercator.FromLonLat(-2.2539759, 51.7476017).ToMPoint());
        // From GPS - not windows TODO iOS
        if (DeviceInfo.Current.Platform == DevicePlatform.Android)
            GetCurrentDeviceLocation();
        _ = Observable
                .Interval(TimeSpan.FromSeconds(1))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async _ =>
                {
                    if (!String.IsNullOrEmpty(this.FullFilepath))
                    {
                        pois = await ReadPOIs.Read(FullFilepath);
                        POIName = this.POITypePicker.Items[POITypePicker.SelectedIndex > -1 ? POITypePicker.SelectedIndex : 0].Replace(" ", string.Empty);
                        await PopulateMap(pois, POIName);
                        this.FullFilepath = string.Empty;
                    }
                    else if(IsPOIIndexChanged)
                    {
                        POIName = this.POITypePicker.Items[POITypePicker.SelectedIndex > -1 ? POITypePicker.SelectedIndex : 0].Replace(" ", string.Empty);
                        await PopulateMap(pois, POIName);
                        IsPOIIndexChanged = false;
                    }
                });
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
    async void OnPickerSelectedIndexChanged(object sender, EventArgs e)
    {
        var picker = (Picker)sender;
        int selectedIndex = picker.SelectedIndex;

        if (selectedIndex != -1)
        {
            POIName = picker.Items[selectedIndex].Replace(" ", string.Empty);
            IsPOIIndexChanged = true;
        }
    }
    async void BrowseButton_Clicked(object sender, EventArgs e)
    {
        pois.Clear();
        await BrowsePOIs();
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
    private async Task PopulateMap(List<POIData> pois, string type)
    {
        foreach (var pin in mapView.Pins)
        {
            pin.HideCallout();
        }
        mapView.Pins.Clear();
        foreach (var poi in pois)
        {
            if(poi.POI.ToString().Equals(type))
            {
                var myPin = new Pin(mapView)
                {
                    Position = new Position(poi.Latitude, poi.Longitude),
                    Type = PinType.Svg,
                    //Label = "Steve",// $"{AppResource.LiveMapViewDeviceLabelText} {device.Name}\n{AppResource.LiveMapViewCalloutTimeLabelText} {device.Date}",
                    Label = $"{poi.Title}\r{poi.Subtitle}",
                    Address = "",
                    Svg = GetPOIIcon(poi),// eg. drinkingwaterStr,
                    Scale = 0.0288F
                };
                //myPin.HideCallout();
                myPin.Callout.TitleTextAlignment = TextAlignment.Start;
                myPin.Callout.ArrowHeight = 15;
                myPin.Callout.TitleFontSize = 15;
                mapView.Pins.Add(myPin);

            }
        }
    }
    private string GetPOIIcon(POIData poi)
    {
        switch(poi.POI)
        {
            case POIType.DrinkingWater: return drinkingwaterStr;
            case POIType.Campsite: return campsiteStr;
            case POIType.BicycleShop: return bicycleshopStr;
                case POIType.Supermarket: return supermarketStr;
                default: return null;
        }
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