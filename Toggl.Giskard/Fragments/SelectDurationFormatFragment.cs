using System;
using System.Linq;
using System.Reactive.Disposables;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;
using MvvmCross.Droid.Support.V4;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using Toggl.Foundation.MvvmCross.Themes;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Giskard.Adapters;
using Toggl.Giskard.Extensions;
using Toggl.Multivac.Extensions;

namespace Toggl.Giskard.Fragments
{
    [MvxDialogFragmentPresentation(AddToBackStack = true)]
    public sealed partial class SelectDurationFormatFragment : ReactiveDialogFragment<SelectDurationFormatViewModel>
    {
        private readonly CompositeDisposable disposeBag  = new CompositeDisposable();
        private ITheme currentTheme;

        public SelectDurationFormatFragment() { }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreateView(inflater, container, savedInstanceState);
            var view = inflater.Inflate(Resource.Layout.SelectDurationFormatFragment, null);

            InitializeViews(view);

            recyclerView.SetLayoutManager(new LinearLayoutManager(Context));
            selectDurationRecyclerAdapter = new SelectDurationFormatRecyclerAdapter();
            selectDurationRecyclerAdapter.Items = ViewModel.DurationFormats.ToList();
            recyclerView.SetAdapter(selectDurationRecyclerAdapter);

            selectDurationRecyclerAdapter.ItemTapObservable
                .Subscribe(ViewModel.SelectDurationFormat.Inputs)
                .DisposedBy(disposeBag);

            return view;
        }

        public override void OnResume()
        {
            base.OnResume();

            Dialog.Window.SetDefaultDialogLayout(Activity, Context, heightDp: 268);
            OnThemeChanged(currentTheme);
        }

        public override void OnCancel(IDialogInterface dialog)
        {
            ViewModel.Close.Execute();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            disposeBag.Dispose();
        }

        protected override void OnThemeChanged(ITheme currentTheme)
        {
            base.OnThemeChanged(currentTheme);
            this.currentTheme = currentTheme;

            if (Activity == null || Activity.IsFinishing || View == null) return;

            View.SetBackgroundColor(currentTheme.Background.ToNativeColor());
            title?.SetTextColor(currentTheme.Text.ToNativeColor());
        }
    }
}
