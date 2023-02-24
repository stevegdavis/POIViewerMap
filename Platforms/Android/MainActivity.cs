using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using CommunityToolkit.Mvvm.Messaging;
using POIViewerMap.Views;

namespace POIViewerMap;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        WeakReferenceMessenger.Default.Register<FullScreenMessage>(this, (r, m) =>
        {
            IWindowInsetsController wicController = Window.InsetsController;
            Window.SetDecorFitsSystemWindows(false);
            Window.SetFlags(WindowManagerFlags.Fullscreen, WindowManagerFlags.Fullscreen);

            if (wicController != null)
            {
                wicController.Hide(WindowInsets.Type.NavigationBars());
            }
        });
        WeakReferenceMessenger.Default.Register<NormalScreenMessage>(this, (r, m) =>
        {
            IWindowInsetsController wicController = Window.InsetsController;
            Window.SetDecorFitsSystemWindows(true);
            Window.ClearFlags(WindowManagerFlags.Fullscreen);
            if (wicController != null)
            {
                wicController.Show(WindowInsets.Type.NavigationBars());
            }
        });
    }
}
