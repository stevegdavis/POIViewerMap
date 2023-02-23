using Mapsui;
using Mapsui.Extensions;
using Mapsui.Projections;
using Mapsui.Tiling;
using Mapsui.UI;
using MyMap = Mapsui.Map;

namespace POIViewerMap.Views;

public partial class MapViewPage : ContentPage
{
	public MapViewPage()
	{
		InitializeComponent();

        mapView.IsZoomButtonVisible = true;
        mapView.IsMyLocationButtonVisible = true;
        mapView.IsNorthingButtonVisible = true;


        var mapControl = new Mapsui.UI.Maui.MapControl();
        var map = new MyMap();
        map.Layers.Add(OpenStreetMap.CreateTileLayer());
        mapView.Map = map;
        var extent = GetLimitsOfStroud();
        map.Limiter.ZoomLimits = new MinMax(0.15, 153900);
        map.Home = n => n.NavigateTo(extent);
        mapControl.Navigator.CenterOn(SphericalMercator.FromLonLat(-2.2539759, 51.7476017).ToMPoint());
        map.RotationLock = true;
    }
    private MRect GetLimitsOfStroud()
    {
        var (minX, minY) = SphericalMercator.FromLonLat(-2.1488, 51.79797);
        var (maxX, maxY) = SphericalMercator.FromLonLat(-2.3434, 51.65957);
        return new MRect(minX, minY, maxX, maxY);
    }
}