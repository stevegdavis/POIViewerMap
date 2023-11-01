using POIViewerMap.Resources.Strings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POIViewerMap.Helpers;

public class Format
{
    public static string FormatDistance(double distance)
    {
        var distanceStr = $" {String.Format("{0:0.00}", distance)}km";
        if (distance < 1)
            distanceStr = $" {String.Format("{0:0}", distance * 1000)} {AppResource.Meters}";
        return distanceStr;
    }
}
