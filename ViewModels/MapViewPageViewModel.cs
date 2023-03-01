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
        this.Browse = ReactiveCommand.CreateFromTask(
                async (_) =>
                {
                    await BrowsePOIs(POIName);
                });
    }

    private async Task BrowsePOIs(string POIName)
    {
        var customFileType = new FilePickerFileType(
             new Dictionary<DevicePlatform, IEnumerable<string>>
             {
                    { DevicePlatform.iOS, new[] { "public.my.osm.extension" } }, // UTType values
                    { DevicePlatform.Android, new[] { "text/plain" } }, // MIME type
                    { DevicePlatform.WinUI, new[] { ".txt" } }, // file extension
                    { DevicePlatform.Tizen, new[] { "*/*" } },
                    { DevicePlatform.macOS, new[] { "txt" } }, // UTType values
             });

        PickOptions options = new()
        {
            PickerTitle = "Please select a TXT file",
            FileTypes = customFileType,
        };
        await PickAndShow(options);
    }
    public async Task<FileResult> PickAndShow(PickOptions options)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(options);
            if (result != null)
            {
                this.Filepath = result.FileName;
            }
            return result;
        }
        catch (Exception ex)
        {
            // The user canceled or something went wrong
        }
        return null;
    }
    private MRect GetLimitsOfStroud()
    {
        var (minX, minY) = SphericalMercator.FromLonLat(-2.1488, 51.79797);
        var (maxX, maxY) = SphericalMercator.FromLonLat(-2.3434, 51.65957);
        return new MRect(minX, minY, maxX, maxY);
    }

    public IReactiveCommand Browse { get; set; }

    [ObservableProperty]
    MyMap map = new MyMap();

    [ObservableProperty]
    private string? name = "Drinking Water";
    [ObservableProperty]
    private string?path = "...";

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
    public string? Filepath
    {
        get => path;
        set
        {
            if (!EqualityComparer<string?>.Default.Equals(name, value))
            {
                OnPathChanging(value);
                OnPropertyChanging();
                path = value;
                OnPathChanged(value);
                OnPropertyChanged();
            }
        }
    }
    partial void OnPathChanging(string? value)
    {
    }

    partial void OnPathChanged(string? value)
    {
    }
    [Reactive] public IList<string> POITypes { get; set; } = new List<string>(); // Picker list source
    //[ObservableProperty] public string Filepath;// { get; set; }
}
//public class POIData
//{
//    public double Latitude { get; set; }
//    public double Longitude { get; set; }
//    public double Time { get; set; }
//    public string Title { get; set; }
//    public string Subtitle { get; set; }
//    public POIType POI { get; set; }
//}
//[Flags]
//public enum POIType
//{
//    Unknown = 0,
//    DrinkingWater,
//    Campsite,
//    Bench,
//    BicycleShop,
//    BicycleRepairStation,
//    Supermarket,
//}
