using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Flurl.Util;
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
using POIViewerMap.DataClasses;
using POIViewerMap.Helpers;
using POIViewerMap.Resources.Strings;
using POIViewerMap.Stores;
using ReactiveUI;
using ReverseGeocodeLib;
using ReverseGeocodeLib.Models;
using RolandK.Formats.Gpx;
using Syncfusion.Maui.Toolkit.Popup;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Windows.Input;
using Location = Microsoft.Maui.Devices.Sensors.Location;

namespace POIViewerMap.Views;
/// <summary>
/// Class <c>MapViewPage</c>
/// </summary>
public partial class MapViewPage : ContentPage
{
    private string FullFilepathRoute;
    static bool POIsReadIsBusy = false;
    static bool POIsMapUpdateIsBusy = false;
    static bool GeocodeIsActive = false;
    static int SearchRadius = 5; // km
    static readonly int MaxDistanceRefresh = 2; //km
    static readonly int MinZoomPOI = 290;
    static POIType CurrentPOIType = POIType.DrinkingWater;
    private List<POIData> pois = [];
    private static Location myCurrentLocation;
    static Location? myCurrentCenterMap = null;
    private static Location CurrentLocationOnLoad = null;
    private CompassData CurrentCompassReading;
    private readonly MyLocationLayer _myLocationLayer;
    private bool _disposed;
    public static bool IsAppStateSettingsBusy = false;
    public static bool IsSearchRadiusCircleBusy = false;
    public static Popup popup;
    private static bool FileListLocalAccess = false;
    public static ILayer myRouteLayer;
    private static List<AreaData> countries = null;
    //static GeoLocation? myCurrentCenterMap = null;
    static string SelectedCountryId = "GBR";
    static bool POIsDownloadIsBusy = false;
    public IAppSettings appSettings;
    CompositeDisposable deactivateWith;
    private string SelectedFilename { get; set; } = string.Empty;

