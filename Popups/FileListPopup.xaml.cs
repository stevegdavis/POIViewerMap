using CommunityToolkit.Maui.Views;
using POIViewerMap.DataClasses;
using POIViewerMap.Helpers;

namespace POIViewerMap.Popups;

public partial class FileListPopup : Popup
{
    public static string SelectedFilename { get; set; }
    public static bool IsCancelled { get; set; } = false;
    public FileListPopup()
    {
        InitializeComponent();
    }
    public void AddList(List<FileFetch> list)
    {
        this.POIServerFileDownloadButton.IsEnabled = false;
        List<string> files = new List<string>();
        foreach (var item in list)
        {
            if (item.Name == null) continue;
            if (!Char.IsUpper(item.Name[0]))
            {
                var uC = Char.ToUpper(item.Name[0]);
                files.Add(Path.GetFileNameWithoutExtension($"{uC}{item.Name.Substring(1)}"));
            }
            else
                files.Add(Path.GetFileNameWithoutExtension(item.Name));
        }
        this.serverfilenamepicker.ItemsSource = files;

    }
    private void serverpicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        SelectedFilename = $"{serverfilenamepicker.SelectedItem.ToString()}.bin";
        this.POIServerFileDownloadButton.IsEnabled = true;
    }
    void Button_ClickedServer(object? sender, EventArgs e)
    {
        IsCancelled = false;
        CloseAsync(SelectedFilename);
    }

    void POIFileCancelButton_Clicked(object? sender, EventArgs e)
    {
        IsCancelled = true;
        CloseAsync(null);
    }   
}