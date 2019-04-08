using System;
using System.Reactive.Linq;
using Toggl.Foundation.UI.Reactive;
using UIKit;

namespace Toggl.Daneel.Extensions.Reactive
{
    public static class UISliderExtensions
    {
        public static IObservable<float> Value(this IReactive<UISlider> reactive)
            => Observable
                .FromEventPattern(e => reactive.Base.ValueChanged += e, e => reactive.Base.ValueChanged -= e)
                .Select(e => ((UISlider) e.Sender).Value);
    }
}
