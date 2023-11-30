using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
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
using POIViewerMap.Popups;
using POIViewerMap.Resources.Strings;
using POIViewerMap.Stores;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using Color = Microsoft.Maui.Graphics.Color;
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
    static int SearchRadius = 5; // km
    static int MaxDistanceRefresh = 2; //km
    static readonly int MinZoomPOI = 290;
    static POIType CurrentPOIType = POIType.DrinkingWater;
    private List<POIData> pois = new();
    private static Location myCurrentLocation;
    private static Location CurrentLocationOnLoad = null;
    private CompassData CurrentCompassReading;
    private static bool IsCompassUpDateBusy = false; 
    private MyLocationLayer? _myLocationLayer;
    private bool _disposed;
    public static bool IsAppStateSettingsBusy = false;
    public static bool IsSearchRadiusCircleBusy = false;
    public static Popup popup;
    public static ILayer myRouteLayer;
    public IAppSettings appSettings;
    CompositeDisposable? deactivateWith;
    protected CompositeDisposable DeactivateWith => this.deactivateWith ??= new CompositeDisposable();
    protected CompositeDisposable DestroyWith { get; } = new CompositeDisposable();

    public MapViewPage(IAppSettings appSettings)
	{
		InitializeComponent();
        this.appSettings = appSettings;
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
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            GetCurrentDeviceLocation();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        _ = Observable
                .Interval(TimeSpan.FromSeconds(10))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async _ =>
                {
                    await UpdateSearchRadiusCircleOnMap(mapView, SearchRadius);
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
                    await CheckLoadingDistance();
                });
    }
    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        if (!appSettings.ShowPopupAtStart) return;
        AppUsagePopup popup = new ();
        popup.ShowPopupAtStartup = appSettings.ShowPopupAtStart;
        popup.Closed += Popup_Closed;
        this.ShowPopup(popup);
    }
    private void Popup_Closed(object sender, PopupClosedEventArgs e)
    {
        appSettings.ShowPopupAtStart = (bool)e.Result;
    }
    private async Task GetCurrentLocation()
    {
        var request = new GeolocationRequest(GeolocationAccuracy.Best);
        var Location = await Geolocation.GetLocationAsync(request, new CancellationToken());
        mapView.MyLocationLayer.UpdateMyLocation(new Mapsui.UI.Maui.Position(Location.Latitude, Location.Longitude));
    }
    // Have we moved since last poi load/search radius by more than 2km
    // if so update all Pins on map but only if allow center map is checked
    private async Task CheckLoadingDistance()
    {
        if(CurrentLocationOnLoad == null) return;
        var distance = Location.CalculateDistance(CurrentLocationOnLoad.Latitude,
                                                 CurrentLocationOnLoad.Longitude,
                                                 new Location(mapView.MyLocationLayer.MyLocation.Latitude, mapView.MyLocationLayer.MyLocation.Longitude),
                                                 DistanceUnits.Kilometers);
        if(distance > MaxDistanceRefresh)
        {
            this.Loading.IsVisible = true;
            this.picker.IsEnabled = false;
            this.pickerRadius.IsEnabled = false;
            this.activityloadindicatorlayout.IsVisible = true;
            await PopulateMapAsync(pois);
            this.picker.IsEnabled = true;
            this.pickerRadius.IsEnabled = true;
            this.Loading.IsVisible = false;
            this.activityloadindicatorlayout.IsVisible = false;
            CurrentLocationOnLoad = myCurrentLocation;
        }        
    }
    private async Task UpdateVisiblePinsLabelDistanceText()
    {
        await Task.Factory.StartNew(async () =>
        {
            foreach (var pin in mapView.Pins)
            {
                var distance = Location.CalculateDistance(pin.Position.Latitude,
                                                                pin.Position.Longitude,
                                                                new Location(mapView.MyLocationLayer.MyLocation.Latitude, mapView.MyLocationLayer.MyLocation.Longitude),
                                                                DistanceUnits.Kilometers);
                if (pin != null && pin.IsVisible)
                {
                    var Idx = pin.Label.IndexOf(AppResource.PinLabelDistanceText);
                    if (Idx > -1)
                    {
                        if (Idx + AppResource.PinLabelDistanceText.Length < pin.Label.Length)
                        {
                            // Remove previous distance value as we may have moved on the map so recalculate
                            pin.Label = pin.Label[..(Idx + AppResource.PinLabelDistanceText.Length)];
                            pin.Label += FormatHelper.FormatDistance(distance);
                        }
                        else
                        {
                            pin.Label += FormatHelper.FormatDistance(distance);
                        }
                    }
                }

            }
        });
    }
    private static Task UpdateSearchRadiusCircleOnMap(MapView mapView, int radius)
    {
        if (IsSearchRadiusCircleBusy)
        { return Task.CompletedTask; }
        try
        {
            
            var loc = myCurrentLocation ?? new Location(51.745564, -2.218266);
            var circle = new Circle
            {
                Center = new Mapsui.UI.Maui.Position(loc.Latitude, loc.Longitude),
                Radius = Distance.FromKilometers(radius),
                Quality = 100,
                StrokeColor = new Color(255, 153, 0),
                StrokeWidth = 2,
                FillColor = new Color(255, 153, 0, 0.0541f),
            };
            IsSearchRadiusCircleBusy = true;
            mapView.Drawables.Clear();
            mapView.Drawables.Add(circle);
            IsSearchRadiusCircleBusy = false;
        }
        catch (Exception ex) { }
        return Task.CompletedTask;
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
        if (POIsMapUpdateIsBusy) return;
        await Task.Factory.StartNew(async () =>
        {
            POIsMapUpdateIsBusy = true;
            var request = new GeolocationRequest(GeolocationAccuracy.Best);
            myCurrentLocation = await Geolocation.GetLocationAsync(request, new CancellationToken());

            var sphericalMercatorCoordinate = SphericalMercator.FromLonLat(myCurrentLocation.Longitude, myCurrentLocation.Latitude).ToMPoint();
            //mapView.Map.Home = n => n.CenterOnAndZoomTo(sphericalMercatorCoordinate, n.Resolutions[14]);
            _myLocationLayer.UpdateMyLocation(sphericalMercatorCoordinate, true);
            mapView.MyLocationLayer.UpdateMyLocation(new Mapsui.UI.Maui.Position(myCurrentLocation.Latitude, myCurrentLocation.Longitude));
            _myLocationLayer.UpdateMyDirection(CurrentCompassReading.HeadingMagneticNorth, mapView?.Map.Navigator.Viewport.Rotation ?? 0);
            _myLocationLayer.UpdateMyViewDirection(CurrentCompassReading.HeadingMagneticNorth, mapView?.Map.Navigator.Viewport.Rotation ?? 0);
            await UpdateVisiblePinsLabelDistanceText();
            POIsMapUpdateIsBusy = false;
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
            {
                if (e.Pin.Callout.IsVisible)
                    e.Pin.HideCallout();
                else
                {
                    var distance = Location.CalculateDistance(e.Point.Latitude,
                                                                e.Point.Longitude,
                                                                new Location(mapView.MyLocationLayer.MyLocation.Latitude, mapView.MyLocationLayer.MyLocation.Longitude),
                                                                DistanceUnits.Kilometers);
                    var Idx = e.Pin.Label.IndexOf(AppResource.PinLabelDistanceText);
                    if(Idx > -1)
                    {
                        if (Idx + AppResource.PinLabelDistanceText.Length < e.Pin.Label.Length)
                        {
                            // Remove previous distance value as we may have moved on the map so recalculate
                            e.Pin.Label = e.Pin.Label.Substring(0, Idx + AppResource.PinLabelDistanceText.Length);
                            //e.Pin.Label += Format.FormatDistance(distance);
                        }
                        e.Pin.Label += FormatHelper.FormatDistance(distance);
                    }
                    e.Pin.ShowCallout();
                }
            }
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
            CurrentPOIType = FormatHelper.GetPOIType(selectedIndex);
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
                this.activityloadindicatorlayout.IsVisible = true;
                await PopulateMapAsync(pois);
                this.activityloadindicatorlayout.IsVisible = false;
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
            SearchRadius = FormatHelper.GetRadiusType(selectedIndex);
            this.pickerRadius.Title = $"{SearchRadius}km";
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
                await UpdateSearchRadiusCircleOnMap(mapView, SearchRadius);
                this.Loading.IsVisible = true;
                this.picker.IsEnabled = false;
                this.pickerRadius.IsEnabled = false;
                this.activityloadindicatorlayout.IsVisible = true;
                await PopulateMapAsync(pois);
                this.activityloadindicatorlayout.IsVisible = false;
                this.picker.IsEnabled = true;
                this.pickerRadius.IsEnabled = true;
                this.Loading.IsVisible = false;
            }
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
            this.POIFilename.IsVisible = true;
            this.pickerRadius.Title = $"{SearchRadius}km";
            this.pickerRadius.SelectedIndex = 0;
            this.picker.Title = AppResource.OptionsPOIPickerDrinkingWaterText;// "Drinking Water";
            CurrentPOIType = POIType.DrinkingWater;
            this.Loading.IsVisible = true;
            this.picker.IsEnabled = false;
            this.pickerRadius.IsEnabled = false;
            this.activityloadindicatorlayout.IsVisible = true;
            this.FilepathPOILabel.Text = Path.GetFileName(FullFilepathPOIs);
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
            this.activityloadindicatorlayout.IsVisible = false;
            this.picker.IsEnabled = true;
            this.pickerRadius.IsEnabled = true;
            this.Loading.IsVisible = false;
            this.picker.SelectedIndex = -1;
            this.pickerRadius.SelectedIndex = -1;
        }
        POIsReadIsBusy = false;
    }
    private static void ShowUpdateUIStateToast()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            // Code to run on the main thread
            CancellationTokenSource cancellationTokenSource = new();
            ToastDuration duration = ToastDuration.Long;
            double fontSize = 15;
            var toast = Toast.Make(AppResource.RestoreUIStateMsg, duration, fontSize);
            await toast.Show(cancellationTokenSource.Token);
        });
    }
    async void BrowseRoutesButton_Clicked(object sender, EventArgs e)
    {
        if (POIsReadIsBusy)
            return;
        try
        {
            await BrowseRoutes();
            if (!String.IsNullOrEmpty(this.FilepathRouteLabel.Text))
            {
                if (myRouteLayer != null)
                    mapView.Map.Layers.Remove(myRouteLayer);
                //this.OptionsRouteDeleteButton.IsVisible = true;
                this.RouteFilename.IsVisible = true;
                var line = await ImportRoutes.ImportGPXRouteAsync(this.FullFilepathRoute);
                myRouteLayer = CreateLineStringLayer(line, CreateLineStringStyle());
                mapView.Map.Layers.Add(myRouteLayer);
            }
        }
        catch(Exception ex) { }
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
                    if (distance > SearchRadius)
                      continue;
                    if (poi.POI != CurrentPOIType)
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
                        Label = $"{FormatHelper.GetTitleLang(poi, poi.Title.Contains(':'))}\r{FormatHelper.GetSubTitleLang(poi)}{space}{AppResource.PinLabelDistanceText}",
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
                CurrentLocationOnLoad = myCurrentLocation;
            }
            catch (Exception ex)
            {
            }
            finally
            { 
                POIsReadIsBusy = false;
                CurrentLocationOnLoad = myCurrentLocation;
            }
        });
        await UpdateSearchRadiusCircleOnMap(mapView, SearchRadius);
    }
    private async void  RefreshButton_Clicked(object sender, EventArgs e)
    {
        Platforms.KeyboardHelper.HideKeyboard();
        if (pois.Count > 0)
            await PopulateMapAsync(pois);
    }
    private void AllowCenterMap_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        
    }
    private void ShowSearchRadiusOnMap_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        
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
    private void DeleteButton_Clicked(object sender, EventArgs e)
    {
        //this.OptionsRouteDeleteButton.IsVisible = false;
        if(myRouteLayer!= null)
        {
            this.FilepathRouteLabel.Text = string.Empty;
            mapView.Map.Layers.Remove(myRouteLayer);
        }        
    }
}