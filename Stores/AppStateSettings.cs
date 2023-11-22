using ReactiveUI.Fody.Helpers;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using POIBinaryFormatLib;

namespace POIViewerMap.Stores;

public interface IAppStateSettings
{
    int SearchRadius { get; set; }
    int ZoomLevel { get; set; }
    POIType POI { get; set; }
    string BINFilepath { get; set; }
    bool CenterMap { get; set; }
    string RouteFilepath { get; set; }
    DateTime? LastUpdated { get; set; }
}

public class AppStateSettings : ReactiveObject, IAppStateSettings
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
    public AppStateSettings()
    {
        this.WhenAnyValue(
                x => x.SearchRadius,
                x => x.ZoomLevel,
                x => x.POI,
                x => x.BINFilepath,
                x => x.RouteFilepath,
                x => x.CenterMap
            )
            .Subscribe(_ =>
                this.LastUpdated = DateTime.Now
            );
    }
    [Reactive] public int SearchRadius { get; set; } = 5;
    [Reactive] public int ZoomLevel { get; set; } = 0;
    [Reactive] public POIType POI { get; set; } = POIType.DrinkingWater;
    [Reactive] public bool CenterMap { get; set; } = false;
    [Reactive] public string BINFilepath { get; set; } = null;
    [Reactive] public string RouteFilepath { get; set; } = null;
    [Reactive] public DateTime? LastUpdated { get; set; }
}