    protected CompositeDisposable DeactivateWith => this.deactivateWith ??= [];
    protected CompositeDisposable DestroyWith { get; } = new CompositeDisposable();
    public ICommand TapCommand => new Command<string>(async (url) => await Launcher.OpenAsync(url));
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="appSettings">Only one setting used for display/hide of disclaimer popup</param>
    public MapViewPage(IAppSettings appSettings)
	{
		InitializeComponent();
        BindingContext = this;
        this.appSettings = appSettings;
        var items = new List<string>
        {
            AppResource.OptionsPOIPickerDrinkingWaterText,
            AppResource.OptionsPOIPickerCampsiteText,
            AppResource.OptionsPOIPickerBicycleShopText,
            AppResource.OptionsPOIPickerBicycleRepairStationText,
            AppResource.OptionsPOIPickerSupermarketText,
            AppResource.OptionsPOIPickerConvenienceStoreText,
            AppResource.OptionsPOIPickerChargingStationText,
            AppResource.OptionsPOIPickerATMText,
            AppResource.OptionsPOIPickerToiletText,
            AppResource.OptionsPOIPickerCafeText,
            AppResource.OptionsPOIPickerBakeryText,
            AppResource.OptionsPOIPickerPicnicTableText,
            AppResource.OptionsPOIPickerTrainStationText,
            AppResource.OptionsPOIPickerVendingMachineText,
            AppResource.OptionsPOIPickerLaundryText,
            
        };
        this.pickerpoi.ItemsSource = items;
        this.pickerpoi.SelectedIndex = 0;
        this.pickerRadius.SelectedIndex = 0;
        Mapsui.Logging.Logger.LogDelegate += (level, message, ex) =>
        {
        };// todo: Write to your own logger;
        AppIconHelper.InitializeIcons();
        
        _myLocationLayer?.Dispose();
        _myLocationLayer = new MyLocationLayer(mapView.Map)
        {
            IsCentered = true,
        };

        mapView.Map.Layers.Add(OpenStreetMap.CreateTileLayer());
        mapView.Map.Layers.Add(_myLocationLayer);

        // Get the lon lat coordinates from somewhere (Mapsui can not help you there)
        var center = new MPoint(-2.218266, 51.745564);
        myCurrentCenterMap = new Location()
        {
            Latitude = center.Y,
            Longitude = center.X
        };
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
        mapView.Map.Widgets.Add(new ScaleBarWidget(mapView.Map) { TextAlignment = Alignment.Center, VerticalAlignment = Mapsui.Widgets.VerticalAlignment.Top });

        mapView.PinClicked += (s, e) =>
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
                        if (Idx > -1)
                        {
                            if (Idx + AppResource.PinLabelDistanceText.Length < e.Pin.Label.Length)
                            {
                                // Remove previous distance value as we may have moved on the map so recalculate
                                e.Pin.Label = e.Pin.Label[..(Idx + AppResource.PinLabelDistanceText.Length)];
                                //e.Pin.Label += Format.FormatDistance(distance);
                            }
                            e.Pin.Label += FormatHelper.FormatDistance(distance);
                        }
                        e.Pin.ShowCallout();
                    }
                }
            }
            e.Handled = true;
        };
        mapView.MapClicked += (s, e) =>
        {
            if (POIsReadIsBusy)
                return;
            try
            {
                foreach (var pin in mapView.Pins)
                {
                    pin.HideCallout();
                }
            }
            catch (Exception) { }
        };
        mapView.TouchEnded += async (s, e) =>
        {
            if (POIsDownloadIsBusy || POIsMapUpdateIsBusy || POIsReadIsBusy || GeocodeIsActive)
                return;
            try
            {
                GeocodeIsActive = true;
                this.activityloadcountryindicatorlayout.IsVisible = true;
                var lonlat = SphericalMercator.ToLonLat(mapView.Map.Navigator.Viewport.CenterX, mapView.Map.Navigator.Viewport.CenterY);
                var location = new GeoLocation()
                {
                    Latitude = lonlat.lat,
                    Longitude = lonlat.lon
                };
                myCurrentCenterMap.Latitude = location.Latitude;
                myCurrentCenterMap.Longitude = location.Longitude;
                var country = ReverseGeocodeService.FindCountry(location);
                if (country == null)
                {
                    //await PopulateMapAsync(pois);
                    GeocodeIsActive = false;
                    this.currentcountry.Text = "?";
                    return;
                }
                if (!country.Id.Equals(SelectedCountryId))
                {
                    SelectedCountryId = country.Id;
                    var name = FormatHelper.TranslateCountryName(FormatHelper.GetCountryCodeFromReverseGeocode(country.Id));
                    this.SelectedFilename = $"{name}.bin";
                    this.currentcountry.Text = name;
                    POIServerFileDownload();
                }
                else
                    await PopulateMapAsync(pois);
                GeocodeIsActive = false;
                activityloadcountryindicatorlayout.IsVisible = false;
            }
            catch (Exception ex) { activityloadcountryindicatorlayout.IsVisible = false; GeocodeIsActive = false; }
        };
        ToggleCompass();
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            // No internet
            //Toast.Make(AppResource.ServerAccessFailedMsg, ToastDuration.Long).Show();
            FileListLocalAccess = true;
        }
        else
        {
            FileListLocalAccess = false;
        }
        var lonlat = SphericalMercator.ToLonLat(mapView.Map.Navigator.Viewport.CenterX, mapView.Map.Navigator.Viewport.CenterY);
        center = new MPoint(-2.218266, 51.745564);
        var location = new GeoLocation()
        {
            Latitude = center.Y,
            Longitude = center.X
        };
        this.activityloadindicatorlayout.IsVisible = true;
        this.activityloadcountryindicatorlayout.IsVisible = true;
        this.pickerpoi.IsEnabled = false;
        this.pickerRadius.IsEnabled = false;
        LoadCountries();
        var country = ReverseGeocodeService.FindCountry(location);
        if (country != null)
        {
            var name = FormatHelper.TranslateCountryName(FormatHelper.GetCountryCodeFromReverseGeocode(country.Id));
            this.SelectedFilename = $"{name}.bin";
            this.currentcountry.Text = name;
            POIServerFileDownload();
        }
        // From GPS - not windows TODO iOS
        if (DeviceInfo.Current.Platform == DevicePlatform.Android)
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            GetCurrentDeviceLocation();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        _ = Observable
                .Interval(TimeSpan.FromMilliseconds(500))
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
                    {
                        //await UpdateSearchRadiusCircleOnMap(mapView, SearchRadius);
                        await GetCurrentDeviceLocationAsync();
                    }                    
                    mapView.MyLocationLayer.Enabled = false;
                    _myLocationLayer.Enabled = true;
                    //await CheckLoadingDistance();
                });
        _ = Observable
                .Interval(TimeSpan.FromSeconds(1))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    this.POIsFoundLabel.Text = $"{mapView.Pins.Count}";
                });
    }
    private void LoadCountries()
    {
        LoadCountriesAsync();
        this.pickerpoi.IsEnabled = true;
        this.pickerRadius.IsEnabled = true;
        this.activityloadindicatorlayout.IsVisible = false;
        this.activityloadcountryindicatorlayout.IsVisible = false;
    }
    private async void LoadCountriesAsync()
    {
        try
        {
            await ReverseGeocodeService.LoadCountriesAsync();
        }
        catch (Exception ex)
        {

        }
        // or from web
        //var webhelper = new WebHelper();
        //await webhelper.DownloadCountriesFileAsync("countries.bin");
        //if (File.Exists(WebHelper.localPath))
        //{
        //    countries = await ReverseGeocodeLib.Deserializer.DeserializeAsync(WebHelper.localPath);
        //}
    }
    private async Task CheckLoadingDistance()
    {
        if(CurrentLocationOnLoad == null) return;
        var distance = Location.CalculateDistance(CurrentLocationOnLoad.Latitude,
                                                 CurrentLocationOnLoad.Longitude,
                                                 new Location(mapView.MyLocationLayer.MyLocation.Latitude, mapView.MyLocationLayer.MyLocation.Longitude),
                                                 DistanceUnits.Kilometers);
        if(distance > MaxDistanceRefresh)
        {
            this.pickerpoi.IsEnabled = false;
            this.pickerRadius.IsEnabled = false;
            this.activityloadindicatorlayout.IsVisible = true;
            if (!POIsReadIsBusy)
                await PopulateMapAsync(pois);
            this.pickerpoi.IsEnabled = true;
            this.pickerRadius.IsEnabled = true;
            this.activityloadindicatorlayout.IsVisible = false;
            CurrentLocationOnLoad = myCurrentLocation;
        }        
    }
    /// <summary>
    /// <c>UpdateVisiblePinsLabelDistanceText</c>
    /// Updates distance value on visible pins on map
    /// </summary>
    /// <returns>Task completed</returns>
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
    /// <summary>
    /// <c>UpdateSearchRadiusCircleOnMap</c>
    /// </summary>
    /// <param name="mapView"></param>
    /// <param name="radius">Picker value</param>
    /// <returns>Task completed</returns>
    //private static Task UpdateSearchRadiusCircleOnMap(MapView mapView, int radius)
    //{
    //    if (IsSearchRadiusCircleBusy || myCurrentLocation == null)
    //    { return Task.CompletedTask; }
    //    try
    //    {
    //        var location = new GeoLocation()
    //        {
    //            Latitude = myCurrentLocation.Latitude,
    //            Longitude = myCurrentLocation.Longitude
    //        };
    //        var circle = new Circle
    //        {
    //            Center = new Mapsui.UI.Maui.Position(location.Latitude, location.Longitude),
    //            Radius = Distance.FromKilometers(radius),
    //            Quality = 100,
    //            StrokeColor = new Color(255, 153, 0),
    //            StrokeWidth = 2,
    //            FillColor = new Color(255, 153, 0, 0.0541f),
    //        };
    //        IsSearchRadiusCircleBusy = true;
    //        mapView.Drawables.Clear();
    //        mapView.Drawables.Add(circle);
    //        IsSearchRadiusCircleBusy = false;
    //    }
    //    catch (Exception ex) { }
    //    return Task.CompletedTask;
    //}
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
    /// <summary>
    /// <c>GetCurrentDeviceLocationAsync</c>
    /// </summary>
    /// <returns></returns>
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
            _myLocationLayer.UpdateMySpeed(1.6);
            await UpdateVisiblePinsLabelDistanceText();
            POIsMapUpdateIsBusy = false;
        });
    }
    /// <summary>
    /// <c>OnPOIPickerSelectedIndexChanged</c>
    /// Populates or re-populates the map on POI picker index change event
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    async void OnPOIPickerSelectedIndexChanged(object sender, EventArgs e)
    {
        if (POIsReadIsBusy)
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
                        var sphericalMercatorCoordinate = SphericalMercator.FromLonLat(myCurrentCenterMap.Longitude, myCurrentCenterMap.Latitude).ToMPoint();
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
                this.pickerpoi.IsEnabled = false;
                this.pickerRadius.IsEnabled = false;
                this.activityloadindicatorlayout.IsVisible = true;
                if (!POIsReadIsBusy)
                    await PopulateMapAsync(pois);
                this.activityloadindicatorlayout.IsVisible = false;
                this.pickerpoi.IsEnabled = true;
                this.pickerRadius.IsEnabled = true;
                this.pickerpoi.Title = picker.Items[selectedIndex];
            }
        }
    }
    /// <summary>
    /// <c>OnRadiusPickerSelectedIndexChanged</c>
    /// Populates or re-populates the map on Search Radius picker index change event
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
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
                //if (mapView.Map.Navigator.Viewport.Resolution >= MinZoomPOI)
                //{
                //    //if (myCurrentLocation != null)
                //    {
                //        //var sphericalMercatorCoordinate = SphericalMercator.FromLonLat(myCurrentLocation.Longitude, myCurrentLocation.Latitude).ToMPoint();
                //        var sphericalMercatorCoordinate = SphericalMercator.FromLonLat(myCurrentCenterMap.Longitude, myCurrentCenterMap.Latitude).ToMPoint();
                //        mapView.Map.Navigator.CenterOnAndZoomTo(sphericalMercatorCoordinate, mapView.Map.Navigator.Resolutions[12], -1, Mapsui.Animations.Easing.CubicOut);
                //    }
                //    //else
                //    //{
                //    //    var center = new MPoint(-2.218266, 51.745564);
                //    //    // OSM uses spherical mercator coordinates. So transform the lon lat coordinates to spherical mercator
                //    //    var sphericalMercatorCoordinate = SphericalMercator.FromLonLat(center.X, center.Y).ToMPoint();
                //    //    mapView.Map.Navigator.CenterOnAndZoomTo(sphericalMercatorCoordinate, mapView.Map.Navigator.Resolutions[12], -1, Mapsui.Animations.Easing.CubicOut);
                //    //}
                //}
                //await UpdateSearchRadiusCircleOnMap(mapView, SearchRadius);
                this.pickerpoi.IsEnabled = false;
                this.pickerRadius.IsEnabled = false;
                this.activityloadindicatorlayout.IsVisible = true;
                if (!POIsReadIsBusy)
                    await PopulateMapAsync(pois);
                this.activityloadindicatorlayout.IsVisible = false;
                this.pickerpoi.IsEnabled = true;
                this.pickerRadius.IsEnabled = true;
            }
        }
    }
    /// <summary>
    /// <c>ShowRouteLoadFailToastMessage</c>
    /// Displays a toast message if Import route fails
    /// </summary>
    /// <param name="mess"></param>
    private static void ShowRouteLoadFailToastMessage(string mess)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            // Code to run on the main thread
            CancellationTokenSource cancellationTokenSource = new();
            ToastDuration duration = ToastDuration.Long;
            double fontSize = 15;
            var toast = Toast.Make($"{AppResource.ImportRouteFailedMessage} {mess}", duration, fontSize);
            await toast.Show(cancellationTokenSource.Token);
        });
    }
    /// <summary>
    /// <c>BrowseRoutesButton_Clicked</c>
    /// Displays File Picker for GPX file import
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    async void BrowseRoutesButton_Clicked(object sender, EventArgs e)
    {
        if (POIsReadIsBusy)
            return;
        try
        {
            await BrowseRoutes();
            if (!String.IsNullOrEmpty(this.FullFilepathRoute))
            {
                if (myRouteLayer != null)
                    mapView.Map.Layers.Remove(myRouteLayer);
                this.activityrouteloadindicatorlayout.IsVisible = true;
                this.RouteImported.IsVisible = true;
                this.RouteImported.Text = System.IO.Path.GetFileNameWithoutExtension(this.FullFilepathRoute);
                var gpxFile = await GpxFile.LoadAsync(FullFilepathRoute);
                var countTracks = gpxFile.Tracks.Count;
                var countRoutes = gpxFile.Routes.Count;
                var countWaypoints = gpxFile.Waypoints.Count;
                var data = new RouteLineData();
                var sb = new StringBuilder("LINESTRING(");
                foreach (var track in gpxFile.Tracks)
                {
                    foreach (var seg in track.Segments)
                    {
                        foreach (var point in seg.Points)
                        {
                            FormattableString fms = $"{point.Latitude} {point.Longitude},";
                            sb.Append(fms.ToInvariantString());
                        }
                    }
                }
                sb.Append(')');
                myRouteLayer = CreateLineStringLayer(sb.ToString().Replace(",)", ")"), CreateLineStringStyle());
                mapView.Map.Layers.Add(myRouteLayer);
            }
        }
        catch (Exception ex)
        {
            ShowRouteLoadFailToastMessage(this.FullFilepathRoute);
            this.FullFilepathRoute = string.Empty;
        }
        finally { this.activityrouteloadindicatorlayout.IsVisible = false; }
    }
    /// <summary>
    /// <c>ToggleCompass</c>
    /// </summary>
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
    /// <summary>
    /// <c>Compass_ReadingChanged</c>
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Compass_ReadingChanged(object sender, CompassChangedEventArgs e)
    {
        CurrentCompassReading = e.Reading;
    }
    /// <summary>
    /// <c>CreateLineStringLayer</c>
    /// Creates a layer to display imported GPX route on the map
    /// </summary>
    /// <param name="line"></param>
    /// <param name="style"></param>
    /// <returns></returns>
    public static ILayer CreateLineStringLayer(string line, IStyle style = null)
    {
        var lineString = (LineString)new WKTReader().Read(line);
        lineString = new LineString([.. lineString.Coordinates.Select(v => SphericalMercator.FromLonLat(v.Y, v.X).ToCoordinate())]);

        return new MemoryLayer
        {
            Features = new[] { new GeometryFeature { Geometry = lineString } },
            Name = "LineStringLayer",
            Style = style

        };
    }
    /// <summary>
    /// <c>CreateLineStringStyle</c>
    /// </summary>
    /// <returns>VectorStyle</returns>
    public static IStyle CreateLineStringStyle()
    {
        return new VectorStyle
        {
            Fill = null,
            Outline = null,
            Line = { Color = Mapsui.Styles.Color.FromString("Red"), Width = 4 }
        };
    }
    /// <summary>
    /// <c>BrowseRoutes</c>
    /// Sets FullFilepathRoute variable to chose file path
    /// </summary>
    /// <returns>Task completed</returns>
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
        if (result is null) return;
        if (System.IO.Path.GetExtension(result.FileName).Equals(".gpx"))
        {
            this.FullFilepathRoute = result?.FullPath;
        }
    }
    /// <summary>
    /// <c></c>
    /// </summary>
    /// <param name="options"></param>
    /// <returns>List of </returns>
    public static async Task<FileResult> PickAndShow(PickOptions options)
    {
        try
        {
            var result = await FilePicker.PickAsync(options);
            return result;
        }
        catch (Exception ex)
        {
            // The user canceled or something went wrong
        }
        return null;
    }
    /// <summary>
    /// <c>GetCurrentDeviceLocation</c>
    /// Set current lat,lon on Location Layer on map
    /// </summary>
    /// <returns>Task completed</returns>
    private async Task GetCurrentDeviceLocation()
    {
        if(!Geolocation.IsEnabled)
        {
            var popup = new SfPopup();
            popup.ShowFooter = true;
            popup.HeaderTitle = AppResource.WarningPopupTitleText;
            popup.Message = AppResource.WarnPleaseEnableGPSText;
            popup.AppearanceMode = PopupButtonAppearanceMode.OneButton;
            popup.AcceptButtonText = AppResource.OKText;
            await popup.ShowAsync();
            return;
        }
        var request = new GeolocationRequest(GeolocationAccuracy.Best);
        myCurrentLocation = await Geolocation.GetLocationAsync(request, new CancellationToken());
        if (myCurrentLocation != null)
        {
            mapView.MyLocationLayer.UpdateMyLocation(new Mapsui.UI.Maui.Position(myCurrentLocation.Latitude, myCurrentLocation.Longitude));
        }
    }
    /// <summary>
    /// <c>PopulateMapAsync</c>
    /// Populates map with current POIType as icons
    /// </summary>
    /// <param name="pois">List of country Points Of Interest</param>
    /// <returns>Task completed</returns>
    private async Task PopulateMapAsync(List<POIData> pois)
    {
        await Task.Factory.StartNew(() =>
        {
            try
            {
                POIsReadIsBusy = true;
                // Remove Pins outside search area
                RemovePinsOutsideSearchArea(mapView, SearchRadius);
                foreach (var pin in mapView.Pins)
                {
                    pin.HideCallout();
                }
                CheckPinsInSearchArea(mapView, SearchRadius);
                if (mapView.Map.Navigator.Viewport.Resolution > MinZoomPOI)
                {
                    mapView.Pins.Clear();
                    return;
                }
                var myLocation = new Location(mapView.MyLocationLayer.MyLocation.Latitude, mapView.MyLocationLayer.MyLocation.Longitude);
                foreach (var poi in pois)
                {
                    //var distance = Location.CalculateDistance(poi.Latitude,
                    //                                            poi.Longitude,
                    //                                            new Location(mapView.MyLocationLayer.MyLocation.Latitude, mapView.MyLocationLayer.MyLocation.Longitude),
                    //                                            DistanceUnits.Kilometers);
                    var distance = Location.CalculateDistance(poi.Latitude,
                                                              poi.Longitude,
                                                              new Location(myCurrentCenterMap.Latitude, myCurrentCenterMap.Longitude),
                                                              DistanceUnits.Kilometers);
                    if (distance > SearchRadius)
                        continue;
                    if (mapView.Pins.Any(p => p.Position.Latitude == poi.Latitude && p.Position.Longitude == poi.Longitude))
                        continue;
                    if (poi.POI != CurrentPOIType)
                        continue;
                    var space = "\r";
                    var label = poi.Title;
                    // Langs for Name: here
                    if (String.IsNullOrEmpty(label))
                        space = string.Empty;
                    else
                        label = $"{AppResource.NameText} {poi.Title}";                                     
                    var space2 = string.Empty;
                    var subtitle = FormatHelper.GetSubTitleLang(poi.Subtitle);
                    if (String.IsNullOrEmpty(subtitle))
                        space2 = string.Empty;
                    else
                        space2 = "\r";
                    label = $"{label}{space}{subtitle}{space2}{AppResource.PinLabelDistanceText}{FormatHelper.FormatDistance(distance)}";
                    var myPin = new Pin(mapView)
                    {
                        Position = new Mapsui.UI.Maui.Position(poi.Latitude, poi.Longitude),
                        Type = PinType.Svg,
                        Label = label,
                        Address = "",
                        Svg = AppIconHelper.GetPOIIcon(poi),// eg. drinkingwaterStr,
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
        //await UpdateSearchRadiusCircleOnMap(mapView, SearchRadius);
    }
    private void RemovePinsOutsideSearchArea(MapView mapView, int searchRadius)
    {
        var pinsToRemove = new List<Pin>();
        foreach (var pin in mapView.Pins)
        {
            var distance = Location.CalculateDistance(pin.Position.Latitude,
                                                  pin.Position.Longitude,
                                                  new Location(myCurrentCenterMap.Latitude, myCurrentCenterMap.Longitude),
                                                  DistanceUnits.Kilometers);
            if (distance > searchRadius)
            {
                pinsToRemove.Add(pin);
            }
        }
        foreach (var pin in pinsToRemove)
        {
            mapView.Pins.Remove(pin);
        }
    }
    private void CheckPinsInSearchArea(MapView mapView, int searchRadius)
    {
        foreach (var pin in mapView.Pins)
        {
            var distance = Location.CalculateDistance(pin.Position.Latitude,
                                                  pin.Position.Longitude,
                                                  new Location(myCurrentCenterMap.Latitude, myCurrentCenterMap.Longitude),
                                                  DistanceUnits.Kilometers);
            if (distance <= searchRadius)
            {
                pin.IsVisible = true;
            }
        }
    }
    private async Task AllowCenterMap_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
    }
    private void ShowSearchRadiusOnMap_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        
    }
    /// <summary>
    /// <c>POIServerFileDownload</c>
    /// File download from remote server or local storage
    /// </summary>
    private async void POIServerFileDownload()
    {
        if (String.IsNullOrEmpty(this.SelectedFilename) || POIsDownloadIsBusy)
        {
            this.activityloadindicatorlayout.IsVisible = false;
            this.activityloadcountryindicatorlayout.IsVisible = false;
            return; // No file
        }
        //this.pickerpoi.IsEnabled = false;
        //this.pickerRadius.IsEnabled = false;
        //this.activityloadindicatorlayout.IsVisible = true;
        pois.Clear();
        foreach (var pin in mapView.Pins)
        {
            pin.HideCallout();
        }
        mapView.Pins.Clear();
        // Download chosen file, is it local?
        if (FileListLocalAccess)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                // Code to run on the main thread
                CancellationTokenSource cancellationTokenSource = new();
                ToastDuration duration = ToastDuration.Long;
                double fontSize = 15;
                var toast = Toast.Make($"{AppResource.ServerAccessFailedMsg}", duration, fontSize);
                await toast.Show(cancellationTokenSource.Token);
            });
            var res = POIBinaryFormat.Read(System.IO.Path.Combine(FileSystem.AppDataDirectory, $"{FilenameHelper.GetCountryCodeFromTranslatedCountry(System.IO.Path.GetFileNameWithoutExtension(this.SelectedFilename))}.bin"));
            if (res != null && !POIsReadIsBusy)
            {
                pois = res.POIs;
                await PopulateMapAsync(pois);
            }                
        }
        else
        {
            POIsDownloadIsBusy = true;
            var webhelper = new WebHelper();
            await webhelper.DownloadPOIFileAsync(this.SelectedFilename);
            if (File.Exists(WebHelper.localPath))
            {
                var res = POIBinaryFormat.Read(WebHelper.localPath);
                if (res != null && !POIsReadIsBusy)
                {
                    pois = res.POIs;
                    await PopulateMapAsync(pois);
                }                   
            }
        }
        //this.pickerpoi.IsEnabled = true;
        //this.pickerRadius.IsEnabled = true;
        //this.activityloadindicatorlayout.IsVisible = false;
        //this.activityloadcountryindicatorlayout.IsVisible = false;
        POIsDownloadIsBusy = false;
    }
}