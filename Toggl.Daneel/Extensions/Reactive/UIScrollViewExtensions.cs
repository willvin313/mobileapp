using System;
using System.Reactive;
using System.Reactive.Linq;
using Toggl.Foundation.MvvmCross.Reactive;
using Toggl.Multivac.Extensions;
using UIKit;

namespace Toggl.Daneel.Extensions.Reactive
{
    public static class UIScrollViewExtensions
    {
        public static IObservable<Unit> DecelerationEnded(this IReactive<UIScrollView> reactive)
            => Observable
                .FromEventPattern(e => reactive.Base.DecelerationEnded += e, e => reactive.Base.DecelerationEnded -= e)
                .SelectUnit();
    }
}