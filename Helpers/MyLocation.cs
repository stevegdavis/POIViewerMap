using POIBinaryFormatLib;
using POIViewerMap.DataClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POIViewerMap.Helpers;

public class MyLocation
{
    public static double Calculate(POIData poi, Location mylocation) =>
        //Location start = new(poi.Latitude, poi.Longitude);
        //GeolocationRequest request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10));
        //var _cancelTokenSource = new CancellationTokenSource();
        //Location location = await Geolocation.Default.GetLocationAsync(request, _cancelTokenSource.Token);
        Location.CalculateDistance(poi.Latitude, poi.Longitude, mylocation, DistanceUnits.Kilometers);
}
