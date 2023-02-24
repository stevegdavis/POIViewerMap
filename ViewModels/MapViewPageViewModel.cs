using CommunityToolkit.Mvvm.ComponentModel;
using Mapsui;
using Mapsui.Projections;
using Mapsui.UI.Maui;
using MyMap = Mapsui.Map;

namespace POIViewerMap.ViewModels;

public partial class MapViewPageViewModel : ObservableObject
{
    public MapViewPageViewModel()
    {
    }
    private MRect GetLimitsOfStroud()
    {
        var (minX, minY) = SphericalMercator.FromLonLat(-2.1488, 51.79797);
        var (maxX, maxY) = SphericalMercator.FromLonLat(-2.3434, 51.65957);
        return new MRect(minX, minY, maxX, maxY);
    }

    [ObservableProperty]
    MyMap map = new MyMap();
    
    //public MapControl MapControl { get; set; }
    //[Reactive] public MyMap map { get; set; } = new MyMap();
}
