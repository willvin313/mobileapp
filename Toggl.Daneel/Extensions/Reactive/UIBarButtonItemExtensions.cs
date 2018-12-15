using System;
using System.Reactive;
using System.Reactive.Linq;
using Toggl.Foundation.MvvmCross.Reactive;
using UIKit;
using Toggl.Multivac.Extensions;

namespace Toggl.Daneel.Extensions
{
    public static class UIBarButtonItemExtensions
    {
        public static Action<bool> Enabled(this IReactive<UIBarButtonItem> reactive)
            => enabled => reactive.Base.Enabled = enabled;

        public static IObservable<Unit> Tap(this IReactive<UIBarButtonItem> reactive)
            => Observable
                .FromEventPattern(e => reactive.Base.Clicked += e, e => reactive.Base.Clicked -= e)
                .SelectUnit();

        public static IDisposable BindAction(this IReactive<UIBarButtonItem> reactive, UIAction action)
        {
            IObservable<Unit> eventObservable =  reactive.Base.Rx().Tap();
            return Observable.Using(
                    () => action.Enabled.Subscribe(e => { reactive.Base.Enabled = e; }),
                    _ => eventObservable
                )
                .Subscribe(action.Inputs);
        }
    }
}
