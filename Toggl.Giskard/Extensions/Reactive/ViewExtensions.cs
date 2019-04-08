﻿using System;
using System.Reactive;
using System.Reactive.Linq;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Toggl.Foundation.UI.Reactive;
using Toggl.Multivac.Extensions;
using static Android.Views.View;

namespace Toggl.Giskard.Extensions.Reactive
{
    public enum ButtonEventType
    {
        Tap,
        LongPress
    }

    public static class ViewExtensions
    {
        public static IObservable<Unit> Tap(this IReactive<View> reactive)
            => Observable
                .FromEventPattern(e => reactive.Base.Click += e, e => reactive.Base.Click -= e)
                .SelectUnit();

        public static IObservable<Unit> LongPress(this IReactive<View> reactive)
            => Observable
                .FromEventPattern<LongClickEventArgs>(e => reactive.Base.LongClick += e, e => reactive.Base.LongClick -= e)
                .SelectUnit();

        public static Action<bool> Enabled(this IReactive<View> reactive)
            => enabled => reactive.Base.Enabled = enabled;

        public static Action<bool> IsVisible(this IReactive<View> reactive, bool useGone = true)
            => isVisible => reactive.Base.Visibility = isVisible.ToVisibility(useGone);

        public static Action<Color> DrawableColor(this IReactive<View> reactive)
            => color =>
            {
                if (reactive.Base.Background is GradientDrawable drawable)
                {
                    drawable.SetColor(color.ToArgb());
                    drawable.InvalidateSelf();
                }
            };

        public static IDisposable BindAction(this IReactive<View> reactive, UIAction action, ButtonEventType eventType = ButtonEventType.Tap)
        {
            IObservable<Unit> eventObservable = Observable.Empty<Unit>();
            switch (eventType)
            {
                case ButtonEventType.Tap:
                    eventObservable = reactive.Base.Rx().Tap();
                    break;
                case ButtonEventType.LongPress:
                    eventObservable = reactive.Base.Rx().LongPress();
                    break;
            }

            return Observable.Using(
                    () => action.Enabled.Subscribe(e => { reactive.Base.Enabled = e; }),
                    _ => eventObservable
                )
                .Subscribe(action.Inputs);
        }

        public static IDisposable BindAction<T>(this IReactive<View> reactive, InputAction<T> action, Func<View, T> convert, ButtonEventType eventType = ButtonEventType.Tap)
        {
            IObservable<Unit> eventObservable = Observable.Empty<Unit>();
            switch (eventType)
            {
                case ButtonEventType.Tap:
                    eventObservable = reactive.Base.Rx().Tap();
                    break;
                case ButtonEventType.LongPress:
                    eventObservable = reactive.Base.Rx().LongPress();
                    break;
            }

            return Observable.Using(
                    () => action.Enabled.Subscribe(e => { reactive.Base.Enabled = e; }),
                    _ => eventObservable
                )
                .Select(_ => convert(reactive.Base))
                .Subscribe(action.Inputs);
        }

        public static IDisposable BindAction<T>(this IReactive<View> reactive, OutputAction<T> action, ButtonEventType eventType = ButtonEventType.Tap)
        {
            IObservable<Unit> eventObservable = Observable.Empty<Unit>();
            switch (eventType)
            {
                case ButtonEventType.Tap:
                    eventObservable = reactive.Base.Rx().Tap();
                    break;
                case ButtonEventType.LongPress:
                    eventObservable = reactive.Base.Rx().LongPress();
                    break;
            }

            return Observable.Using(
                    () => action.Enabled.Subscribe(e => { reactive.Base.Enabled = e; }),
                    _ => eventObservable
                )
                .Subscribe(action.Inputs);
        }
    }
}
