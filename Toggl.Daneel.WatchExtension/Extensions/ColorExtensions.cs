using System;
using UIKit;

namespace Toggl.Daneel.WatchExtension.Extensions
{
    public static class ColorExtensions
    {
        public static UIColor ToUIColor(this string hex)
        {
            nfloat red = Convert.ToInt32(hex.Substring(1, 2), 16) / 255f;
            nfloat green = Convert.ToInt32(hex.Substring(3, 2), 16) / 255f;
            nfloat blue = Convert.ToInt32(hex.Substring(5, 2), 16) / 255f;
            return UIColor.FromRGB(red, green, blue);
        }
    }
}
