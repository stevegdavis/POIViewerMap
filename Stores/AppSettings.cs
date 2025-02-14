using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace POIViewerMap.Stores;
/// <summary>
/// Interface
/// </summary>
public interface IAppSettings
{
    bool ShowPopupAtStart { get; set; }
    DateTime? LastUpdated { get; set; }
}
/// <summary>
/// Class <c>AppSettings</c>
/// </summary>
public class AppSettings : ReactiveObject, IAppSettings
{
    /// <summary>
    /// <c>Contructor</c>
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
    public AppSettings()
    {
        //Preferences.Default.Clear();
        GetAppSettings();
        this.WhenAnyValue(
                x => x.ShowPopupAtStart
            ).Subscribe(_ =>
                UpdateAppSettings()
            );
        
    }
    [Reactive] public bool ShowPopupAtStart { get; set; } = true;
    [Reactive] public DateTime? LastUpdated { get; set; }
    /// <summary>
    /// <c>GetAppSettings</c>
    /// Gets app settings from App Preferences
    /// </summary>
    private void GetAppSettings()
    {
        if (Preferences.Default.ContainsKey("show"))
        {
            this.ShowPopupAtStart = Preferences.Default.Get("show", true);
        }
        else
            this.ShowPopupAtStart = true;
    }
    public void UpdateAppSettings()
    {
        Preferences.Default.Set("show", this.ShowPopupAtStart);
        this.LastUpdated = DateTime.Now;
    }
}
