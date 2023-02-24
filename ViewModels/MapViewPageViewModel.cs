using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using Mapsui;
using Mapsui.Projections;
using Mapsui.UI.Maui;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using MyMap = Mapsui.Map;

namespace POIViewerMap.ViewModels;

public partial class MapViewPageViewModel : ObservableObject
{
    public MapViewPageViewModel()
    {
        // Populate POIs picker
        POITypes.Add("Drinking Water");
        POITypes.Add("Campsite");
        POITypes.Add("Bench");
        POITypes.Add("Bicycle Shop");
        POITypes.Add("Bicycle Repair Station");
        POITypes.Add("Supermarket");
        POITypes.Add("Show All");
    }
    private MRect GetLimitsOfStroud()
    {
        var (minX, minY) = SphericalMercator.FromLonLat(-2.1488, 51.79797);
        var (maxX, maxY) = SphericalMercator.FromLonLat(-2.3434, 51.65957);
        return new MRect(minX, minY, maxX, maxY);
    }

    public int SelectedIndex { get; set; }

    [ObservableProperty]
    MyMap map = new MyMap();

    [ObservableProperty]
    private string? name = "Drinking Water";
    
    public string? POIName
    {
        get => name;
        set
        {
            if (!EqualityComparer<string?>.Default.Equals(name, value))
            {
                OnNameChanging(value);
                OnPropertyChanging();
                name = value;
                OnNameChanged(value);
                OnPropertyChanged();
            }
        }
    }
    partial void OnNameChanging(string? value)
    {
    }

    partial void OnNameChanged(string? value)
    {
    }
    [Reactive] public IList<string> POITypes { get; set; } = new List<string>(); // Picker list source

}
public class POIData
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Time { get; set; }
    public string Title { get; set; }
    public string Subtitle { get; set; }
    public POIType POI { get; set; }
}
[Flags]
public enum POIType
{
    Unknown = 0,
    DrinkingWater,
    Campsite,
    Bench,
    BicycleShop,
    BicycleRepairStation,
    Supermarket,
}
