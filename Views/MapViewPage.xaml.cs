using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Storage;
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
using Mapsui.Widgets.ButtonWidget;
using Mapsui.Widgets.ScaleBar;
using Microsoft.Maui.ApplicationModel;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using POIBinaryFormatLib;
using POIViewerMap.DataClasses;
using POIViewerMap.Helpers;
using POIViewerMap.Popups;
using POIViewerMap.Resources.Strings;
using POIViewerMap.Stores;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using The49.Maui.BottomSheet;
using static Microsoft.Maui.ApplicationModel.Permissions;
using Color = Microsoft.Maui.Graphics.Color;
using Location = Microsoft.Maui.Devices.Sensors.Location;

namespace POIViewerMap.Views;

public partial class MapViewPage : ContentPage
{
    private string FullFilepathPOIs;
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
    private static FileSaverResult filesaverresult;

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
        this.picker.ItemsSource = items;
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
        mapView.Map.Widgets.Add(new ScaleBarWidget(mapView.Map) { TextAlignment = Alignment.Center });
        mapView.PinClicked += OnPinClicked;
        mapView.MapClicked += OnMapClicked;

        ToggleCompass();
        var clickMeButton = CreateButton("Options", Mapsui.Widgets.VerticalAlignment.Bottom, Mapsui.Widgets.HorizontalAlignment.Right);
        clickMeButton.WidgetTouched += (s, a) =>
        {
            //((ButtonWidget?)s!).Text = $"Clicked {++clickCount} times";
            var options = new Options();
            options.HasHandle = true;
            options.ShowAsync();
            mapView.Map.RefreshGraphics();
        };
        //mapView.Map.Widgets.Add(clickMeButton);
        var imagebtn = CreateButtonWithImage(Mapsui.Widgets.VerticalAlignment.Top, Mapsui.Widgets.HorizontalAlignment.Right);
        imagebtn.WidgetTouched += (s, a) =>
        {
            var options = new Options();
            options.HasHandle = true;
            options.ShowAsync();
            mapView.Map.RefreshGraphics();
        };
        mapView.Map.Widgets.Add(imagebtn);
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
    private async void Popup_Closed(object sender, PopupClosedEventArgs e)
    {
        if(sender is AppUsagePopup)
            appSettings.ShowPopupAtStart = (bool)e.Result;
        else if(sender is FileListPopup)
        {
            if (FileListPopup.SelectedFilename == null || FileListPopup.IsCancelled)
            {
                this.activityloadindicatorlayout.IsVisible = false;
                return; // No file or Cancelled
            }
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
                pois = await POIBinaryFormat.ReadAsync(Path.Combine(FileSystem.AppDataDirectory, FileListPopup.SelectedFilename.ToLower()));
                await PopulateMapAsync(pois);
            }
            else 
            {
                var webhelper = new WebHelper();
                await webhelper.DownloadPOIFileAsync(FileListPopup.SelectedFilename);
                if (File.Exists(WebHelper.localPath))
                {
                    pois = await POIBinaryFormat.ReadAsync(WebHelper.localPath);
                    await PopulateMapAsync(pois);
                }
            }
            this.picker.IsEnabled = true;
            this.pickerRadius.IsEnabled = true;
            this.activityloadindicatorlayout.IsVisible = false;
        }
    }
    private static ButtonWidget CreateButton(string text,
        Mapsui.Widgets.VerticalAlignment verticalAlignment, Mapsui.Widgets.HorizontalAlignment horizontalAlignment)
    {
        return new ButtonWidget()
        {
            Text = text,
            VerticalAlignment = verticalAlignment,
            HorizontalAlignment = horizontalAlignment,
            MarginX = 10,
            MarginY = 30,
            PaddingX = 18,
            PaddingY = 18,
            CornerRadius = 8,
            Width = 100,
            BackColor = new Mapsui.Styles.Color(36, 143, 143),
            TextColor = Mapsui.Styles.Color.White,
        };
    }
    private static ButtonWidget CreateButtonWithImage(
        Mapsui.Widgets.VerticalAlignment verticalAlignment, Mapsui.Widgets.HorizontalAlignment horizontalAlignment)
    {
        return new ButtonWidget()
        {
            Text = "hi", // This text is apparently needed to update to position of the button
            SvgImage = AppIconHelper.optionsStr,
            VerticalAlignment = verticalAlignment,
            HorizontalAlignment = horizontalAlignment,
            MarginX = 25,
            MarginY = 160,
            PaddingX = 10,
            PaddingY = 8,
            CornerRadius = 8,
            Envelope = new MRect(0, 0, 64, 64)
        };
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
    async void BrowseButton_Clicked(object sender, EventArgs e)
    {
        if (POIsReadIsBusy)
            return;
        POIsReadIsBusy = true;
        this.activityloadindicatorlayout.IsVisible = true;
        
        FileListPopup popup = new FileListPopup();
        // Online download from server (when connected)
        var webhelper = new WebHelper();
        var parameters = new Dictionary<string, string>
        {
            { WebHelper.PARAM_ACTION, WebHelper.ACTION_FILES },
            { WebHelper.PARAM_FILE_NAME, "Show All" }
        };
        var serverlist = await webhelper.FilenamesFetchAsync(parameters);
        if(serverlist.Error)
        {
            await Toast.Make($"{serverlist.ErrorMsg}").Show();
        }
        if(serverlist.Error)
        {
            // Try local storage
            string[] files = Directory.GetFiles(FileSystem.AppDataDirectory, "*.bin");
            if (files.Length > 0)
            {
                // Local files found
                FileListLocalAccess = true;
                var ff = new FileFetch();
                foreach (var item in files)
                {
                    ff.Names.Add(Path.GetFileName(item).ToLower());
                }
                ff.LastUpdated = new DateTime();
                FilenameComparer.filenameSortOrder = FilenameComparer.SortOrder.asc;
                ff.Names.Sort(FilenameComparer.NameArray);
                popup.AddList(ff);
            }
        }
        else if (serverlist.Names.Count > 0)
        {
            FileListLocalAccess = false;
            popup.AddList(serverlist);
        }
        popup.Closed += Popup_Closed;
        this.ShowPopup(popup);
        POIsReadIsBusy = false;
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
            if (!String.IsNullOrEmpty(this.FilepathRouteLabel.Text))
            {
                if (myRouteLayer != null)
                    mapView.Map.Layers.Remove(myRouteLayer);
                this.activityloadindicatorlayout.IsVisible = true;
                this.RouteFilename.IsVisible = true;
                var list = await ImportRoutes.ImportGPXRouteAsync(this.FullFilepathRoute);
                var sb = new StringBuilder("LINESTRING(");
                foreach (var data in list)
                {
                    sb.Append($"{data.Latitude} {data.Longitude},");
                }
                sb.Append(")");
                myRouteLayer = CreateLineStringLayer(sb.ToString().Replace(",)", ")"), CreateLineStringStyle());
                mapView.Map.Layers.Add(myRouteLayer);
                this.activityloadindicatorlayout.IsVisible = false;
            }
        }
        catch(Exception ex) 
        {
            ShowRouteLoadFailToastMessage(this.FilepathRouteLabel.Text);
            this.FilepathRouteLabel.Text = string.Empty;
        }
        finally { this.activityloadindicatorlayout.IsVisible = false; }
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
    private async Task BrowseLocalPOIs()
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
        var result = await PickAndShow(options);//, "route");
        if (result != null && Path.GetExtension(result.FileName).Equals(".bin"))
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
                    var space = string.Empty;
                    if (!String.IsNullOrEmpty(poi.Subtitle))
                    {
                        space = "\r";
                    }
                    var myPin = new Pin(mapView)
                    {
                        Position = new Mapsui.UI.Maui.Position(poi.Latitude, poi.Longitude),
                        Type = PinType.Svg,
                        Label = $"{FormatHelper.GetTitleLang(poi, poi.Title.Contains(':'))}\r{FormatHelper.GetSubTitleLang(poi.Subtitle)}{space}{AppResource.PinLabelDistanceText}",
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
    private void DeleteButton_Clicked(object sender, EventArgs e)
    {
        //this.OptionsRouteDeleteButton.IsVisible = false;
        if(myRouteLayer!= null)
        {
            this.FilepathRouteLabel.Text = string.Empty;
            mapView.Map.Layers.Remove(myRouteLayer);
        }        
    }

    private void OptionsButton_Clicked(object sender, EventArgs e)
    {
        BottomSheet bottomsheet = new Options();
        bottomsheet.HasHandle = true;
        bottomsheet.ShowAsync();
    }
}