using System;
using System.Reactive;
using System.Reactive.Linq;
using Android.Widget;
using Toggl.Foundation.UI.Reactive;
using Toggl.Multivac.Extensions;

namespace Toggl.Giskard.Extensions.Reactive
{
    public static class EditTextExtensions
    {
        public static IObservable<Unit> EditorActionSent(this IReactive<EditText> reactive)
            => Observable
                .FromEventPattern<TextView.EditorActionEventArgs>(e => reactive.Base.EditorAction += e, e => reactive.Base.EditorAction -= e)
                .SelectUnit();
    }
}
