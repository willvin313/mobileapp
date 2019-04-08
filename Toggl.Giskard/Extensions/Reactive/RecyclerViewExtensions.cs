using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Android.Support.V7.Widget;
using Toggl.Foundation.UI.Reactive;
using Toggl.Giskard.ViewHelpers;

namespace Toggl.Giskard.Extensions.Reactive
{
    public static class RecyclerViewExtensions
    {
        public static IObservable<Unit> OnScrolled(this IReactive<RecyclerView> reactive)
        {
            return Observable.Create<Unit>(observer =>
            {
                var onScrollChangeSubject = new Subject<Unit>();
                var scrollChangeListener = new SimpleRecyclerViewScrollChangeListener(onScrollChangeSubject);

                reactive.Base.AddOnScrollListener(scrollChangeListener);
                var scrollDisposable = onScrollChangeSubject.Subscribe(observer.OnNext);

                return Disposable.Create(() =>
                {
                    reactive.Base.RemoveOnScrollListener(scrollChangeListener);
                    scrollChangeListener = null;
                    scrollDisposable.Dispose();
                    onScrollChangeSubject.Dispose();
                });
            });
        }

    }
}
