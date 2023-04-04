namespace POIViewerMap.Helpers;

public class ReadPOIs
{
    //public static async Task<List<POIData>> ReadAysnc(string filepath)
    //{
    //    List<POIData> pois = new();
    //    await Task.Factory.StartNew(delegate
    //    {
    //        using var sr = new StreamReader(filepath);
    //        while (!sr.EndOfStream)
    //        {
    //            var line = sr.ReadLine().Trim();
    //            if (line.StartsWith("[POI]"))
    //            {
    //                line = sr.ReadLine();
    //                var lat = Convert.ToDouble(line);
    //                line = sr.ReadLine();
    //                var lon = Convert.ToDouble(line);
    //                var title = sr.ReadLine();
    //                var subtitle = sr.ReadLine();
    //                var poi = sr.ReadLine();
    //                var data = new POIData
    //                {
    //                    Latitude = lat,
    //                    Longitude = lon,
    //                    POI = Enum.Parse<POIType>(poi),
    //                    Title = title,
    //                    Subtitle = subtitle,
    //                };
    //                pois.Add(data);
    //            }
    //        }
    //    });
    //    return pois;
    //}
    //private static POIData ParseLine(string line)
    //{
    //    double lat = 0, lon = 0;
    //    var Idx = line.IndexOf("<lat=");
    //    if (Idx != -1)
    //    {
    //        var Idx2 = line.IndexOf(">", Idx);
    //        lat = Convert.ToDouble(line.Substring(Idx + "<lat=".Length, Idx2 - (Idx + "<lat=".Length)));
    //    }
    //    Idx = line.IndexOf("<lon=");
    //    if (Idx != -1)
    //    {
    //        var Idx2 = line.IndexOf(">", Idx);
    //        lon = Convert.ToDouble(line.Substring(Idx + "<lon=".Length, Idx2 - (Idx + "<lon=".Length)));
    //    }
    //    var poi = new POIType();
    //    Idx = line.IndexOf("<type=");
    //    if (Idx > -1)
    //    {
    //        var Idx2 = line.IndexOf('>', Idx);
    //        if (Idx2 > -1)
    //        {
    //            var poiStr = line.Substring(Idx + "<type=".Length, Idx2 - (Idx + "<type=".Length));
    //            poi = Enum.Parse<POIType>(poiStr);
    //        }
    //    }
    //    var title = string.Empty;
    //    Idx = line.IndexOf("<Title=");
    //    if (Idx > -1)
    //    {
    //        var Idx2 = line.IndexOf('>', Idx);
    //        if (Idx2 > -1)
    //        {
    //            title = line.Substring(Idx + "<Title=".Length, Idx2 - (Idx + "<Title=".Length));
    //            title = title.Replace("&apos;", "'").Replace("&amp;", "&");
    //        }
    //    }
    //    var subtitle = string.Empty;
    //    Idx = line.IndexOf("<Subtitle=");
    //    if (Idx > -1)
    //    {
    //        var Idx2 = line.IndexOf('>', Idx);
    //        if (Idx2 > -1)
    //        {
    //            subtitle = line.Substring(Idx + "<Subtitle=".Length, Idx2 - (Idx + "<Subtitle=".Length));
    //        }
    //    }
    //    double time = 0;
    //    Idx = line.IndexOf("<time=");
    //    if (Idx > -1)
    //    {
    //        var Idx2 = line.IndexOf('>', Idx);
    //        if (Idx2 > -1)
    //        {
    //            time = Convert.ToDouble(line.Substring(Idx + "<time=".Length, Idx2 - (Idx + "<time=".Length)));
    //        }
    //    }
    //    return new POIData
    //    {
    //        Latitude = lat,
    //        Longitude = lon,
    //        POI = poi,
    //        Title = title,
    //        Subtitle = subtitle,
    //        Time = time,
    //    };
    //}
}
