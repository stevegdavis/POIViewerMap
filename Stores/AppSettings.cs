using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace POIViewerMap.Stores;

public interface IAppSettings
{
    bool ShowPopupAtStart { get; set; }
    DateTime? LastUpdated { get; set; }
}
public class AppSettings : ReactiveObject, IAppSettings
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
    public AppSettings()
    {
        GetAppSettings();
        this.WhenAnyValue(
                x => x.ShowPopupAtStart
            ).Subscribe(_ =>
                UpdateAppSettings()
            );
        
    }
    [Reactive] public bool ShowPopupAtStart { get; set; } = true;
    [Reactive] public DateTime? LastUpdated { get; set; }

    private void GetAppSettings()
    {
        if (Preferences.Default.ContainsKey("show"))
        {
            this.ShowPopupAtStart = Preferences.Default.Get("show", true);
        }       
    }
    public void UpdateAppSettings()
    {
        Preferences.Default.Set("show", this.ShowPopupAtStart);
        this.LastUpdated = DateTime.Now;
    }
}
