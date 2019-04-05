using System;
using System.Reactive.Disposables;
using Android.Views;
using MvvmCross.Droid.Support.V4;
using MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.Themes;

namespace Toggl.Giskard.Fragments
{
    public abstract class ReactiveDialogFragment<TViewModel> : MvxDialogFragment<TViewModel>
        where TViewModel : class, IMvxViewModel
    {
        private IDisposable themeDisposable;

        protected CompositeDisposable DisposeBag = new CompositeDisposable();

        protected abstract void InitializeViews(View view);

        protected virtual void OnThemeChanged(ITheme currentTheme)
        {
        }

        public override void OnResume()
        {
            base.OnResume();

            themeDisposable = AndroidDependencyContainer.Instance
                .ThemeProvider
                .CurrentTheme
                .Subscribe(OnThemeChanged);
        }

        public override void OnPause()
        {
            base.OnPause();
            themeDisposable?.Dispose();
        }

        public override void OnDestroyView()
        {
            base.OnDestroyView();
            DisposeBag.Dispose();
            DisposeBag = new CompositeDisposable();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;
            DisposeBag?.Dispose();
            themeDisposable?.Dispose();
        }
    }
}
