using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Nts.Extensions;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using POIViewerMap.DataClasses;
using POIViewerMap.Helpers;
using POIViewerMap.Resources.Strings;
using System.Reflection;
using Location = Microsoft.Maui.Devices.Sensors.Location;

namespace POIViewerMap.Views;

public partial class MapViewPage : ContentPage
{
    private string FullFilepathPOIs;
    private string FullFilepathRoute;
    static string drinkingwaterStr = null;
    static string campsiteStr = null;
    static string bicycleshopStr = null;
    static string supermarketStr = null;
    static string bicyclerepairstationStr = null;
    static bool POIsReadIsBusy = false;
    static bool POIsMapUpdateIsBusy = false;
    static int MaxDistancePOIShow = 10; //Meters
    static int MinZoomPOI = 40;
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
        //mapView.Map = mapView.Map;
        //mapView.PanLock = true;
        mapView.IsZoomButtonVisible = true;
        mapView.IsMyLocationButtonVisible = true;
        mapView.IsNorthingButtonVisible = true;
        var mapControl = new Mapsui.UI.Maui.MapControl();
        mapView.PinClicked += OnPinClicked;
        mapView.MapClicked += OnMapClicked;
        mapView.Viewport.ViewportChanged += Viewport_ViewportChanged;
        mapControl.Navigator.CenterOn(SphericalMercator.FromLonLat(-2.2539759, 51.7476017).ToMPoint());
        // From GPS - not windows TODO iOS
        if (DeviceInfo.Current.Platform == DevicePlatform.Android)
            GetCurrentDeviceLocation();
    }
    private async void Viewport_ViewportChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (POIsMapUpdateIsBusy || e.PropertyName.Equals("SetSize"))
            return;
        var res = mapView.Viewport.Resolution;
        if (mapView.Viewport.Resolution < MinZoomPOI)
        {
            pois.Clear();
            //mapView.Pins.Clear();
            if (!String.IsNullOrEmpty(FullFilepathPOIs))
            {
                POIsMapUpdateIsBusy = true;
                pois = await ReadPOIs.ReadAysnc(FullFilepathPOIs);
                await PopulateMapAsync(pois);
                POIsMapUpdateIsBusy = false;
            }
        }
        else
        {
            mapView.Pins.Clear();
        }
    }
    private void OnMapClicked(object sender, MapClickedEventArgs e)
    {
        if (POIsReadIsBusy)
            return;
        try
        {
            foreach (var pin in mapView.Pins)
            {
                pin.HideCallout();
            }
            this.expander.IsExpanded = false;
        }
        catch(Exception ex) { }
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
        if (POIsReadIsBusy)
            return;
        pois.Clear();
        mapView.Pins.Clear();
        await BrowsePOIs();
        if(!String.IsNullOrEmpty(this.FilepathPOILabel.Text))
        {
            this.POITypeLabel.Text = AppResource.POIsLoadingMsg;
            pois = await ReadPOIs.ReadAysnc(FullFilepathPOIs);
            if (mapView.Viewport.Resolution < MinZoomPOI)
                await PopulateMapAsync(pois);
            this.POITypeLabel.Text = GetPOIString(pois.Count > 0 ? pois[0].POI : POIType.Unknown);
        }
    }
    async void BrowseRoutesButton_Clicked(object sender, EventArgs e)
    {
        if (POIsReadIsBusy)
            return;
        await BrowseRoutes();
        if(!String.IsNullOrEmpty(this.FilepathRouteLabel.Text))
        {
            var line = await ImportRoutes.ImportGPXRouteAsync(this.FullFilepathRoute);
            try
            {
                var lineStringLayer = CreateLineStringLayer(line, CreateLineStringStyle());
                mapView.Map.Layers.Add(lineStringLayer);
            }
            catch { } // TODO
        }
    }
    public static ILayer CreateLineStringLayer(string line, IStyle? style = null)
    {
        var lineString = (LineString)new WKTReader().Read(line);
        lineString = new LineString(lineString.Coordinates.Select(v => SphericalMercator.FromLonLat(v.Y, v.X).ToCoordinate()).ToArray());

        return new MemoryLayer
        {
            Features = new[] { new GeometryFeature { Geometry = lineString } },
            Name = "LineStringLayer",
            Style = style

        };
    }
    public static IStyle CreateLineStringStyle()
    {
        return new VectorStyle
        {
            Fill = null,
            Outline = null,
#pragma warning disable CS8670 // Object or collection initializer implicitly dereferences possibly null member.
            Line = { Color = Mapsui.Styles.Color.FromString("Red"), Width = 4 }
        };
    }
    async void OnStepperValueChanged(object sender, ValueChangedEventArgs e)
    {
        if (POIsReadIsBusy)
            return;
        if (String.IsNullOrEmpty(this.FullFilepathPOIs))
            return;
        this.MaxDistanceLabel.Text = e.NewValue.ToString();
        pois.Clear();
        this.POITypeLabel.Text = AppResource.POIsLoadingMsg;
        pois = await ReadPOIs.ReadAysnc(FullFilepathPOIs);
        await PopulateMapAsync(pois);
        this.POITypeLabel.Text = GetPOIString(pois.Count > 0 ? pois[0].POI : POIType.Unknown);
    }
    private async Task BrowsePOIs()
    {
        var customFileType = new FilePickerFileType(
             new Dictionary<DevicePlatform, IEnumerable<string>>
             {
                    { DevicePlatform.iOS, new[] { "public.my.poi.extension" } }, // UTType values
                    { DevicePlatform.Android, new[] { "application/octet-stream" } }, // MIME type
                    { DevicePlatform.WinUI, new[] { ".poi" } }, // file extension
                    { DevicePlatform.Tizen, new[] { "*/*" } },
                    { DevicePlatform.macOS, new[] { "poi" } }, // UTType values
             });

        PickOptions options = new()
        {
            PickerTitle = "Please select a POI file",
            FileTypes = customFileType,
        };
        var result = await PickAndShow(options);//, "route");
        if (Path.GetExtension(result.FileName).Equals(".poi"))
        {
            this.FullFilepathPOIs = result?.FullPath;
            this.FilepathPOILabel.Text = result?.FileName;
        }
    }
    private async Task BrowseRoutes()
    {
        var customFileType = new FilePickerFileType(
             new Dictionary<DevicePlatform, IEnumerable<string>>
             {
                    { DevicePlatform.iOS, new[] { "public.my.gpx.extension" } }, // UTType values
                    { DevicePlatform.Android, new[] { "application/octet-stream" } }, // MIME type
                    { DevicePlatform.WinUI, new[] { ".gpx" } }, // file extension
                    { DevicePlatform.Tizen, new[] { "*/*" } },
                    { DevicePlatform.macOS, new[] { "gpx" } }, // UTType values
             });

        PickOptions options = new()
        {
            PickerTitle = "Please select a GPX file",
            FileTypes = customFileType,
        };
        var result = await PickAndShow(options);
        if (Path.GetExtension(result.FileName).Equals(".gpx"))
        {
            this.FullFilepathRoute = result?.FullPath;
            this.FilepathRouteLabel.Text = result?.FileName;
        }
    }
    public async Task<FileResult> PickAndShow(PickOptions options)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(options);
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
        await Task.Factory.StartNew(() =>
        {
            try
            {
                POIsReadIsBusy = true;
                foreach (var pin in mapView.Pins)
                {
                    pin.HideCallout();
                }
                if(mapView.Viewport.Resolution > MinZoomPOI)
                {
                    mapView.Pins.Clear();
                    return;
                }
                var myLocation = new Location(mapView.MyLocationLayer.MyLocation.Latitude, mapView.MyLocationLayer.MyLocation.Longitude);
                foreach (var poi in pois)
                {
                    var distance = Location.CalculateDistance(poi.Latitude,
                                                                poi.Longitude,
                                                                new Location(mapView.MyLocationLayer.MyLocation.Latitude, mapView.MyLocationLayer.MyLocation.Longitude),
                                                                DistanceUnits.Kilometers);
                    if (distance > MaxDistancePOIShow)
                      continue;
                    var space = string.Empty;
                    if (!String.IsNullOrEmpty(poi.Subtitle))
                    {
                        space = "\r";
                    }
                    var myPin = new Pin(mapView)
                    {
                        Position = new Mapsui.UI.Maui.Position(poi.Latitude, poi.Longitude),
                        Type = PinType.Svg,
                        Label = $"{poi.Title}\r{poi.Subtitle}{space}Distance: {String.Format("{0:0.00}", distance)}km",
                        Address = "",
                        Svg = GetPOIIcon(poi),// eg. drinkingwaterStr,
                        Scale = 0.0362F
                    };

                    //myPin.HideCallout();
                    myPin.Callout.TitleTextAlignment = TextAlignment.Start;
                    myPin.Callout.ArrowHeight = 15;
                    myPin.Callout.TitleFontSize = 15;
                    mapView.Pins.Add(myPin);
                }
                POIsReadIsBusy = false;
            }
            catch(Exception ex) {  }
            finally { POIsReadIsBusy = false; }
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
    private async void  RefreshButton_Clicked(object sender, EventArgs e)
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