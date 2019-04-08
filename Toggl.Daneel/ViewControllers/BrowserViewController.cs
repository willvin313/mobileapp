using CoreGraphics;
using Foundation;
using MvvmCross.Platforms.Ios.Views;
using MvvmCross.Platforms.Ios.Presenters.Attributes;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Foundation;
using Toggl.Foundation.UI.Helper;
using Toggl.Foundation.UI.ViewModels;
using UIKit;
using WebKit;

namespace Toggl.Daneel.ViewControllers
{
    [MvxChildPresentation]
    public sealed class BrowserViewController : MvxViewController<BrowserViewModel>, IWKNavigationDelegate
    {
        private const int distanceFromTopForIos10 = 3;

        private WKWebView webView;

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            Title = Resources.Loading;

            setNavBar();
            buildUI();

            webView.LoadRequest(new NSUrlRequest(new NSUrl(ViewModel.Url)));
        }

        private void buildUI()
        {
            // This is done via code because of a bug in XCode that does not allow WKWebViews in IB files.
            // This needs to be turned into an .xib file after the bug is fixed.
            var backgroundView = new UIView(new CGRect(0, 0, View.Frame.Width, View.Frame.Height))
            {
                BackgroundColor = Color.NavigationBar.BackgroundColor.ToNativeColor()
            };

            webView = new WKWebView(CGRect.Empty, new WKWebViewConfiguration());
            webView.TranslatesAutoresizingMaskIntoConstraints = false;

            webView.NavigationDelegate = this;

            View.Add(backgroundView);
            View.Add(webView);

            if (UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
                webView.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor).Active = true;
            else
                webView.TopAnchor.ConstraintEqualTo(View.TopAnchor, distanceFromTopForIos10).Active = true;

            webView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor).Active = true;
            webView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor).Active = true;
            webView.BottomAnchor.ConstraintEqualTo(View.BottomAnchor).Active = true;
        }

        private void setNavBar()
        {
            var tintColor = Color.NavigationBar.BackButton.ToNativeColor();
            NavigationController.NavigationBar.TintColor = tintColor;
            NavigationController.NavigationBar.BarStyle = UIBarStyle.Default;
            NavigationController.NavigationBar.TitleTextAttributes = new UIStringAttributes
            {
                ForegroundColor = tintColor
            };
        }

        [Export("webView:didFinishNavigation:")]
        public void OnNavigationFinished(WKWebView webView, WKNavigation navigation)
        {
            Title = ViewModel.Title;
        }
    }
}

