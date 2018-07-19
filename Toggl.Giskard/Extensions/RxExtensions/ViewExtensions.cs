using System;
using System.Reactive;
using System.Reactive.Linq;
using Android.Views;
using Toggl.Multivac.Extensions;

namespace Toggl.Giskard.Extensions
{
    public static partial class ViewExtensions
    {
        public static IObservable<Unit> Tapped(this View button)
            => Observable
                .FromEventPattern(e => button.Click += e, e => button.Click -= e)
                .SelectUnit();

        public static Action<bool> BindIsVisible(this View view)
            => isVisible => view.Visibility = isVisible.ToVisibility();

        public static Action<bool> BindEnabled(this View view)
            => enabled => view.Enabled = enabled;
    }
}
