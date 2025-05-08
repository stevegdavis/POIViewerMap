using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POIViewerMap.DataClasses;
/// <summary>
/// Class <c>FileFetch</c>
/// From poidata-server
/// </summary>
public class POIsFilesFetch
{
    public List<POIFile> POIs { get; set; } = new();
    public bool Error { get; set; } = false;
    public string ErrorMsg { get; set; }
}
public class POIFile
{
    public string Name { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}
/// <summary>
/// Class <c>MbFileFetch</c>
/// From Drive
/// </summary>
public class MbTileFilesFetch
{
    public List<MbTileFile> MbTileFiles { get; set; } = new();
    public bool Error { get; set; } = false;
    public string ErrorMsg { get; set; } = string.Empty;
}
/// <summary>
/// Class <c>MbTileFile</c>
/// Represents a file containing map tiles with metadata.
/// </summary>
public class MbTileFile
{
    public string Name { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
    public double[] BBox { get; set; } = [0, 0, 0, 0];
}
