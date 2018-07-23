using System.Reactive.Disposables;
using Android.Views;
using MvvmCross.Droid.Support.V4;
using MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.ViewModels;

namespace Toggl.Giskard.Fragments
{
    public abstract class ReactiveFragment<TViewModel> : MvxFragment<TViewModel>, IReactiveBindingHolder
        where TViewModel : MvxViewModel
    {
        public CompositeDisposable DisposeBag { get; } = new CompositeDisposable();

        protected View InflateAndInitializeViews(int resourceId)
        {
            var view = LayoutInflater.Inflate(resourceId, null);
            InitializeViews(view);
            return view;
        }

        protected abstract void InitializeViews(View view);

        public override void OnDestroyView()
        {
            base.OnDestroyView();

            DisposeBag?.Dispose();
        }
    }
}
