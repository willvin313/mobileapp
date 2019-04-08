using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MvvmCross.Droid.Support.V4;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using MvvmCross.Platforms.Android.Views;
using Toggl.Foundation.UI.ViewModels;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.Extensions.Reactive;
using Toggl.Multivac.Extensions;

namespace Toggl.Giskard.Fragments
{
    [MvxDialogFragmentPresentation(Cancelable = false)]
    public sealed partial class NoWorkspaceFragment : MvxDialogFragment<NoWorkspaceViewModel>
    {
        public CompositeDisposable DisposeBag { get; } = new CompositeDisposable();

        public NoWorkspaceFragment()
        {
        }

        public NoWorkspaceFragment(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreateView(LayoutInflater, container, savedInstanceState);
            var rootView = inflater.Inflate(Resource.Layout.NoWorkspaceFragment, container, false);
            InitializeViews(rootView);

            createWorkspaceTextView.Rx()
                .BindAction(ViewModel.CreateWorkspaceWithDefaultName)
                .DisposedBy(DisposeBag);

            tryAgainTextView.Rx()
                .BindAction(ViewModel.TryAgain)
                .DisposedBy(DisposeBag);

            ViewModel.IsLoading
                .Select(CommonFunctions.Invert)
                .Subscribe(createWorkspaceTextView.Rx().Enabled())
                .DisposedBy(DisposeBag);

            ViewModel.IsLoading
                .Select(CommonFunctions.Invert)
                .Subscribe(tryAgainTextView.Rx().Enabled())
                .DisposedBy(DisposeBag);

            ViewModel.IsLoading
                .StartWith(false)
                .Subscribe(onLoadingStateChanged)
                .DisposedBy(DisposeBag);

            return rootView;
        }

        public override void OnResume()
        {
            base.OnResume();
            updateDialogHeight();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            DisposeBag?.Dispose();
        }

        private void onLoadingStateChanged(bool isLoading)
        {
            progressBar.Visibility = isLoading.ToVisibility();
            updateDialogHeight();
        }

        private void updateDialogHeight()
        {
            Dialog?.Window?.SetDefaultDialogLayout(Activity, Context, heightDp: ViewGroup.LayoutParams.WrapContent);
        }
    }
}
