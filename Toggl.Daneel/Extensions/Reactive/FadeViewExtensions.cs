using System;
using Toggl.Daneel.Views;
using Toggl.Foundation.UI.Reactive;

namespace Toggl.Daneel.Extensions.Reactive
{
    public static class FadeViewExtensions
    {
        public static Action<bool> FadeRight(this IReactive<FadeView> reactive)
            => useRightFading => reactive.Base.FadeRight = useRightFading;

        public static Action<bool> FadeLeft(this IReactive<FadeView> reactive)
            => useLeftFading => reactive.Base.FadeLeft = useLeftFading;
    }
}
