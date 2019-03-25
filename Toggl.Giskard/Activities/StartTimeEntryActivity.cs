using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using Toggl.Foundation.Autocomplete;
using Toggl.Foundation.MvvmCross.Onboarding.StartTimeEntryView;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Giskard.Adapters;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.Extensions.Reactive;
using Toggl.Giskard.Helper;
using Toggl.Multivac.Extensions;

namespace Toggl.Giskard.Activities
{
    [MvxActivityPresentation]
    [Activity(Theme = "@style/AppTheme.BlueStatusBar",
              ScreenOrientation = ScreenOrientation.Portrait,
              WindowSoftInputMode = SoftInput.StateVisible,
              ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public sealed partial class StartTimeEntryActivity : ReactiveActivity<StartTimeEntryViewModel>
    {
        private static readonly TimeSpan typingThrottleDuration = TimeSpan.FromMilliseconds(300);

        private PopupWindow onboardingPopupWindow;
        private IDisposable onboardingDisposable;
        private EventHandler onLayoutFinished;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.StartTimeEntryActivity);
            OverridePendingTransition(Resource.Animation.abc_slide_in_bottom, Resource.Animation.abc_fade_out);

            InitializeViews();

            // Suggestions RecyclerView
            var adapter = new StartTimeEntryRecyclerAdapter();
            recyclerView.SetLayoutManager(new LinearLayoutManager(this));
            recyclerView.SetAdapter(adapter);
            
            ViewModel.Suggestions
                .Subscribe(adapter.SetItems)
                .DisposedBy(DisposeBag);

            adapter.ItemTapObservable
                .Subscribe(ViewModel.SelectSuggestion.Inputs)
                .DisposedBy(DisposeBag);

            adapter.ToggleTasks
                .Subscribe(ViewModel.ToggleTasks.Inputs)
                .DisposedBy(DisposeBag);

            // Displayed time
            ViewModel.DisplayedTime
                .Subscribe(durationLabel.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            // Toggle project suggestions toolbar button
            selectProjectToolbarButton.Rx()
                .BindAction(ViewModel.ToggleProjectSuggestions)
                .DisposedBy(DisposeBag);

            ViewModel.IsSuggestingProjects
                .Select(isSuggesting => isSuggesting ? Resource.Drawable.te_project_active : Resource.Drawable.project)
                .Subscribe(selectProjectToolbarButton.SetImageResource)
                .DisposedBy(DisposeBag);

            // Toggle tag suggestions toolbar button
            selectTagToolbarButton.Rx()
                .BindAction(ViewModel.ToggleTagSuggestions)
                .DisposedBy(DisposeBag);

            ViewModel.IsSuggestingTags
                .Select(isSuggesting => isSuggesting ? Resource.Drawable.te_tag_active : Resource.Drawable.tag)
                .Subscribe(selectTagToolbarButton.SetImageResource)
                .DisposedBy(DisposeBag);

            // Billable toolbar button
            selectBillableToolbarButton.Rx()
                .BindAction(ViewModel.ToggleBillable)
                .DisposedBy(DisposeBag);

            ViewModel.IsBillable
                .Select(isSuggesting => isSuggesting ? Resource.Drawable.te_billable_active : Resource.Drawable.billable)
                .Subscribe(selectBillableToolbarButton.SetImageResource)
                .DisposedBy(DisposeBag);

            ViewModel.IsBillableAvailable
                .Subscribe(selectBillableToolbarButton.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            // Finish buttons
            doneButton.Rx()
                .BindAction(ViewModel.Done)
                .DisposedBy(DisposeBag);

            closeButton.Rx()
                .BindAction(ViewModel.Close)
                .DisposedBy(DisposeBag);
            
            // Description text field
            descriptionField.Hint = ViewModel.PlaceholderText;

            ViewModel.TextFieldInfo
                .Subscribe(onTextFieldInfo)
                .DisposedBy(DisposeBag);

            durationLabel.Rx()
                .BindAction(ViewModel.ChangeTime)
                .DisposedBy(DisposeBag);

            descriptionField.TextObservable
                .SubscribeOn(ThreadPoolScheduler.Instance)
                .Throttle(typingThrottleDuration)
                .Select(text => text.AsImmutableSpans(descriptionField.SelectionStart))
                .Subscribe(ViewModel.SetTextSpans.Inputs)
                .DisposedBy(DisposeBag);

            onLayoutFinished = (s, e) => ViewModel.StopSuggestionsRenderingStopwatch();
            recyclerView.ViewTreeObserver.GlobalLayout += onLayoutFinished;
        }

        protected override void OnResume()
        {
            base.OnResume();
            descriptionField.RequestFocus();
            selectProjectToolbarButton.LayoutChange += onSelectProjectToolbarButtonLayoutChanged;
            recyclerView.ViewTreeObserver.GlobalLayout += onLayoutFinished;
        }

        private void onSelectProjectToolbarButtonLayoutChanged(object sender, View.LayoutChangeEventArgs changeEventArgs)
        {
            selectProjectToolbarButton.Post(setupStartTimeEntryOnboardingStep);
        }

        protected override void OnPause()
        {
            base.OnPause();
            recyclerView.ViewTreeObserver.GlobalLayout -= onLayoutFinished;
        }

        protected override void OnStop()
        {
            base.OnStop();
            selectProjectToolbarButton.LayoutChange -= onSelectProjectToolbarButtonLayoutChanged;
            onboardingPopupWindow?.Dismiss();
        }

        public override void Finish()
        {
            base.Finish();
            OverridePendingTransition(Resource.Animation.abc_fade_in, Resource.Animation.abc_slide_out_bottom);
        }

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            if (keyCode == Keycode.Back)
            {
                ViewModel.Close.Execute();
                return true;
            }

            return base.OnKeyDown(keyCode, e);
        }

        private void setupStartTimeEntryOnboardingStep()
        {
            clearPreviousOnboardingSetup();

            onboardingPopupWindow = PopupWindowFactory.PopupWindowWithText(
                this,
                Resource.Layout.TooltipWithCenteredBottomArrow,
                Resource.Id.TooltipText,
                Resource.String.OnboardingAddProjectOrTag);

            var storage = ViewModel.OnboardingStorage;

            onboardingDisposable = new AddProjectOrTagOnboardingStep(storage, ViewModel.DataSource)
                .ManageDismissableTooltip(
                    Observable.Return(true),
                    onboardingPopupWindow,
                    selectProjectToolbarButton,
                    (popup, anchor) => popup.TopHorizontallyCenteredOffsetsTo(anchor, 8),
                    storage);
        }

        private void clearPreviousOnboardingSetup()
        {
            onboardingDisposable?.Dispose();
            onboardingDisposable = null;
            onboardingPopupWindow?.Dismiss();
            onboardingPopupWindow = null;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            DisposeBag?.Dispose();
            onboardingDisposable?.Dispose();
        }

        private void onTextFieldInfo(TextFieldInfo textFieldInfo)
        {
            var (formattedText, cursorPosition) = textFieldInfo.AsSpannableTextAndCursorPosition();
            if (descriptionField.TextFormatted.ToString() == formattedText.ToString())
                return;

            descriptionField.TextFormatted = formattedText;
            descriptionField.SetSelection(cursorPosition);
        }
    }
}
