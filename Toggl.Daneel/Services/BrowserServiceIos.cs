using System;
using Foundation;
using Toggl.Foundation.UI.Services;
using UIKit;

namespace Toggl.Daneel.Services
{
    public sealed class BrowserServiceIos : IBrowserService
    {
        public void OpenStore()
        {
            OpenUrl("https://itunes.apple.com/us/app/toggl/id1291898086?mt=8");
        }

        public void OpenUrl(string url)
        {
            var nsUrl = new NSUrl(url);
            UIApplication.SharedApplication.OpenUrl(nsUrl);
        }
    }
}
