using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Toggl.Foundation.MvvmCross.Reactive;
using Toggl.Multivac.Extensions;
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

        public static IDisposable BindToggleAction<TElement>(this IReactive<UISwitch> reactive, RxAction<bool, TElement> action)
            => reactive.BindAction(action, s => s.On);

        public static IDisposable BindTapAction<TElement>(this IReactive<UISwitch> reactive, RxAction<Unit, TElement> action)
            => reactive.BindAction(action, _ => Unit.Default);

        public static IDisposable BindAction<TInput, TElement>(this IReactive<UISwitch> reactive,
            RxAction<TInput, TElement> action, Func<UISwitch, TInput> inputTransform)
        {
            IObservable<Unit> eventObservable = reactive.Base.Rx().Changed();

            return Observable.Using(
                    () => action.Enabled.Subscribe(e => { reactive.Base.Enabled = e; }),
                    _ => eventObservable
                )
                .Select(_ => inputTransform(reactive.Base))
                .Subscribe(action.Inputs);
        }

        public static Action<bool> CheckedObserver(this IReactive<UISwitch> reactive)
        {
            return isChecked =>
            {
                if (reactive.Base.On != isChecked)
                {
                    reactive.Base.SetState(isChecked, false);
                }
            };
        }
        
        public static Action<bool> On(this IReactive<UISwitch> reactive)
            => isOn => reactive.Base.SetState(isOn, true);
    }
}
