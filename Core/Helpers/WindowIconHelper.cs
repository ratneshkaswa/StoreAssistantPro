using System.Windows;
using System.Windows.Media.Imaging;

namespace StoreAssistantPro.Core.Helpers;

public static class WindowIconHelper
{
    private static BitmapFrame? _cachedIcon;
    private static bool _iconLoaded;

    public static void Apply(Window window)
    {
        if (!_iconLoaded)
        {
            _iconLoaded = true;
            try
            {
                var uri = new Uri("pack://application:,,,/Assets/app.ico", UriKind.Absolute);
                var info = Application.GetResourceStream(uri);
                if (info is not null)
                {
                    using var stream = info.Stream;
                    _cachedIcon = BitmapFrame.Create(
                        stream,
                        BitmapCreateOptions.None,
                        BitmapCacheOption.OnLoad);
                }
            }
            catch
            {
                // Leave icon unset when the packaged asset cannot be loaded.
            }
        }

        if (_cachedIcon is not null)
            window.Icon = _cachedIcon;
    }
}
