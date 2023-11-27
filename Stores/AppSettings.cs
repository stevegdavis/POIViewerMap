using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace POIViewerMap.Stores;

public interface IAppSettings
{
    bool ShowDisclaimer { get; set; }
    DateTime? LastUpdated { get; set; }
}
public class AppSettings : ReactiveObject, IAppSettings
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
    public AppSettings()
    {
        GetAppSettings();
        this.WhenAnyValue(
                x => x.ShowDisclaimer
            ).Subscribe(_ =>
                UpdateAppSettings()
            );
        
    }
    [Reactive] public bool ShowDisclaimer { get; set; } = true;
    [Reactive] public DateTime? LastUpdated { get; set; }

    private void GetAppSettings()
    {
        if (Preferences.Default.ContainsKey("show_disclaimer"))
        {
            this.ShowDisclaimer = Preferences.Default.Get("show_disclaimer", true);
        }       
    }
    public void UpdateAppSettings()
    {
        Preferences.Default.Set("show_disclaimer", this.ShowDisclaimer);
        this.LastUpdated = DateTime.Now;
    }
}
