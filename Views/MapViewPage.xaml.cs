using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Storage;
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
using Mapsui.Widgets.ButtonWidget;
using Mapsui.Widgets.ScaleBar;
using Microsoft.Maui.Controls.Shapes;
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
using System.Globalization;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using UraniumUI.Pages;
using Color = Microsoft.Maui.Graphics.Color;
using Location = Microsoft.Maui.Devices.Sensors.Location;

namespace POIViewerMap.Views;

public partial class MapViewPage : UraniumContentPage
{
    private string FullFilepathRoute;
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
    private static bool FileListLocalAccess = false;
    public static ILayer myRouteLayer;
    public IAppSettings appSettings;
    CompositeDisposable? deactivateWith;
    //private static FileSaverResult filesaverresult;

    private string SelectedFilename { get; set; } = string.Empty;

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
        items.Add(AppResource.OptionsPOIPickerTrainStationText);
        items.Add(AppResource.OptionsPOIPickerVendingMachineText);
        items.Add(AppResource.OptionsPOIPickerLaundryText);
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
        mapView.PinClicked += OnPinClicked;
        mapView.MapClicked += OnMapClicked;
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
                    this.POIsFoundLabel.Text = Convert.ToString(mapView.Pins.Count);
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
        if(sender is AppUsagePopup)
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
            this.picker.IsEnabled = false;
            this.pickerRadius.IsEnabled = false;
            this.activityloadindicatorlayout.IsVisible = true;
            await PopulateMapAsync(pois);
            this.picker.IsEnabled = true;
            this.pickerRadius.IsEnabled = true;
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
            _myLocationLayer.UpdateMySpeed(1.6);
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
            //this.expander.IsExpanded = false;
            //this.expander.IsVisible = false;
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
                this.picker.IsEnabled = false;
                this.pickerRadius.IsEnabled = false;
                this.activityloadindicatorlayout.IsVisible = true;
                await PopulateMapAsync(pois);
                this.activityloadindicatorlayout.IsVisible = false;
                this.picker.IsEnabled = true;
                this.pickerRadius.IsEnabled = true;
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
                this.picker.IsEnabled = false;
                this.pickerRadius.IsEnabled = false;
                this.activityloadindicatorlayout.IsVisible = true;
                await PopulateMapAsync(pois);
                this.activityloadindicatorlayout.IsVisible = false;
                this.picker.IsEnabled = true;
                this.pickerRadius.IsEnabled = true;
            }
        }
    }
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
                sb.Append(")");
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
                    label = $"{label}{space}{subtitle}{space2}Distance: {FormatHelper.FormatDistance(distance)}";
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
                    ff.Names.Add(FilenameHelper.GetCountryNameFromCountryCode(System.IO.Path.GetFileNameWithoutExtension(item)));
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
                ff.Names.Add(FilenameHelper.GetCountryNameFromCountryCode(System.IO.Path.GetFileNameWithoutExtension(item)));
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
    private void DeleteButton_Clicked(object sender, EventArgs e)
    {
        //this.OptionsRouteDeleteButton.IsVisible = false;
        if(myRouteLayer!= null)
        {
            //this.FilepathRouteLabel.Text = string.Empty;
            mapView.Map.Layers.Remove(myRouteLayer);
        }        
    }
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
        // Download chosen file or local?
        if (FileListLocalAccess)
        {
            pois = await POIBinaryFormat.ReadAsync(System.IO.Path.Combine(FileSystem.AppDataDirectory, $"{FilenameHelper.GetCountryCodeFromCountry(System.IO.Path.GetFileNameWithoutExtension(this.SelectedFilename))}.bin"));
            await PopulateMapAsync(pois);
        }
        else
        {
            var webhelper = new WebHelper();
            await webhelper.DownloadPOIFileAsync(this.SelectedFilename);
            if (File.Exists(WebHelper.localPath))
            {
                pois = await POIBinaryFormat.ReadAsync(WebHelper.localPath);
                await PopulateMapAsync(pois);
            }
        }
        this.picker.IsEnabled = true;
        this.pickerRadius.IsEnabled = true;
        this.serverfilenamepicker.IsEnabled=true;
        this.activityloadindicatorlayout.IsVisible = false;
    }
}