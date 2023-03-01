using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POIViewerMap.DataClasses;

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
