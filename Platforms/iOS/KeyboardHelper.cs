using UIKit;

namespace POIViewerMap.Platforms;

public static partial class KeyboardHelper
{
    public static void HideKeyboard()
    {
        UIApplication.SharedApplication.KeyWindow.EndEditing(true);
    }
}
