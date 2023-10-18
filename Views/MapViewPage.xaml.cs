using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Nts.Extensions;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using Mapsui.Widgets;
using Mapsui.Widgets.ScaleBar;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using POIBinaryFormatLib;
using POIViewerMap.Helpers;
using POIViewerMap.Resources.Strings;
using ReactiveUI;
using System.Reactive.Linq;
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
    static string atmStr = null;
    static string toiletStr = null;
    static string cupStr = null;
    static string bakeryStr = null;
    static string picnictableStr = null;
    static bool POIsReadIsBusy = false;
    static bool POIsMapUpdateIsBusy = false;
    static int MaxRadius = 5; // km
    static readonly int MinZoomPOI = 290;
    static POIType currentPOIType = POIType.DrinkingWater;
    private List<POIData> pois = new();
    private static Location myCurrentLocation;
    private object minX;
    private object minY;
    private CompassData CurrentCompassReading;
    private static bool IsCompassUpDateBusy = false; 
    private MyLocationLayer? _myLocationLayer;
    private bool _disposed;
    private (MPoint, double, double, double, bool, bool, bool)[] _points = new (MPoint, double, double, double, bool, bool, bool)[0];
    private int _count = 0;
    
    public MapViewPage()
	{
		InitializeComponent();

        var items = new List<string>();
        items.Add(AppResource.OptionsPOIPickerDrinkingWaterText);
        items.Add(AppResource.OptionsPOIPickerCampsiteText);
        items.Add(AppResource.OptionsPOIPickerBicycleShopText);
        items.Add(AppResource.OptionsPOIPickerBicycleRepairStationText);
        items.Add(AppResource.OptionsPOIPickerSupermarketText);
        items.Add(AppResource.OptionsPOIPickerATMText);
        items.Add(AppResource.OptionsPOIPickerToiletText);
        items.Add(AppResource.OptionsPOIPickerCafeText);
        items.Add(AppResource.OptionsPOIPickerBakeryText);
        items.Add(AppResource.OptionsPOIPickerPicnicTableText);
        this.picker.ItemsSource = items;
        Mapsui.Logging.Logger.LogDelegate += (level, message, ex) =>
        {
        };// todo: Write to your own logger;
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
        using var atm = assembly.GetManifestResourceStream($"{assemblyName}.Resources.Images.atm.svg");
        if (atm != null)
        {
            using StreamReader reader = new(atm!);
            atmStr = reader.ReadToEnd();
        }
        using var toilet = assembly.GetManifestResourceStream($"{assemblyName}.Resources.Images.toilet.svg");
        if (toilet != null)
        {
            using StreamReader reader = new(toilet!);
            toiletStr = reader.ReadToEnd();
        }
        using var cup = assembly.GetManifestResourceStream($"{assemblyName}.Resources.Images.coffee-cup.svg");
        if (cup != null)
        {
            using StreamReader reader = new(cup!);
            cupStr = reader.ReadToEnd();
        }
        using var bakery = assembly.GetManifestResourceStream($"{assemblyName}.Resources.Images.cupcake.svg");
        if (bakery != null)
        {
            using StreamReader reader = new(bakery!);
            bakeryStr = reader.ReadToEnd();
        }
        using var picnictable = assembly.GetManifestResourceStream($"{assemblyName}.Resources.Images.picnic-table.svg");
        if (picnictable != null)
        {
            using StreamReader reader = new(picnictable!);
            picnictableStr = reader.ReadToEnd();
        }
        _myLocationLayer?.Dispose();
        _myLocationLayer = new MyLocationLayer(mapView.Map)
        {
            IsCentered = true,
        };

        mapView.Map.Layers.Add(OpenStreetMap.CreateTileLayer());
        mapView.Map.Layers.Add(_myLocationLayer);

        // Get the lon lat coordinates from somewhere (Mapsui can not help you there)
        var center = new MPoint(-2.218266, 51.745564);
        // OSM uses spherical mercator coordinates. So transform the lon lat coordinates to spherical mercator
        var sphericalMercatorCoordinate = SphericalMercator.FromLonLat(center.X, center.Y).ToMPoint();
        // Set the center of the viewport to the coordinate. The UI will refresh automatically
        // Additionally you might want to set the resolution, this could depend on your specific purpose
        mapView.Map.Home = n => n.CenterOnAndZoomTo(sphericalMercatorCoordinate, n.Resolutions[9]);
        
        mapView.Map.Navigator.RotationLock = true;
        
        mapView.IsZoomButtonVisible = true;
        mapView.IsMyLocationButtonVisible = true;
        mapView.IsNorthingButtonVisible = false;
        mapView.Map.Navigator.OverrideZoomBounds = new MMinMax(0.15, 1600);
        mapView.Map.Widgets.Add(new ScaleBarWidget(mapView.Map) { TextAlignment = Alignment.Center });
        mapView.PinClicked += OnPinClicked;
        mapView.MapClicked += OnMapClicked;
        ToggleCompass();
        
        // From GPS - not windows TODO iOS
        if (DeviceInfo.Current.Platform == DevicePlatform.Android)
            GetCurrentDeviceLocation();
        _ = Observable
                .Interval(TimeSpan.FromSeconds(10))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async _ =>
                {
                    if (!this.AllowCenterMap.IsChecked)
                    {
                        mapView.MyLocationLayer.Enabled = true;
                        _myLocationLayer.Enabled = false;
                        return;
                    }
                    if (DeviceInfo.Current.Platform == DevicePlatform.Android)
                        await GetCurrentDeviceLocationAsync();
                    mapView.MyLocationLayer.Enabled = false;
                    _myLocationLayer.Enabled = true;
                });
    }
    public virtual void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _myLocationLayer?.Dispose();
    }

    protected virtual void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
    }
    private async Task GetCurrentDeviceLocationAsync()
    {
        await Task.Factory.StartNew(async () =>
        {
            var request = new GeolocationRequest(GeolocationAccuracy.Best);
            myCurrentLocation = await Geolocation.GetLocationAsync(request, new CancellationToken());

            var sphericalMercatorCoordinate = SphericalMercator.FromLonLat(myCurrentLocation.Longitude, myCurrentLocation.Latitude).ToMPoint();
            //mapView.Map.Home = n => n.CenterOnAndZoomTo(sphericalMercatorCoordinate, n.Resolutions[14]);
            _myLocationLayer.UpdateMyLocation(sphericalMercatorCoordinate, true);
            mapView.MyLocationLayer.UpdateMyLocation(new Mapsui.UI.Maui.Position(myCurrentLocation.Latitude, myCurrentLocation.Longitude));
            _myLocationLayer.UpdateMyDirection(CurrentCompassReading.HeadingMagneticNorth, mapView?.Map.Navigator.Viewport.Rotation ?? 0);
            _myLocationLayer.UpdateMyViewDirection(CurrentCompassReading.HeadingMagneticNorth, mapView?.Map.Navigator.Viewport.Rotation ?? 0);
        });
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
        catch (Exception) { }
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
    async void OnPickerSelectedIndexChanged(object sender, EventArgs e)
    {
        if(POIsReadIsBusy)
        { return; }
        var picker = (Picker)sender;
        int selectedIndex = picker.SelectedIndex;

        if (selectedIndex != -1)
        {
            currentPOIType = MapViewPage.GetPOIType(selectedIndex);
            if (pois.Count > 0)
            {
                foreach (var pin in mapView.Pins)
                {
                    pin.HideCallout();
                }
                mapView.Pins.Clear();
                
                if (mapView.Map.Navigator.Viewport.Resolution >= MinZoomPOI)
                {
                    if (myCurrentLocation != null)
                    {
                        var sphericalMercatorCoordinate = SphericalMercator.FromLonLat(myCurrentLocation.Longitude, myCurrentLocation.Latitude).ToMPoint();
                        mapView.Map.Navigator.CenterOnAndZoomTo(sphericalMercatorCoordinate, mapView.Map.Navigator.Resolutions[12], -1, Mapsui.Animations.Easing.CubicOut);
                    }
                    else
                    {
                        var center = new MPoint(-2.218266, 51.745564);
                        // OSM uses spherical mercator coordinates. So transform the lon lat coordinates to spherical mercator
                        var sphericalMercatorCoordinate = SphericalMercator.FromLonLat(center.X, center.Y).ToMPoint();
                        mapView.Map.Navigator.CenterOnAndZoomTo(sphericalMercatorCoordinate, mapView.Map.Navigator.Resolutions[12], -1, Mapsui.Animations.Easing.CubicOut);
                    }
                }
                this.Loading.IsVisible = true;
                this.picker.IsEnabled = false;
                this.pickerRadius.IsEnabled = false;
                await PopulateMapAsync(pois);
                this.picker.IsEnabled = true;
                this.pickerRadius.IsEnabled = true;
                this.Loading.IsVisible = false;
                this.picker.Title = picker.Items[selectedIndex];
            }
        }
    }
    async void OnRadiusPickerSelectedIndexChanged(object sender, EventArgs e)
    {
        if (POIsReadIsBusy)
        { return; }
        var picker = (Picker)sender;
        int selectedIndex = picker.SelectedIndex;

        if (selectedIndex != -1)
        {
            MaxRadius = GetRadiusType(selectedIndex);
            this.pickerRadius.Title = $"{MaxRadius}km";
            if (pois.Count > 0)
            {
                foreach (var pin in mapView.Pins)
                {
                    pin.HideCallout();
                }
                mapView.Pins.Clear();
                if (mapView.Map.Navigator.Viewport.Resolution >= MinZoomPOI)
                {
                    if (myCurrentLocation != null)
                    {
                        var sphericalMercatorCoordinate = SphericalMercator.FromLonLat(myCurrentLocation.Longitude, myCurrentLocation.Latitude).ToMPoint();
                        mapView.Map.Navigator.CenterOnAndZoomTo(sphericalMercatorCoordinate, mapView.Map.Navigator.Resolutions[12], -1, Mapsui.Animations.Easing.CubicOut);
                    }
                    else
                    {
                        var center = new MPoint(-2.218266, 51.745564);
                        // OSM uses spherical mercator coordinates. So transform the lon lat coordinates to spherical mercator
                        var sphericalMercatorCoordinate = SphericalMercator.FromLonLat(center.X, center.Y).ToMPoint();
                        mapView.Map.Navigator.CenterOnAndZoomTo(sphericalMercatorCoordinate, mapView.Map.Navigator.Resolutions[12], -1, Mapsui.Animations.Easing.CubicOut);
                    }
                }
                this.Loading.IsVisible = true;
                this.picker.IsEnabled = false;
                this.pickerRadius.IsEnabled = false;
                await PopulateMapAsync(pois);
                this.picker.IsEnabled = true;
                this.pickerRadius.IsEnabled = true;
                this.Loading.IsVisible = false;
            }
        }
    }
    static int GetRadiusType(int selectedIndex)
    {
        switch(selectedIndex)
        {
            case 0: return 5; // Km
            case 1: return 10; 
            case 2: return 20;
            case 3: return 50;
            case 4: return 75;
            case 5: return 100; 
            default: return 5;
        }
    }
    async void BrowseButton_Clicked(object sender, EventArgs e)
    {
        if (POIsReadIsBusy)
            return;
        POIsReadIsBusy = true;
        pois.Clear();
        foreach (var pin in mapView.Pins)
        {
            pin.HideCallout();
        }
        mapView.Pins.Clear();
        await BrowsePOIs();
        if (!String.IsNullOrEmpty(this.FilepathPOILabel.Text))
        {
            this.pickerRadius.Title = $"{MaxRadius}km";
            this.picker.Title = AppResource.OptionsPOIPickerDrinkingWaterText;// "Drinking Water";
            currentPOIType = POIType.DrinkingWater;
            this.Loading.IsVisible = true;
            this.picker.IsEnabled = false;
            this.pickerRadius.IsEnabled = false;
            pois = await POIBinaryFormat.ReadAsync(FullFilepathPOIs);
            if (mapView.Map.Navigator.Viewport.Resolution < MinZoomPOI)
                await PopulateMapAsync(pois);
            else
            {
                if(myCurrentLocation != null)
                {
                    var sphericalMercatorCoordinate = SphericalMercator.FromLonLat(myCurrentLocation.Longitude, myCurrentLocation.Latitude).ToMPoint();
                    mapView.Map.Navigator.CenterOnAndZoomTo(sphericalMercatorCoordinate, mapView.Map.Navigator.Resolutions[12], -1, Mapsui.Animations.Easing.CubicOut);
                }
                else
                {
                    var center = new MPoint(-2.218266, 51.745564);
                    // OSM uses spherical mercator coordinates. So transform the lon lat coordinates to spherical mercator
                    var sphericalMercatorCoordinate = SphericalMercator.FromLonLat(center.X, center.Y).ToMPoint();
                    mapView.Map.Navigator.CenterOnAndZoomTo(sphericalMercatorCoordinate, mapView.Map.Navigator.Resolutions[12], -1, Mapsui.Animations.Easing.CubicOut);
                }
                await PopulateMapAsync(pois);
            }
            this.picker.IsEnabled = true;
            this.pickerRadius.IsEnabled = true;
            this.Loading.IsVisible = false;
            this.picker.SelectedIndex = -1;
            this.pickerRadius.SelectedIndex = -1;
            POIsReadIsBusy = false;
        }
    }
    private static void ShowZoomInToast()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            // Code to run on the main thread
            CancellationTokenSource cancellationTokenSource = new();
            ToastDuration duration = ToastDuration.Short;
            double fontSize = 15;
            var toast = Toast.Make(AppResource.ZoomInToastMsg, duration, fontSize);
            await toast.Show(cancellationTokenSource.Token);
        });
    }
    private static void ShowDistanceToGreatToast()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            // Code to run on the main thread
            CancellationTokenSource cancellationTokenSource = new();
            ToastDuration duration = ToastDuration.Short;
            double fontSize = 15;
            var toast = Toast.Make($"{AppResource.DistanceTooGreatToastMsg} {MaxRadius}km", duration, fontSize);
            await toast.Show(cancellationTokenSource.Token);
        });
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
    private void ToggleCompass()
    {
        if (Compass.Default.IsSupported)
        {
            if (!Compass.Default.IsMonitoring)
            {
                // Turn on compass
                Compass.Default.ReadingChanged += Compass_ReadingChanged;
                Compass.Default.Start(SensorSpeed.UI);
            }
            else
            {
                // Turn off compass
                Compass.Default.Stop();
                Compass.Default.ReadingChanged -= Compass_ReadingChanged;
            }
        }
    }
    private void Compass_ReadingChanged(object sender, CompassChangedEventArgs e)
    {
        CurrentCompassReading = e.Reading;
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
    private async Task BrowsePOIs()
    {
        var customFileType = new FilePickerFileType(
             new Dictionary<DevicePlatform, IEnumerable<string>>
             {
                    { DevicePlatform.iOS, new[] { "public.my.bin.extension" } }, // UTType values
                    { DevicePlatform.Android, new[] { "application/octet-stream" } }, // MIME type
                    { DevicePlatform.WinUI, new[] { ".bin" } }, // file extension
                    { DevicePlatform.Tizen, new[] { "*/*" } },
                    { DevicePlatform.macOS, new[] { "bin" } }, // UTType values
             });

        PickOptions options = new()
        {
            PickerTitle = "Please select a POI file",
            FileTypes = customFileType,
        };
        var result = await MapViewPage.PickAndShow(options);//, "route");
        if (Path.GetExtension(result.FileName).Equals(".bin"))
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
        var result = await MapViewPage.PickAndShow(options);
        if (Path.GetExtension(result.FileName).Equals(".gpx"))
        {
            this.FullFilepathRoute = result?.FullPath;
            this.FilepathRouteLabel.Text = result?.FileName;
        }
    }
    public static async Task<FileResult> PickAndShow(PickOptions options)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(options);
            return result;
        }
        catch (Exception)
        {
            // The user canceled or something went wrong
        }
        return null;
    }
    private async Task GetCurrentDeviceLocation()
    {
        var request = new GeolocationRequest(GeolocationAccuracy.Best);
        myCurrentLocation = await Geolocation.GetLocationAsync(request, new CancellationToken());
        if (myCurrentLocation != null)
        {
            mapView.MyLocationLayer.UpdateMyLocation(new Mapsui.UI.Maui.Position(myCurrentLocation.Latitude, myCurrentLocation.Longitude));
        }
    }
    private static MRect GetLimitsOfStroud()
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
                if (mapView.Map.Navigator.Viewport.Resolution > MinZoomPOI)
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
                    if (distance > MaxRadius)
                      continue;
                    if (poi.POI != currentPOIType)
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
                        Label = $"{GetTitleLang(poi, poi.Title.Contains(':'))}\r{GetSubTitleLang(poi)}{space}{AppResource.DistanceMessageText}: {String.Format("{0:0.00}", distance)}km",
                        Address = "",
                        Svg = MapViewPage.GetPOIIcon(poi),// eg. drinkingwaterStr,
                        Scale = 0.0462F
                    };
                    myPin.Callout.TitleTextAlignment = TextAlignment.Start;
                    myPin.Callout.ArrowHeight = 15;
                    myPin.Callout.TitleFontSize = 15;
                    mapView.Pins.Add(myPin);
                }
                POIsReadIsBusy = false;
            }
            catch(Exception ex)
            {
            }
            finally { POIsReadIsBusy = false; }
        });
    }
    private string GetSubTitleLang(POIData poi)
    {
        var subtitle = string.Empty;
        if(poi.Subtitle.Contains("Open:"))
        {
            subtitle = $"{AppResource.PinLabelSubtitleOpen} {poi.Subtitle.Substring(poi.Subtitle.IndexOf(":") + 2)}";
        }
        if (poi.Subtitle.Contains("Website:"))
        {
            subtitle = $"{AppResource.PinLabelSubtitleWebsite} {poi.Subtitle.Substring(poi.Subtitle.IndexOf(":") + 2)}";
        }
        if (poi.Subtitle.Contains("Services:"))
        {
            subtitle = $"{AppResource.PinLabelSubtitleServices} {poi.Subtitle.Substring(poi.Subtitle.IndexOf(":") + 2)}";
        }
        if (poi.Subtitle.Contains("Refill Here"))
        {
            subtitle = $"{AppResource.PinLabelSubtitleRefill}";
        }
        return subtitle;
    }
    private static string GetTitleLang(POIData data, bool v)
    {
        var Title = string.Empty;
        switch(data.POI)
        {
            case POIType.DrinkingWater: 
                Title = AppResource.OptionsPOIPickerDrinkingWaterText;
                break;
            case POIType.Campsite:
                Title = AppResource.OptionsPOIPickerCampsiteText;
                break;
            case POIType.BicycleShop:
                Title = AppResource.OptionsPOIPickerBicycleShopText;
                break;
            case POIType.BicycleRepairStation:
                Title =  AppResource.OptionsPOIPickerBicycleRepairStationText;
                break;
            case POIType.Supermarket: Title =  AppResource.OptionsPOIPickerSupermarketText;
                break; ;
            case POIType.ATM: Title =  AppResource.OptionsPOIPickerATMText;
                break;
            case POIType.Toilet: Title =  AppResource.OptionsPOIPickerToiletText;
                break;
            case POIType.Cafe: Title =  AppResource.OptionsPOIPickerCafeText;
                break;
            case POIType.Bakery: Title =  AppResource.OptionsPOIPickerBakeryText;
                break;
            case POIType.PicnicTable: Title =  AppResource.OptionsPOIPickerPicnicTableText;
                break;
            default: Title =  string.Empty;
                break;
        }
        return Title += v ? data.Title[data.Title.IndexOf(":")..] : string.Empty;
    }

    private static string GetPOIIcon(POIData poi)
    {
        switch (poi.POI)
        {
            case POIType.DrinkingWater: return drinkingwaterStr;
            case POIType.Campsite: return campsiteStr;
            case POIType.BicycleShop: return bicycleshopStr;
            case POIType.BicycleRepairStation: return bicyclerepairstationStr;
            case POIType.Supermarket: return supermarketStr;
            case POIType.ATM: return atmStr;
            case POIType.Toilet: return toiletStr;
            case POIType.Cafe: return cupStr;
            case POIType.Bakery: return bakeryStr;
            case POIType.PicnicTable: return picnictableStr;
            case POIType.Unknown:
                break;
        }
        return string.Empty;
    }
    private static POIType GetPOIType(int selectedIndex)
    {
        switch (selectedIndex)
        {
            case 0: return POIType.DrinkingWater;
            case 1: return POIType.Campsite;
            case 2: return POIType.BicycleShop;
            case 3: return POIType.BicycleRepairStation;
            case 4: return POIType.Supermarket;
            case 5: return POIType.ATM;
            case 6: return POIType.Toilet;
            case 7: return POIType.Cafe;
            case 8: return POIType.Bakery;
            case 9: return POIType.PicnicTable;
            default:
                break;
        }
        return POIType.Unknown;
    }
    private async void  RefreshButton_Clicked(object sender, EventArgs e)
    {
        Platforms.KeyboardHelper.HideKeyboard();
        if (pois.Count > 0)
            await PopulateMapAsync(pois);
    }
}