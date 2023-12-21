using POIViewerMap.DataClasses;
using System.Text;

namespace POIViewerMap.Helpers;

public class ImportRoutes
{
    private List<RouteLineData> list = new();
    public static async Task<string> ImportGPXRouteAsync(string path)
    {
        List<RouteLineData> list = new();
        StringBuilder sb = new();
        await Task.Factory.StartNew(() =>
        {
            // Parse GPX file
            using var sr = new StreamReader(path);
            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();
                if(line.EndsWith("</gpx>"))
                {
                    var Idx = 0;
                    for (; ; )
                    {
                        if(Idx >= line.Length)
                        { 
                            break;
                        }
                        Idx = line.IndexOf("<trkseg>", Idx);
                        //<trkseg><trkpt lat="56.296887" lon="21.538749"></trkpt></trkseg>
                        if (Idx > -1)
                        {
                            var Idx2 = line.IndexOf("</trkseg>", Idx);
                            if (Idx2 > -1)
                            {
                                var trkseg = line.Substring(Idx + "<trkseg>".Length, Idx2 - Idx);
                                for (; ; )
                                {
                                    var Idx3 = trkseg.IndexOf("</trkpt>");
                                    if (Idx3 > -1)
                                    {
                                        var trkpt = trkseg.Substring(0, Idx3);
                                        if (trkpt.Contains("<trkpt lat="))
                                        {
                                            var result = ParseLatLonLine(trkpt);
                                            list.Add(result.Item2);
                                            trkseg = trkseg.Substring((result.Item1 + 2) + "<trkpt>".Length);
                                        }//<trkpt lat="56.296104" lon="21.538877"></trkpt>
                                    }
                                    else
                                    {
                                        Idx += trkseg.Length;
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                else
                {
                    if (line.Contains("<trkpt lat="))
                    {
                        var result = ParseLatLonLine(line);
                        list.Add(result.Item2);
                    }
                }
            }
            sb.Append("LINESTRING(");
            foreach (var data in list)
            {
                sb.Append($"{data.Latitude} {data.Longitude},");
            }
            sb.Append(")");
        });
        return sb.ToString().Replace(",)", ")");
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