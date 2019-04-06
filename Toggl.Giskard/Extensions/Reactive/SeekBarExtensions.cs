using System;
using System.Reactive.Linq;
using Android.Text;
using Android.Widget;
using Java.Lang;
using Toggl.Foundation.MvvmCross.Reactive;
using Toggl.Giskard.Views.Pomodoro;
using static Android.Views.View;
using static Android.Widget.SeekBar;

namespace Toggl.Giskard.Extensions.Reactive
{
    public static class SeekBarExtensions
    {
        public static IObservable<int> Duration(this IReactive<PomodoroDurationSeekbar> reactive)
            => Observable
                .FromEventPattern<ProgressChangedEventArgs>(e => reactive.Base.ProgressChanged += e, e => reactive.Base.ProgressChanged -= e)
                .Select(args => ((PomodoroDurationSeekbar)args.Sender).Duration);

        public static Action<int> DurationObserver(this IReactive<PomodoroDurationSeekbar> reactive, bool animate = true)
            => duration => reactive.Base.Duration = duration;

    }
}
