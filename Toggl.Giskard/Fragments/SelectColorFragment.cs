using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using MvvmCross.Droid.Support.V4;
using MvvmCross.Platforms.Android.Binding.BindingContext;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using Toggl.Foundation.MvvmCross.Themes;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Giskard.Adapters;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.Extensions.Reactive;
using Toggl.Giskard.ViewHolders;
using Toggl.Giskard.Views;
using Toggl.Multivac.Extensions;

namespace Toggl.Giskard.Fragments
{
    [MvxDialogFragmentPresentation(AddToBackStack = true)]
    public sealed partial class SelectColorFragment : ReactiveDialogFragment<SelectColorViewModel>
    {
        private ITheme currentTheme = new LightTheme();
        private const int customColorEnabledHeight = 425;
        private const int customColorDisabledHeight = 270;

        public CompositeDisposable DisposeBag { get; } = new CompositeDisposable();

        public SelectColorFragment() { }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreateView(inflater, container, savedInstanceState);
            var view = inflater.Inflate(Resource.Layout.SelectColorFragment, null);

            InitializeViews(view);

            recyclerView.SetLayoutManager(new GridLayoutManager(Context, 5));

            selectableColorsAdapter = new SimpleAdapter<SelectableColorViewModel>(
                Resource.Layout.SelectColorFragmentCell, ColorSelectionViewHolder.Create);

            selectableColorsAdapter.ItemTapObservable
                .Select(x => x.Color)
                .Subscribe(ViewModel.SelectColor.Inputs)
                .DisposedBy(DisposeBag);

            recyclerView.SetAdapter(selectableColorsAdapter);

            ViewModel.Hue
                .Subscribe(hueSaturationPicker.Rx().HueObserver())
                .DisposedBy(DisposeBag);

            ViewModel.Saturation
                .Subscribe(hueSaturationPicker.Rx().SaturationObserver())
                .DisposedBy(DisposeBag);

            ViewModel.Value
                .Subscribe(hueSaturationPicker.Rx().ValueObserver())
                .DisposedBy(DisposeBag);

            hueSaturationPicker.Rx().Hue()
                .Subscribe(ViewModel.SetHue.Inputs)
                .DisposedBy(DisposeBag);

            hueSaturationPicker.Rx().Saturation()
                .Subscribe(ViewModel.SetSaturation.Inputs)
                .DisposedBy(DisposeBag);

            valueSlider.Rx().Progress()
                .Select(invertedNormalizedProgress)
                .Subscribe(ViewModel.SetValue.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.Hue
                .Subscribe(valueSlider.Rx().HueObserver())
                .DisposedBy(DisposeBag);

            ViewModel.Saturation
                .Subscribe(valueSlider.Rx().SaturationObserver())
                .DisposedBy(DisposeBag);

            saveButton.Rx()
                .BindAction(ViewModel.Save)
                .DisposedBy(DisposeBag);

            closeButton.Rx()
                .BindAction(ViewModel.Close)
                .DisposedBy(DisposeBag);

            ViewModel.SelectableColors
                     .Subscribe(updateColors)
                     .DisposedBy(DisposeBag);

            hueSaturationPicker.Visibility = ViewModel.AllowCustomColors.ToVisibility();
            valueSlider.Visibility = ViewModel.AllowCustomColors.ToVisibility();

            return view;
        }

        private void updateColors(IEnumerable<SelectableColorViewModel> colors)
        {
            selectableColorsAdapter.Items = colors.ToList();
        }

        public override void OnResume()
        {
            base.OnResume();

            var height = ViewModel.AllowCustomColors ? customColorEnabledHeight : customColorDisabledHeight;

            Dialog.Window.SetDefaultDialogLayout(Activity, Context, heightDp: height);
            OnThemeChanged(currentTheme);
        }

        public override void OnCancel(IDialogInterface dialog)
        {
            ViewModel.Close.Execute();
        }

        private float invertedNormalizedProgress(int progress)
            => 1f - (progress / 100f);

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
