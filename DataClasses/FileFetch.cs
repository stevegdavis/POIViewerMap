using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POIViewerMap.DataClasses;

public class FileFetch
{
    public List<string> Names { get; set; } = new();
    //public string Name { get; set; }
    public DateTime LastUpdated { get; set; }
    public bool Error { get; set; } = false;
    public string ErrorMsg { get; set; }
}
