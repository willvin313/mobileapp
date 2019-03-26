using System;
using Android.Views;
using Toggl.Foundation.MvvmCross.Reactive;

namespace Toggl.Giskard.Extensions.Reactive
{
    public static class LayoutParamsExtensions
    {
        public static Action<int> MarginTop(this IReactive<ViewGroup.LayoutParams> reactive)
            => margin =>
            {
                var marginParams = reactive.Base as ViewGroup.MarginLayoutParams;
                if (marginParams == null) return;

                marginParams.TopMargin = margin;
            };
    }
}
