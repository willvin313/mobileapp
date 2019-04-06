using System;
using System.Reactive.Linq;
using Android.Text;
using Android.Widget;
using Java.Lang;
using Toggl.Foundation.Models.Pomodoro;
using Toggl.Foundation.MvvmCross.Reactive;
using Toggl.Giskard.Views.Pomodoro;
using static Android.Views.View;
using static Android.Widget.SeekBar;

namespace Toggl.Giskard.Extensions.Reactive
{
    public static class PomodoroWorkflowViewExtensions
    {
        public static IObservable<int> SelectedIndex(this IReactive<PomodoroWorkflowView> reactive)
            => Observable
            .FromEventPattern<SelectedWorkflowItemChangedEventArgs>(e => reactive.Base.SelectedWorkflowItemIndexChanged += e, e => reactive.Base.SelectedWorkflowItemIndexChanged -= e)
            .Select(args => reactive.Base.SelectedWorkflowItemIndex);

        public static IObservable<PomodoroWorkflowItem> SelectedWorkflowItem(this IReactive<PomodoroWorkflowView> reactive)
            => Observable
            .FromEventPattern<SelectedWorkflowItemChangedEventArgs>(e => reactive.Base.SelectedWorkflowItemIndexChanged += e, e => reactive.Base.SelectedWorkflowItemIndexChanged -= e)
            .Select(args => reactive.Base.SelectedWorkflowItem);
    }
}
