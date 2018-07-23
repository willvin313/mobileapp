using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Android.Support.V4.View;
using Toggl.Multivac.Extensions;
using Object = Java.Lang.Object;

namespace Toggl.Giskard.Extensions
{
    public static partial class ViewExtensions
    {
        public static IObservable<int> CurrentPage(this ViewPager pager)
        {
            var pageChangeListener = new OnPageChangeListener();
            pager.AddOnPageChangeListener(pageChangeListener);

            return Observable.Create<int>(observer =>
            {
                var disposeBag = new CompositeDisposable();

                pageChangeListener
                    .PageChange
                    .Subscribe(observer)
                    .DisposedBy(disposeBag);

                Disposable
                    .Create(() => pager.RemoveOnPageChangeListener(pageChangeListener))
                    .DisposedBy(disposeBag);

                return disposeBag;
            });
        }

        public static Action<int> BindCurrentPage(this ViewPager pager)
            => currentPage => pager.SetCurrentItem(currentPage, true);

        private class OnPageChangeListener : Object, ViewPager.IOnPageChangeListener
        {
            private readonly Subject<int> pageChangedSubject = new Subject<int>();

            public IObservable<int> PageChange { get; }

            public OnPageChangeListener()
            {
                PageChange = pageChangedSubject.AsObservable();
            }

            public void OnPageScrolled(int position, float positionOffset, int positionOffsetPixels)
            {
            }

            public void OnPageScrollStateChanged(int state)
            {
            }

            public void OnPageSelected(int position)
            {
                pageChangedSubject.OnNext(position);
            }
        }
    }
}
