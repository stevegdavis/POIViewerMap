using POIBinaryFormatLib;
using POIViewerMap.Helpers;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace POIViewerMap.Stores;

public interface IAppStateSettings
{
    int SearchRadius { get; set; }
    int ZoomLevel { get; set; }
    int POI { get; set; } // 0 - 9
    string BINFilepath { get; set; }
    bool CenterMap { get; set; }
    bool RestoreOptions { get; set; }
    string RouteFilepath { get; set; }
    bool ShowSearchRadiusOnMap { get; set; }
    DateTime? LastUpdated { get; set; }
}
public class AppStateSettings : ReactiveObject, IAppStateSettings
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
    public AppStateSettings()
    {
        GetAppSettings();
        this.WhenAnyValue(
                x => x.SearchRadius,
                x => x.ZoomLevel,
                x => x.POI,
                x => x.BINFilepath,
                x => x.RouteFilepath,
                x => x.CenterMap,
                x => x.RestoreOptions
            ).Subscribe(_ =>
                UpdateAppStateSettings()
            );
        this.WhenAnyValue(
                x => x.ShowSearchRadiusOnMap
            ).Subscribe(_ =>
                UpdateAppStateSettings()
            );
    }
    [Reactive] public int SearchRadius { get; set; } = 5;
    [Reactive] public int ZoomLevel { get; set; } = 0;
    [Reactive] public int POI { get; set; } = 0; // DrinkingWater;
    [Reactive] public bool CenterMap { get; set; } = false;
    [Reactive] public bool RestoreOptions { get; set; } = false;
    [Reactive] public string BINFilepath { get; set; } = null;
    [Reactive] public string RouteFilepath { get; set; } = null;
    [Reactive] public bool ShowSearchRadiusOnMap { get; set; } = false;
    [Reactive] public DateTime? LastUpdated { get; set; }

    private void GetAppSettings()
    {
        if (Preferences.Default.ContainsKey("search_radius"))
        {
            this.SearchRadius = Preferences.Default.Get("search_radius", 5);
        }
        if (Preferences.Default.ContainsKey("poi_type"))
        {
            this.POI = (int)Preferences.Default.Get("poi_type", (int)FormatHelper.GetSelectedIndexFromPOIType(POIType.DrinkingWater));
        }
        if (Preferences.Default.ContainsKey("bin_filepath"))
        {
            this.BINFilepath = Preferences.Default.Get("bin_filepath", "");
        }
        if (Preferences.Default.ContainsKey("route_filepath"))
        {
            this.RouteFilepath = Preferences.Default.Get("route_filepath", "");
        }
        if (Preferences.Default.ContainsKey("zoom_level"))
        {
            this.ZoomLevel = Preferences.Default.Get("zoom_level", 5);
        }
        if (Preferences.Default.ContainsKey("center_map"))
        {
            this.CenterMap = Preferences.Default.Get("center_map", false);
        }
        if (Preferences.Default.ContainsKey("restore_options"))
        {
            this.RestoreOptions = Preferences.Default.Get("restore_options", false);
        }
        if (Preferences.Default.ContainsKey("show_search"))
        {
            this.ShowSearchRadiusOnMap = Preferences.Default.Get("show_search", false);
        }
    }
    public void UpdateAppStateSettings()
    {
        Preferences.Default.Set("search_radius", this.SearchRadius);
        Preferences.Default.Set("poi_type", this.POI);
        Preferences.Default.Set("bin_filepath", this.BINFilepath);
        Preferences.Default.Set("route_filepath", this.RouteFilepath);
        Preferences.Default.Set("center_map", this.CenterMap);
        Preferences.Default.Set("restore_options", this.RestoreOptions);
        Preferences.Default.Set("zoom_level", this.ZoomLevel);
        Preferences.Default.Set("show_search", this.ShowSearchRadiusOnMap);
        this.LastUpdated = DateTime.Now;
    }
}
