using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Views;
using DynamicData;
using POIViewerMap.DataClasses;
using POIViewerMap.Helpers;
using System.Collections.Generic;
using The49.Maui.BottomSheet;

namespace POIViewerMap;

public partial class Options : BottomSheet
{
    public static string SelectedFilename { get; set; }
    public static bool IsCancelled { get; set; } = false;
    public Options()
	{
		InitializeComponent();
        InitializePicker();
    }

    private async void InitializePicker()
    {
        var webhelper = new WebHelper();
        var parameters = new Dictionary<string, string>
        {
            { WebHelper.PARAM_ACTION, WebHelper.ACTION_FILES },
            { WebHelper.PARAM_FILE_NAME, "Show All" }
        };
        var serverlist = await webhelper.FilenamesFetchAsync(parameters);
        if (serverlist.Error)
        {
            await Toast.Make($"{serverlist.ErrorMsg}").Show();
        }
        if (serverlist.Error)
        {
            // Try local storage
            string[] files = Directory.GetFiles(FileSystem.AppDataDirectory, "*.bin");
            if (files.Length > 0)
            {
                // Local files found
                //FileListLocalAccess = true;
                var ff = new FileFetch();
                foreach (var item in files)
                {
                    if (item == null) continue;
                    var filename = Path.GetFileNameWithoutExtension(item);
                    if (!Char.IsUpper(filename[0]))
                    {
                        var uC = Char.ToUpper(filename[0]);
                        ff.Names.Add(($"{uC}{filename.Substring(1)}"));
                    }
                    else
                        ff.Names.Add(Path.GetFileNameWithoutExtension(filename));
                }
                ff.LastUpdated = new DateTime();
                FilenameComparer.filenameSortOrder = FilenameComparer.SortOrder.asc;
                ff.Names.Sort(FilenameComparer.NameArray);
                this.serverfilenamepicker.ItemsSource = ff.Names;
            }
        }
        else if (serverlist.Names.Count > 0)
        {
            //FileListLocalAccess = false;
            List<string> files = new List<string>();
            foreach (var item in serverlist.Names)
            {
                if (item == null) continue;
                if (!Char.IsUpper(item[0]))
                {
                    var uC = Char.ToUpper(item[0]);
                    files.Add(Path.GetFileNameWithoutExtension($"{uC}{item.Substring(1)}"));
                }
                else
                    files.Add(Path.GetFileNameWithoutExtension(item));
            }
            this.serverfilenamepicker.ItemsSource = files;
        }
        //else
        //{
        //    activityloadindicatorlayout.IsVisible = true;
        //    this.POIServerFileDownloadButton.IsEnabled = true;
        //    List<string> files = new List<string>();
        //    foreach (var item in serverlist.Names)
        //    {
        //        if (item == null) continue;
        //        if (!Char.IsUpper(item[0]))
        //        {
        //            var uC = Char.ToUpper(item[0]);
        //            files.Add(Path.GetFileNameWithoutExtension($"{uC}{item.Substring(1)}"));
        //        }
        //        else
        //            files.Add(Path.GetFileNameWithoutExtension(item));
        //    }
        //    this.serverfilenamepicker.ItemsSource = files;
        //    activityloadindicatorlayout.IsVisible = false;
        //    //this.POIServerFileDownloadButton.IsEnabled = false;
        //}
    }

    private void serverfilenamepicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        SelectedFilename = $"{serverfilenamepicker.SelectedItem.ToString()}.bin";
        this.POIServerFileDownloadButton.IsEnabled = true;
    }

    private void POIServerFileDownloadButton_Clicked(object sender, EventArgs e)
    {

    }
}