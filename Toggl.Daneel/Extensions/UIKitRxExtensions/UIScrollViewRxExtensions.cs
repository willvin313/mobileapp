using System;
using System.Reactive.Linq;
using CoreGraphics;
using Toggl.Foundation.MvvmCross.Helper;
using UIKit;

namespace Toggl.Daneel.Extensions
{
    public static partial class UIKitRxExtensions
    {
        public static IObservable<int> CurrentPage(this UIScrollView scrollView)
            => Observable
            .FromEventPattern(e => scrollView.DecelerationEnded += e, e => scrollView.DecelerationEnded -= e)
            .Select(e => 
            {
                var view = (UIScrollView)e.Sender;
                var newPage = (int)(view.ContentOffset.X / view.Frame.Width);
                return newPage;
            })
            .DistinctUntilChanged();

        public static Action<int> BindCurrentPage(this UIScrollView scrollView) => page =>
        {
            var scrollPoint = new CGPoint(scrollView.Frame.Size.Width * page, 0);
            scrollView.SetContentOffset(scrollPoint, false);
        };
    }
}
