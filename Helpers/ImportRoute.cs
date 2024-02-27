using DynamicData;
using Microsoft.Maui.LifecycleEvents;
using POIViewerMap.DataClasses;
using System.Text;

namespace POIViewerMap.Helpers;

public class ImportRoutes
{
    private List<RouteLineData> list = new();
    public static async Task<List<RouteLineData>> ImportGPXRouteAsync(string path)
    {
        List<RouteLineData> list = new();
        await Task.Factory.StartNew(() =>
        {
            // Parse GPX file
            using var sr = new StreamReader(path);
            while (!sr.EndOfStream)
            {
                var line = string.Empty;
                for (; ; )
                {
                    char C = (char)sr.Read();
                    line += C;
                    if (line.Contains("</trkpt>") || line.Contains("/>") || line.Contains("</rtept") || line.Contains("</wpt>") || sr.EndOfStream)
                    {
                        break;
                    }
                }
                var Idx = line.IndexOf("<trkpt");
                if (Idx > -1)
                {
                    var result = ParseLatLonLine(line);
                    list.Add(result.Item2);
                }
                Idx = line.IndexOf("<rtept");
                if (Idx > -1)
                {
                    var result = ParseLatLonLine(line);
                    list.Add(result.Item2);
                }
            }
        });
        return list;
    }
    public static async Task<string> ImportKMLRouteAsync(string path)
    {
        var result = string.Empty;
        await Task.Factory.StartNew(delegate
        {
        });
        return result;
    }
    private static (int, RouteLineData) ParseLatLonLine(string latlon_line)
    {
        string lat = string.Empty, lon = string.Empty;
        var Idx = latlon_line.IndexOf("lat=\"");
        if (Idx != -1)
        {
            var Idx2 = latlon_line.IndexOf(" lon", Idx);
            lat = latlon_line.Substring(Idx + "lat=\"".Length, Idx2 - (Idx + "lat=\"".Length) - 1);
        }
        Idx = latlon_line.IndexOf("lon=\"");
        if (Idx != -1)
        {
            var Idx2 = latlon_line.IndexOf(" />", Idx);
            if(Idx2 > -1)
            {
                lon = latlon_line.Substring(Idx + "lon=\"".Length, Idx2 - (Idx + "lon=\"".Length + 1));
            }
            else
            {
                Idx2 = latlon_line.IndexOf(">", Idx);
                if (Idx2 > -1)
                {
                    lon = latlon_line.Substring(Idx + "lon=\"".Length, Idx2 - (Idx + "lon=\"".Length + 1));
                }
            }
            var data = new RouteLineData
            {
                Latitude = lat.TrimEnd(),
                Longitude = lon.TrimEnd(),
            };
            return (Idx2, data);
        }
        return (0, new RouteLineData());
    }
}