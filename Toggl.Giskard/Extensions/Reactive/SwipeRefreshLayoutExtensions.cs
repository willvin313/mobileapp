using System;
using System.Reactive;
using System.Reactive.Linq;
using Android.Support.V4.Widget;
using Toggl.Foundation.UI.Reactive;
using Toggl.Multivac.Extensions;

namespace Toggl.Giskard.Extensions.Reactive
{
    public static class SwipeRefreshLayoutExtensions
    {
        public static IObservable<Unit> Refreshed(this IReactive<SwipeRefreshLayout> reactive)
            => Observable
                .FromEventPattern(e => reactive.Base.Refresh += e, e => reactive.Base.Refresh -= e)
                .SelectUnit();
    }
}
