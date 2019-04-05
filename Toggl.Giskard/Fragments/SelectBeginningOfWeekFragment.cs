using System;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using MvvmCross.Droid.Support.V4;
using MvvmCross.Platforms.Android.Binding.BindingContext;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac.Extensions;
using Toggl.Giskard.Adapters;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.ViewHolders;
using System.Reactive.Disposables;
using Android.Support.V7.Widget;
using Toggl.Foundation.MvvmCross.Themes;

namespace Toggl.Giskard.Fragments
{
    [MvxDialogFragmentPresentation(AddToBackStack = true)]
    public sealed partial class SelectBeginningOfWeekFragment : ReactiveDialogFragment<SelectBeginningOfWeekViewModel>
    {
        private readonly CompositeDisposable disposeBag = new CompositeDisposable();

        public SelectBeginningOfWeekFragment() { }

        private SimpleAdapter<SelectableBeginningOfWeekViewModel> adapter;
        private ITheme currentTheme = new LightTheme();

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreateView(inflater, container, savedInstanceState);
            var view = inflater.Inflate(Resource.Layout.SelectBeginningOfWeekFragment, null);

            InitializeViews(view);

            setupRecyclerView();

            adapter.ItemTapObservable
                .Subscribe(ViewModel.SelectBeginningOfWeek.Inputs)
                .DisposedBy(disposeBag);

            return view;
        }

        private void setupRecyclerView()
        {
            recyclerView.SetLayoutManager(new LinearLayoutManager(Context));

            adapter = new SimpleAdapter<SelectableBeginningOfWeekViewModel>(
                Resource.Layout.SelectBeginningOfWeekFragmentCell,
                BeginningOfWeekViewHolder.Create);

            adapter.Items = ViewModel.BeginningOfWeekCollection;

            recyclerView.SetAdapter(adapter);
        }

        public override void OnResume()
        {
            base.OnResume();

            Dialog.Window.SetDefaultDialogLayout(Activity, Context, heightDp: 400);
            OnThemeChanged(currentTheme);
        }

        public override void OnCancel(IDialogInterface dialog)
        {
            ViewModel.Close.Execute();
        }

        protected override void OnThemeChanged(ITheme currentTheme)
        {
            base.OnThemeChanged(currentTheme);
            this.currentTheme = currentTheme;

            if (Activity == null || Activity.IsFinishing || View == null) return;

            View.SetBackgroundColor(currentTheme.Background.ToNativeColor());
            title?.SetTextColor(currentTheme.Text.ToNativeColor());
            subTitle?.SetTextColor(currentTheme.Text.ToNativeColor());
        }
    }
}
