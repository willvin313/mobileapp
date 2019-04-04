using System;
using System.Reactive.Linq;
using Android.Text;
using Android.Widget;
using Java.Lang;
using Toggl.Foundation.MvvmCross.Reactive;
using static Android.Views.View;
using static Android.Widget.SeekBar;

namespace Toggl.Giskard.Extensions.Reactive
{
    public static class SeekBarExtensions
    {
        public static IObservable<int> Progress(this IReactive<SeekBar> reactive)
            => Observable
                .FromEventPattern<ProgressChangedEventArgs>(e => reactive.Base.ProgressChanged += e, e => reactive.Base.ProgressChanged -= e)
                .Select(args => ((SeekBar)args.Sender).Progress)
                .Do(progress => System.Diagnostics.Debug.WriteLine($"Seekbar emitted {progress}"));

        public static Action<int> TextObserver(this IReactive<SeekBar> reactive, bool animate = true)
            => progressValue => reactive.Base.SetProgress(progressValue, animate);

    }
}
