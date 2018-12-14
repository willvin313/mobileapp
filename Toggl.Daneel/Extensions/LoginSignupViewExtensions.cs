using System;
using CoreGraphics;
using UIKit;

namespace Toggl.Daneel.Extensions
{
    public static class LoginSignupViewExtensions
    {
        public static void SetupShowPasswordButton(this UIButton button)
        {
            var image = UIImage
                .FromBundle("icPassword")
                .ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
            button.SetImage(image, UIControlState.Normal);
            button.TintColor = UIColor.Black;
        }
    }
}
