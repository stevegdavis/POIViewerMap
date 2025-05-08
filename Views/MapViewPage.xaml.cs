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
using Mapsui.Utilities;
using Mapsui.VectorTileLayers.Core.Enums;
using Mapsui.VectorTileLayers.Core.Renderer;
using Mapsui.VectorTileLayers.Core.Styles;
using Mapsui.VectorTileLayers.OpenMapTiles;
using Mapsui.Widgets;
using Mapsui.Widgets.ScaleBar;
using Microsoft.Maui.Media;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.HPRtree;
using NetTopologySuite.IO;
using POIBinaryFormatLib;
using POIViewerMap.DataClasses;
using POIViewerMap.Helpers;
using POIViewerMap.Popups;
using POIViewerMap.Resources.Strings;
using POIViewerMap.Stores;
using ReactiveUI;
using RolandK.Formats.Gpx;
using SkiaSharp;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
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
    private string SelectedMbTilesFilename { get; set; } = string.Empty;
    private static MbTileFilesFetch servermbtff = new(); // MbTiles from server
    private static MbTileFilesFetch localmbtff = new();  // MbTiles from local storage
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
        InitializePOIsServerFilenamePicker();

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
        mapView.Map.Widgets.Add(new ScaleBarWidget(mapView.Map) { TextAlignment = Mapsui.Widgets.Alignment.Center, VerticalAlignment = Mapsui.Widgets.VerticalAlignment.Top });
        mapView.Renderer.StyleRenderers[typeof(BackgroundTileStyle)] = new BackgroundTileStyleRenderer();
        mapView.Renderer.StyleRenderers[typeof(RasterTileStyle)] = new RasterTileStyleRenderer();
        mapView.Renderer.StyleRenderers[typeof(VectorTileStyle)] = new VectorTileStyleRenderer();
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
    /// <c>Localmapfilenamepicker_SelectedIndexChanged</c>
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void Localmapfilenamepicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (localmapfilenamepicker.SelectedIndex == -1)
            return;
        this.servermapfilenamepicker.IsEnabled = false;
        this.localmapfilenamepicker.IsEnabled = false;
        this.titleloadinglabel.Text = AppResource.LoadingOfflineMapLabelText;
        this.activityloadingofflinemapsindicatorlayout.IsVisible = true;
        var v = localmapfilenamepicker.SelectedIndex;
        var s = localmapfilenamepicker.SelectedItem as string;
        if (v == -1 || s.Equals("No Data"))
        {
            this.SelectedMbTilesFilename = string.Empty;
            return;
        }
        localmapfilenamepicker.Title = s;
        foreach (var item in localmbtff.MbTileFiles)
        {
            var name = item.Name[0..(item.Name.IndexOf("{"))];
            name = FormatHelper.TranslateCountryName(Path.GetFileNameWithoutExtension(name));
            if (name.Equals(s))
            {
                this.SelectedMbTilesFilename = item.Name;
                break;
            }
        }
        try
        {
            await LoadMapboxGL();
        }
        catch { }
        finally { this.activityloadingofflinemapsindicatorlayout.IsVisible = false; this.servermapfilenamepicker.IsEnabled = true; this.localmapfilenamepicker.IsEnabled = true; }
    }
    /// <summary>
    /// <c>serverfilenamepicker_SelectedIndexChanged</c>
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void ServerMapfilenamepicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        var v = servermapfilenamepicker.SelectedIndex;
        var s = servermapfilenamepicker.SelectedItem as string;
        if (v == -1 || s.Equals("No Data"))
        {
            this.SelectedMbTilesFilename = string.Empty;
            return;
        }
        this.servermapfilenamepicker.Title = s;
        foreach (var item in servermbtff.MbTileFiles)
        {
            var name = item.Name[0..(item.Name.IndexOf("{"))];
            name = FormatHelper.TranslateCountryName(name);
            if (name.Equals(s))
            {
                this.SelectedMbTilesFilename = item.Name;
                break;
            }
        }
        if (this.DownloadMapsViaWiFi.IsChecked)
        {
            IEnumerable<ConnectionProfile> profiles = Connectivity.Current.ConnectionProfiles;
            if (!profiles.Contains(ConnectionProfile.WiFi))
            {
                // No connection to internet via Wifi
                await DisplayAlert(AppResource.ErrorText, AppResource.NoInternetConnectionText, AppResource.OKText);
                return;
            }
        }
        this.titleloadinglabel.Text = AppResource.DownloadingOfflineMapLabelText;
        this.activityloadingofflinemapsindicatorlayout.IsVisible = true;
        this.servermapfilenamepicker.IsEnabled = false;
        this.localmapfilenamepicker.IsEnabled = false;
        this.OfflineMaps.IsEnabled = false;
        this.DownloadMapsViaWiFi.IsEnabled = false;
        var ms = await DriveHelper.DriveDownloadFile(this.SelectedMbTilesFilename);
        if (ms != null)
        {
            using var fs = new FileStream($"{Path.Combine(FileSystem.AppDataDirectory, this.SelectedMbTilesFilename)}", FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            ms.WriteTo(fs);
            // Successfully downloaded so add to local picker - check for duplicates
            this.SelectedMbTilesFilename = fs.Name;
            this.mbtilebytes.Text = string.Empty;
            this.activityloadingofflinemapsindicatorlayout.IsVisible = false;
            var sourcenames = new List<string>();
            var fn = Path.GetFileName(this.SelectedMbTilesFilename);
            var Idx = fn.IndexOf("{");
            if (Idx > -1)
            {
                bool Found = false;
                var name = FormatHelper.TranslateCountryName(fn[..Idx]);
                if (this.localmapfilenamepicker.ItemsSource != null && this.localmapfilenamepicker.Items.Count > 0)
                {
                    foreach (var item in this.localmapfilenamepicker.Items)
                    {
                        if (!item.Contains("No Data"))
                            sourcenames.Add(item);
                        if (item.Equals(name))
                            Found = true;
                    }
                }
                if (!Found)
                {
                    var bbox = fn[(Idx + 1)..fn.IndexOf("}")]; // TODO
                    var Idx2 = fn.IndexOf("}");
                    if (Idx2 > -1)
                    {
                        var mbf = new MbTileFile();
                        // Get last update part
                        //}yyyyMMdd
                        var dts = fn.Substring(Idx2 + 1, 8);
                        mbf.LastUpdated = new DateTime(int.Parse(dts[..4]), int.Parse(dts[4..6]), int.Parse(dts[6..8]));
                        mbf.Name = fn;
                        sourcenames.Add(name);
                        localmbtff.MbTileFiles.Add(mbf);
                        FilenameComparer.filenameSortOrder = FilenameComparer.SortOrder.asc;
                        sourcenames.Sort(FilenameComparer.NameArray);
                        this.localmapfilenamepicker.ItemsSource = sourcenames;
                    }
                }
            }            
        }
        try
        {
            await LoadMapboxGL();
        }
        catch (AggregateException ae)
        {
            foreach (var ex in ae.InnerExceptions)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            this.activityloadingofflinemapsindicatorlayout.IsVisible = false;
            this.servermapfilenamepicker.IsEnabled = true;
            this.localmapfilenamepicker.IsEnabled = true;
            this.OfflineMaps.IsEnabled = true;
            this.DownloadMapsViaWiFi.IsEnabled = true;
        }
    }
    /// <summary>
    /// <c>LoadMapboxGL</c>
    /// Loads Mapbox GL layers from the specified MBTiles file and adds them to the map.
    /// </summary>
    /// <returns>Task completed</returns>
    public async Task LoadMapboxGL()
    {
        await Task.Factory.StartNew(() =>
        {
            OMTStyleFileLoader.DirectoryForFiles = FileSystem.AppDataDirectory;
            OMTStyleFileLoader.Filename = Path.GetFileName(this.SelectedMbTilesFilename);

            CheckForMBTilesFile(OMTStyleFileLoader.Filename, OMTStyleFileLoader.DirectoryForFiles);

            var stream = EmbeddedResourceLoader.Load("styles.osm-liberty.json", GetType()) ?? throw new FileNotFoundException($"styles.osm - liberty.json not found");

            var layers = new OpenMapTilesLayer(stream, GetLocalContent);
            mapView.Map.Layers.Clear();
            foreach (var layer in layers)
                mapView.Map.Layers.Add(layer);

            if (_myLocationLayer != null)
                mapView.Map.Layers.Add(_myLocationLayer);
            if (myRouteLayer != null)
                mapView.Map.Layers.Add(myRouteLayer);
        });
    }
    /// <summary>
    /// <c>CheckForMBTilesFile</c>
    /// </summary>
    /// <param name="filename"></param>
    /// <param name="dataDir"></param>
    /// <returns>filename if exists</returns>
    /// <exception cref="FileNotFoundException"></exception>
    private static string CheckForMBTilesFile(string filename, string dataDir)
    {
        if (!File.Exists(Path.Combine(dataDir, filename)))
        {
            throw new FileNotFoundException($"File {filename} not found");
        }
        return filename;
    }
    /// <summary>
    /// <c>GetLocalContent</c>
    /// </summary>
    /// <param name="type"></param>
    /// <param name="name"></param>
    /// <returns>Stream</returns>
    public Stream GetLocalContent(LocalContentType type, string name)
    {
        switch (type)
        {
            case LocalContentType.File:
                if (File.Exists(name))
                    return File.OpenRead(name);
                else
                    return null;
            case LocalContentType.Resource:
                return EmbeddedResourceLoader.Load(name, GetType());
        }
        return null;
    }
    /// <summary>
    /// <c>LoadFontResources</c>
    /// </summary>
    /// <param name="assemblyToUse"></param>
    public void LoadFontResources(Assembly assemblyToUse)
    {
        // Try to load this font from resources
        var resourceNames = assemblyToUse?.GetManifestResourceNames();

        foreach (var resourceName in resourceNames.Where(s => s.EndsWith(".ttf", System.StringComparison.CurrentCultureIgnoreCase)))
        {
            var fontName = resourceName.Substring(0, resourceName.Length - 4);
            fontName = fontName.Substring(fontName.LastIndexOf(".") + 1);

            using (var stream = assemblyToUse.GetManifestResourceStream(resourceName))
            {
                var typeface = SKFontManager.Default.CreateTypeface(stream);

                if (typeface != null)
                {
                    //((Mapsui.VectorTileLayers.OpenMapTiles.Utilities.FontMapper)Topten.RichTextKit.FontMapper.Default).Add(typeface);
                }
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
    /// Gets a list of filenames (2 letter country code for the filename) 
    /// from remote server or from local storage
    /// </summary>
    private async void InitializePOIsServerFilenamePicker()
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
                var list = new List<string>();
                foreach (var item in files)
                {
                    if (item == null) continue;
                    list.Add(FormatHelper.TranslateCountryName(Path.GetFileNameWithoutExtension(item)));
                }
                FilenameComparer.filenameSortOrder = FilenameComparer.SortOrder.asc;
                list.Sort(FilenameComparer.NameArray);
                this.serverfilenamepicker.ItemsSource = list;
            }
        }
        else if (serverlist.POIs.Count > 0)
        {
            FileListLocalAccess = false;
            var ff = new POIsFilesFetch();
            var list = new List<string>();
            foreach (var item in serverlist.POIs)
            {
                if (item == null) continue;
                list.Add(FormatHelper.TranslateCountryName(Path.GetFileNameWithoutExtension(item.Name)));
                
            }
            FilenameComparer.filenameSortOrder = FilenameComparer.SortOrder.asc;
            list.Sort(FilenameComparer.NameArray);
            this.serverfilenamepicker.ItemsSource = list;
        }        
    }
    /// <summary>
    /// <c>UseOfflineMaps_CheckedChanged</c>
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void UseOfflineMaps_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (e.Value)
        {
            this.DownloadMapsViaWiFi.IsEnabled = true;
            this.localmapfilenamepicker.IsEnabled = true;
            this.servermapfilenamepicker.IsEnabled = true;
            InitializeOfflineMapPickers();
        }
        else
        {
            this.DownloadMapsViaWiFi.IsEnabled = false;
            this.localmapfilenamepicker.IsEnabled = false;
            this.servermapfilenamepicker.IsEnabled = false;
            this.localmapfilenamepicker.SelectedIndex = -1;
            this.localmapfilenamepicker.Title = AppResource.ChooseText;
            this.servermapfilenamepicker.SelectedIndex = -1;
            this.servermapfilenamepicker.Title = AppResource.ChooseText;

            // Check to ensure that if the Use Offline maps is repeatedly unchecked/checked
            // then remove the OpenStreetMap layer and add it again, normally this is not needed
            // when actually downloading the offline map tiles as that function will remove the OSM layer
            foreach (var item in mapView.Map.Layers.ToList())
            {
                if (item.Name.Equals("OpenStreetMap"))
                    mapView.Map.Layers.Remove(item);
                if (item.Name.Equals("Layer"))
                    mapView.Map.Layers.Remove(item);
            }
            mapView.Map.Layers.Add(OpenStreetMap.CreateTileLayer());
            if (_myLocationLayer != null)
                mapView.Map.Layers.Add(_myLocationLayer);
            if (myRouteLayer != null)
                mapView.Map.Layers.Add(myRouteLayer);
        }
    }
    /// <summary>
    /// <c>UseOfflinePOIs_CheckedChanged</c>
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
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
    /// <c>ServerPOIsFilenamepicker_SelectedIndexChanged</c>
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ServerPOIsFilenamepicker_SelectedIndexChanged(object sender, EventArgs e)
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
    private async void InitializeOfflineMapPickers()
    {
        servermbtff.MbTileFiles.Clear();
        localmbtff.MbTileFiles.Clear();

        // Local storage
        var llist = new List<string>();
        string[] files = Directory.GetFiles(FileSystem.AppDataDirectory, "*.mbtiles");
        foreach (var item in files)
        {
            // Local files found
            FileListLocalAccess = true;
            if (item == null) continue;
            var mbf = new MbTileFile();
            // Get the bounding box
            var Idx = item.IndexOf("{");
            if (Idx > -1)
            {
                var Idx2 = item.IndexOf("}");
                if (Idx2 > -1)
                {
                    var ds = item[(Idx + 1)..(Idx2)].Split(',');
                    mbf.BBox[0] = double.Parse(ds[0]);
                    mbf.BBox[1] = double.Parse(ds[1]);
                    mbf.BBox[2] = double.Parse(ds[2]);
                    mbf.BBox[3] = double.Parse(ds[3]);
                }
            }
            // Get last update part
            //xx-yyyyMMdd
            Idx = item.IndexOf("}");
            if (Idx > -1)
            {
                var dts = item.Substring(Idx + 1, 8);
                mbf.LastUpdated = new DateTime(int.Parse(dts[..4]), int.Parse(dts[4..6]), int.Parse(dts[6..8]));
                Idx = item.IndexOf("{");
                if (Idx > -1)
                {
                    mbf.Name = item;
                    var name = FormatHelper.TranslateCountryName(Path.GetFileNameWithoutExtension(item[0..Idx]));
                    localmbtff.MbTileFiles.Add(mbf);
                    llist.Add(name);
                }
            }
        }
        if (llist.Count > 0)
        {
            FilenameComparer.filenameSortOrder = FilenameComparer.SortOrder.asc;
            llist.Sort(FilenameComparer.NameArray);
            this.localmapfilenamepicker.ItemsSource = llist;
        }
        // Try server
        var slist = new List<string>();
        var serverlist = await DriveHelper.DriveListFilesAsync();
        if (!serverlist.Error && serverlist.MbTileFiles.Count > 0)
        {
            FileListLocalAccess = false;
            foreach (var item in serverlist.MbTileFiles)
            {
                if (item == null) continue;
                var mbf = new MbTileFile();
                // Get the bounding box
                var Idx = item.Name.IndexOf("{");
                if (Idx > -1)
                {
                    var Idx2 = item.Name.IndexOf("}");
                    if (Idx2 > -1)
                    {
                        var ds = item.Name[(Idx + 1)..(Idx2)].Split(',');
                        mbf.BBox[0] = double.Parse(ds[0]);
                        mbf.BBox[1] = double.Parse(ds[1]);
                        mbf.BBox[2] = double.Parse(ds[2]);
                        mbf.BBox[3] = double.Parse(ds[3]);
                    }
                }
                // Get last update part
                //xx-yyyyMMdd
                Idx = item.Name.IndexOf("}");
                if (Idx > -1)
                {
                    var dts = item.Name.Substring(Idx + 1, 8);
                    mbf.LastUpdated = new DateTime(int.Parse(dts[..4]), int.Parse(dts[4..6]), int.Parse(dts[6..8]));
                    Idx = item.Name.IndexOf("{");
                    if (Idx > -1)
                    {
                        mbf.Name = item.Name;
                        var name = FormatHelper.TranslateCountryName(Path.GetFileNameWithoutExtension(item.Name)[0..Idx]);
                        servermbtff.MbTileFiles.Add(mbf);
                        slist.Add(name);
                    }
                }
            }
            FilenameComparer.filenameSortOrder = FilenameComparer.SortOrder.asc;
            slist.Sort(FilenameComparer.NameArray);
            this.servermapfilenamepicker.ItemsSource = slist;
        }
    }
}