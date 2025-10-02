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
using POIViewerMap.Popups;
using POIViewerMap.Resources.Strings;
using POIViewerMap.Stores;
using ReactiveUI;
using RolandK.Formats.Gpx;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using Color = Microsoft.Maui.Graphics.Color;
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
    static int SearchRadius = 5; // km
    static readonly int MaxDistanceRefresh = 2; //km
    static readonly int MinZoomPOI = 290;
    static POIType CurrentPOIType = POIType.DrinkingWater;
    private List<POIData> pois = [];
    private static Location myCurrentLocation;
    private static Location CurrentLocationOnLoad = null;
    private CompassData CurrentCompassReading;
    private readonly MyLocationLayer _myLocationLayer;
    private bool _disposed;
    public static bool IsAppStateSettingsBusy = false;
    public static bool IsSearchRadiusCircleBusy = false;
    public static Popup popup;
    private static bool FileListLocalAccess = false;
    public static ILayer myRouteLayer;
    public IAppSettings appSettings;
    CompositeDisposable deactivateWith;
    private string SelectedFilename { get; set; } = string.Empty;

    protected CompositeDisposable DeactivateWith => this.deactivateWith ??= [];
    protected CompositeDisposable DestroyWith { get; } = new CompositeDisposable();

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="appSettings">Only one setting used for display/hide of disclaimer popup</param>
    public MapViewPage(IAppSettings appSettings)
	{
		InitializeComponent();
        this.appSettings = appSettings;
        var items = new List<string>
        {
            AppResource.OptionsPOIPickerDrinkingWaterText,
            AppResource.OptionsPOIPickerCampsiteText,
            AppResource.OptionsPOIPickerBicycleShopText,
            AppResource.OptionsPOIPickerBicycleRepairStationText,
            AppResource.OptionsPOIPickerSupermarketText,
            AppResource.OptionsPOIPickerATMText,
            AppResource.OptionsPOIPickerToiletText,
            AppResource.OptionsPOIPickerCafeText,
            AppResource.OptionsPOIPickerBakeryText,
            AppResource.OptionsPOIPickerPicnicTableText,
            AppResource.OptionsPOIPickerTrainStationText,
            AppResource.OptionsPOIPickerVendingMachineText,
            AppResource.OptionsPOIPickerLaundryText
        };
       this.picker.ItemsSource = items;
        InitializeServerFilenamePicker();

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
                this.expander.IsExpanded = false;
                //this.expander.IsVisible = false;
            }
            catch (Exception) { }
        };
        ToggleCompass();
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
        _ = Observable
                .Interval(TimeSpan.FromSeconds(1))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    this.POIsFoundLabel.Text = $"{mapView.Pins.Count}";
                });
    }
    /// <summary>
    /// <c>OnNavigatedTo</c>
    /// Displays disclaimer popup if ShowPopupAtStart is true
    /// </summary>
    /// <param name="args"></param>
    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        if (!appSettings.ShowPopupAtStart) return;
        AppUsagePopup popup = new ();
        popup.ShowPopupAtStartup = appSettings.ShowPopupAtStart;
        popup.Closed += Popup_Closed;
        this.ShowPopup(popup);
    }
    /// <summary>
    /// <c>PopupClosed</c>
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Popup_Closed(object sender, PopupClosedEventArgs e)
    {
        if(sender is AppUsagePopup)
            appSettings.ShowPopupAtStart = (bool)e.Result;
    }
    /// <summary>
    /// <c>CheckLoadingDistance</c>
    /// Have we moved since last poi load/search radius by more than 2km
    /// if so update all Pins on map but only if Center Map On My Position check box is checked
    /// </summary>
    /// <returns></returns>
    private async Task CheckLoadingDistance()
    {
        if(CurrentLocationOnLoad == null) return;
        var distance = Location.CalculateDistance(CurrentLocationOnLoad.Latitude,
                                                 CurrentLocationOnLoad.Longitude,
                                                 new Location(mapView.MyLocationLayer.MyLocation.Latitude, mapView.MyLocationLayer.MyLocation.Longitude),
                                                 DistanceUnits.Kilometers);
        if(distance > MaxDistanceRefresh)
        {
            this.picker.IsEnabled = false;
            this.pickerRadius.IsEnabled = false;
            this.activityloadindicatorlayout.IsVisible = true;
            if (!POIsReadIsBusy)
                await PopulateMapAsync(pois);
            this.picker.IsEnabled = true;
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
    /// <c>OnPickerSelectedIndexChanged</c>
    /// Populates or re-populates the map on Country picker index change event
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
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
                this.picker.IsEnabled = false;
                this.pickerRadius.IsEnabled = false;
                this.activityloadindicatorlayout.IsVisible = true;
                if (!POIsReadIsBusy)
                    await PopulateMapAsync(pois);
                this.activityloadindicatorlayout.IsVisible = false;
                this.picker.IsEnabled = true;
                this.pickerRadius.IsEnabled = true;
                this.picker.Title = picker.Items[selectedIndex];
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
                this.picker.IsEnabled = false;
                this.pickerRadius.IsEnabled = false;
                this.activityloadindicatorlayout.IsVisible = true;
                if (!POIsReadIsBusy)
                    await PopulateMapAsync(pois);
                this.activityloadindicatorlayout.IsVisible = false;
                this.picker.IsEnabled = true;
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
                    var space = "\r";
                    var label = poi.Title;
                    // Langs for Name: here
                    if (String.IsNullOrEmpty(label))
                        space = string.Empty;
                    else
                        label = $"{AppResource.NameText} {poi.Title}";                                     
                    var space2 = string.Empty;
                    var subtitle = FormatHelper.GetSubTitleLang(poi.Subtitle);
                    subtitle = FormatHelper.FormatOpeningHours(subtitle);
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
        await UpdateSearchRadiusCircleOnMap(mapView, SearchRadius);
    }
    /// <summary>
    /// <c>InitializeServerFilenamePicker</c>
    /// Gets a list of filenames (2 letter country code for the filename) from remote server or from local storage
    /// </summary>
    private async void InitializeServerFilenamePicker()
    {
        //this.POIServerFileDownloadButton.IsEnabled = false;
        var webhelper = new WebHelper();
        var parameters = new Dictionary<string, string>
        {
            { WebHelper.PARAM_ACTION, WebHelper.ACTION_FILES },
            { WebHelper.PARAM_FILE_NAME, "Show All" }
        };
        var serverlist = await webhelper.FilenamesFetchAsync(parameters);
        if (serverlist.Error)
        {
            await Toast.Make($"{serverlist.ErrorMsg}").Show();
            // Try local storage
            string[] files = Directory.GetFiles(FileSystem.AppDataDirectory, "*.bin");
            if (files.Length > 0)
            {
                // Local files found
                FileListLocalAccess = true;
                var ff = new FileFetch();
                foreach (var item in files)
                {
                    if (item == null) continue;
                    ff.Names.Add(FormatHelper.TranslateCountryName(System.IO.Path.GetFileNameWithoutExtension(item)));
                }
                ff.LastUpdated = new DateTime();
                FilenameComparer.filenameSortOrder = FilenameComparer.SortOrder.asc;
                ff.Names.Sort(FilenameComparer.NameArray);
                this.serverfilenamepicker.ItemsSource = ff.Names;
            }
        }
        else if (serverlist.Names.Count > 0)
        {
            FileListLocalAccess = false;
            var ff = new FileFetch();
            foreach (var item in serverlist.Names)
            {
                if (item == null) continue;
                ff.Names.Add(FormatHelper.TranslateCountryName(System.IO.Path.GetFileNameWithoutExtension(item)));
            }
            FilenameComparer.filenameSortOrder = FilenameComparer.SortOrder.asc;
            ff.Names.Sort(FilenameComparer.NameArray);
            this.serverfilenamepicker.ItemsSource = ff.Names;
        }        
    }
    private void AllowCenterMap_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        
    }
    private void ShowSearchRadiusOnMap_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        
    }
    /// <summary>
    /// <c>expander_ExpandedChanged</c>
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void expander_ExpandedChanged(object sender, ExpandedChangedEventArgs e)
    {
        if (e.IsExpanded)
        {
            //InitializeServerFilenamePicker();
            if (this.serverfilenamepicker.SelectedItem != null)
            {
                this.serverfilenamepicker.Title = this.serverfilenamepicker.SelectedItem.ToString();
            }
        }
        else
            this.expander.IsExpanded = false;
    }
    /// <summary>
    /// <c>serverfilenamepicker_SelectedIndexChanged</c>
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void serverfilenamepicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        var v = serverfilenamepicker.SelectedIndex;
        var s = serverfilenamepicker.SelectedItem as string;
        if (v == -1 || s.Equals("No Data"))
        {
            this.SelectedFilename = string.Empty;
            return;
        }
        this.SelectedFilename = $"{s}.bin";
        serverfilenamepicker.Title = s;
        POIServerFileDownload();
    }
    /// <summary>
    /// <c>POIServerFileDownload</c>
    /// Fires file download from picker selected index from remote server or local storage
    /// </summary>
    private async void POIServerFileDownload()
    {
        if (String.IsNullOrEmpty(this.SelectedFilename))
        {
            this.activityloadindicatorlayout.IsVisible = false;
            return; // No file
        }
        serverfilenamepicker.IsEnabled = false;
        this.picker.IsEnabled = false;
        this.pickerRadius.IsEnabled = false;
        this.activityloadindicatorlayout.IsVisible = true;
        pois.Clear();
        foreach (var pin in mapView.Pins)
        {
            pin.HideCallout();
        }
        mapView.Pins.Clear();
        // Download chosen file, is it local?
        if (FileListLocalAccess)
        {
            var res = POIBinaryFormat.Read(System.IO.Path.Combine(FileSystem.AppDataDirectory, $"{FilenameHelper.GetCountryCodeFromTranslatedCountry(System.IO.Path.GetFileNameWithoutExtension(this.SelectedFilename))}.bin"));
            if (res != null && !POIsReadIsBusy)
            {
                pois = res.POIs;
                await PopulateMapAsync(pois);
            }                
        }
        else
        {
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
        this.picker.IsEnabled = true;
        this.pickerRadius.IsEnabled = true;
        this.serverfilenamepicker.IsEnabled=true;
        this.activityloadindicatorlayout.IsVisible = false;
    }
}