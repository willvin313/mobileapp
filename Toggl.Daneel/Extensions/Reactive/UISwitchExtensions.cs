using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Toggl.Foundation.MvvmCross.Reactive;
using UIKit;

namespace Toggl.Daneel.Extensions.Reactive
{
    public static class UISwitchExtensions
    {
        public static IObservable<Unit> Changed(this IReactive<UISwitch> reactive)
            => Observable.Create<Unit>(observer =>
            {
                void changed(object sender, EventArgs args)
                {
                    observer.OnNext(Unit.Default);
                }

                reactive.Base.ValueChanged += changed;

                return Disposable.Create(() => reactive.Base.ValueChanged -= changed);
            });

        public static Action<bool> On(this IReactive<UISwitch> reactive)
            => isOn => reactive.Base.SetState(isOn, true);
    }
}
