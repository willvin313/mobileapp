using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CoreGraphics;
using Foundation;
using MvvmCross.Plugin.Color.Platforms.Ios;
using MvvmCross.UI;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Foundation.Extensions;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.Onboarding.EditView;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac.Extensions;
using UIKit;
using Toggl.Daneel.Transformations;
using System.Linq;
using Toggl.Foundation.MvvmCross.Transformations;
using Toggl.Foundation.Analytics;

namespace Toggl.Daneel.ViewControllers
{
    [ModalCardPresentation]
    public partial class EditTimeEntryViewController : KeyboardAwareViewController<EditTimeEntryViewModel>, IDismissableViewController
    {
        private const float nonScrollableContentHeight = 116f;

        private IDisposable projectOnboardingDisposable;
        private IDisposable contentSizeChangedDisposable;

        private ProjectTaskClientToAttributedString projectTaskClientToAttributedString;
        private TagsListToAttributedString tagsListToAttributedString;

        private const string boundsKey = "bounds";

        public EditTimeEntryViewController()
            : base(nameof(EditTimeEntryViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            projectTaskClientToAttributedString = new ProjectTaskClientToAttributedString(
                ProjectTaskClientLabel.Font.CapHeight,
                Color.EditTimeEntry.ClientText.ToNativeColor(),
                false);

            tagsListToAttributedString = new TagsListToAttributedString(TagsTextView);

            localizeLabels();
            prepareViews();
            prepareOnboarding();

            contentSizeChangedDisposable = ScrollViewContent.AddObserver(boundsKey, NSKeyValueObservingOptions.New, onContentSizeChanged);

            DescriptionTextView.Text = ViewModel.Description.Value;

            ViewModel.Preferences
                .Select(preferences => preferences.DurationFormat)
                .Select(format => ViewModel.GroupDuration.ToFormattedString(format))
                .Subscribe(GroupDuration.Rx().Text())
                .DisposedBy(DisposeBag);

            CloseButton.Rx()
                .BindAction(ViewModel.Close)
                .DisposedBy(DisposeBag);

            ConfirmButton.Rx()
                .BindAction(ViewModel.Save)
                .DisposedBy(DisposeBag);

            DescriptionTextView.TextObservable
                .Subscribe(ViewModel.Description.Accept)
                .DisposedBy(DisposeBag);

            ViewModel.SyncErrorMessage
                .Subscribe(ErrorMessageLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            ViewModel.IsSyncErrorMessageVisible
                .Subscribe(ErrorView.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ErrorView.Rx().Tap()
                .Subscribe(ViewModel.DismissSyncErrorMessage.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.ProjectClientTask
                .Select(info => projectTaskClientToAttributedString.Convert(
                    info.Project,
                    info.Task,
                    info.Client,
                    MvxColor.ParseHexString(info.ProjectColor).ToNativeColor()))
                .Subscribe(ProjectTaskClientLabel.Rx().AttributedText())
                .DisposedBy(DisposeBag);

            ViewModel.ProjectClientTask
                .Select(info => info.HasProject)
                .Subscribe(ProjectTaskClientLabel.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.ProjectClientTask
                .Select(info => !info.HasProject)
                .Subscribe(AddProjectAndTaskView.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            SelectProject.Rx()
                .BindAction(ViewModel.SelectProject)
                .DisposedBy(DisposeBag);

            TagsTextView.Rx()
                .BindAction(ViewModel.SelectTags)
                .DisposedBy(DisposeBag);

            AddTagsView.Rx()
                .BindAction(ViewModel.SelectTags)
                .DisposedBy(DisposeBag);

            var containsTags = ViewModel.Tags
                .Select(tags => tags.Any());

            containsTags
                .Invert()
                .Subscribe(AddTagsView.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            containsTags
                .Subscribe(TagsTextView.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.IsBillable
                .Subscribe(BillableSwitch.Rx().CheckedObserver())
                .DisposedBy(DisposeBag);

            BillableSwitch.Rx().Changed()
                .Subscribe(ViewModel.ToggleBillable.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.IsBillableAvailable
                .Subscribe(BillableView.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.IsInaccessible
                .Subscribe(adjustUIForInaccessibleTimeEntry)
                .DisposedBy(DisposeBag);

            ViewModel.StartTime
                .WithLatestFrom(ViewModel.Preferences,
                    (startTime, preferences) => DateTimeToFormattedString.Convert(startTime, preferences.TimeOfDayFormat.Format))
                .Subscribe(StartTimeLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            ViewModel.StartTime
                .WithLatestFrom(ViewModel.Preferences,
                    (startTime, preferences) => DateTimeToFormattedString.Convert(startTime, preferences.DateFormat.Short))
                .Subscribe(StartDateLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            StartTimeView.Rx().Tap()
                .SelectValue(EditViewTapSource.StartTime)
                .Subscribe(ViewModel.EditTimes.Inputs)
                .DisposedBy(DisposeBag);

            StartDateView.Rx().Tap()
                .SelectValue(EditViewTapSource.StartDate)
                .Subscribe(ViewModel.EditTimes.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.IsTimeEntryRunning
                .Subscribe(StopButton.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.IsTimeEntryRunning
                .Select(CommonFunctions.Invert)
                .Subscribe(EndTimeLabel.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.StopTime
                .Where(stopTime => stopTime.HasValue)
                .Select(stopTime => stopTime.Value)
                .WithLatestFrom(ViewModel.Preferences,
                    (stopTime, preferences) => DateTimeToFormattedString.Convert(stopTime, preferences.TimeOfDayFormat.Format))
                .Subscribe(EndTimeLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            EndTimeView.Rx().Tap()
                .SelectLatestFrom(ViewModel.IsTimeEntryRunning)
                .Invert()
                .Where(CommonFunctions.Identity)
                .SelectValue(EditViewTapSource.StopTime)
                .Subscribe(ViewModel.EditTimes.Inputs)
                .DisposedBy(DisposeBag);

            EndTimeView.Rx().Tap()
               .Merge(StopButton.Rx().Tap())
               .SelectLatestFrom(ViewModel.IsTimeEntryRunning)
               .Where(CommonFunctions.Identity)
               .SelectUnit()
               .Subscribe(ViewModel.StopTimeEntry.Inputs)
               .DisposedBy(DisposeBag);

            ViewModel.Duration
                .WithLatestFrom(ViewModel.Preferences,
                    (duration, preferences) => duration.ToFormattedString(preferences.DurationFormat))
                .Subscribe(DurationLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            DurationView.Rx().Tap()
                .SelectValue(EditViewTapSource.Duration)
                .Subscribe(ViewModel.EditTimes.Inputs)
                .DisposedBy(DisposeBag);

            DeleteButton.Rx()
                .BindAction(ViewModel.Delete)
                .DisposedBy(DisposeBag);
        }

        public override void ViewWillLayoutSubviews()
        {
            adjustHeight();
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();

            //This binding needs to be created, when TagsTextView has it's
            //proper size. In ViewDidLoad() TagsTextView width isn't initialized
            //yet, which results in displaying less tags than possible
            ViewModel.Tags
                .Select(tagsListToAttributedString.Convert)
                .Subscribe(TagsTextView.Rx().AttributedTextObserver())
                .DisposedBy(DisposeBag);
        }

        public async Task<bool> Dismiss()
        {
            return await ViewModel.Close.ExecuteWithCompletion();
        }

        private void prepareViews()
        {
            DurationLabel.Font = DurationLabel.Font.GetMonospacedDigitFont();
            PreferredContentSize = View.Frame.Size;

            centerTextVertically(TagsTextView);
            TagsTextView.TextContainer.LineFragmentPadding = 0;

            if (UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
            {
                var bottomSafeAreaInset = UIApplication.SharedApplication.KeyWindow.SafeAreaInsets.Bottom;
                if (bottomSafeAreaInset >= DeleteButtonBottomConstraint.Constant)
                    DeleteButtonBottomConstraint.Constant
                        = ConfirmButtonBottomConstraint.Constant
                        = 0;
            }

            DescriptionTextView.TintColor = Color.StartTimeEntry.Cursor.ToNativeColor();
            DescriptionTextView.PlaceholderText = Resources.AddDescription;

            TimeEntryTimes.Hidden = ViewModel.IsEditingGroup;
            TimeEntryTimesSeparator.Hidden = ViewModel.IsEditingGroup;
            GroupDuration.Hidden = !ViewModel.IsEditingGroup;
            DurationView.Hidden = ViewModel.IsEditingGroup;
            StartDateView.Hidden = ViewModel.IsEditingGroup;
            DurationSeparator.Hidden = ViewModel.IsEditingGroup;
            StartDateSeparator.Hidden = ViewModel.IsEditingGroup;
        }

        private void localizeLabels()
        {
            TitleLabel.Text = ViewModel.IsEditingGroup
                ? string.Format(Resources.EditingTimeEntryGroup, ViewModel.GroupCount)
                : Resources.Edit;

            BillableLabel.Text = Resources.Billable;
            StartDateDescriptionLabel.Text = Resources.Startdate;
            DurationDescriptionLabel.Text = Resources.Duration;
            StartDescriptionLabel.Text = Resources.Start;
            EndDescriptionLabel.Text = Resources.End;
            ErrorMessageTitleLabel.Text = Resources.Oops;
            AddProjectTaskLabel.Text = Resources.AddProjectTask;
            CategorizeWithProjectsLabel.Text = Resources.CategorizeYourTimeWithProjects;
            AddTagsLabel.Text = Resources.AddTags;
            DeleteButton.SetTitle(Resources.Delete, UIControlState.Normal);
            ConfirmButton.SetTitle(Resources.ConfirmChanges, UIControlState.Normal);
        }

        private void adjustUIForInaccessibleTimeEntry(bool isInaccessible)
        {
            DescriptionTextView.UserInteractionEnabled = !isInaccessible;
            StartTimeView.UserInteractionEnabled = !isInaccessible;
            StartDateView.UserInteractionEnabled = !isInaccessible;
            EndTimeView.UserInteractionEnabled = !isInaccessible;
            DurationView.UserInteractionEnabled = !isInaccessible;
            StopButton.UserInteractionEnabled = !isInaccessible;

            BillableSwitch.Enabled = !isInaccessible;
            TagsContainerView.Hidden = isInaccessible;
            TagsSeparator.Hidden = isInaccessible;

            var textColor = isInaccessible
                ? Color.Common.Disabled.ToNativeColor()
                : Color.Common.TextColor.ToNativeColor();

            DescriptionTextView.TextColor = textColor;

            foreach (var label in getLabelsToChangeColorWhenEditingInaccessibleEntry())
            {
                label.TextColor = textColor;
            }
        }

        IEnumerable<UILabel> getLabelsToChangeColorWhenEditingInaccessibleEntry()
        {
            yield return StartTimeLabel;
            yield return StartDateLabel;
            yield return EndTimeLabel;
            yield return DurationLabel;
        }

        private void centerTextVertically(UITextView textView)
        {
            var topOffset = (textView.Bounds.Height - textView.ContentSize.Height) / 2;
            textView.ContentInset = new UIEdgeInsets(topOffset, 0, 0, 0);
        }

        private void prepareOnboarding()
        {
            var storage = ViewModel.OnboardingStorage;

            projectOnboardingDisposable = new CategorizeTimeUsingProjectsOnboardingStep(storage, ViewModel.ProjectClientTask.Select(x => x.HasProject).AsObservable())
                .ManageDismissableTooltip(CategorizeWithProjectsBubbleView, storage);
        }

        private void onContentSizeChanged(NSObservedChange change)
        {
            adjustHeight();
        }

        private void adjustHeight()
        {
            var height = nonScrollableContentHeight + ScrollViewContent.Bounds.Height;
            if (UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
            {
                height += UIApplication.SharedApplication.KeyWindow.SafeAreaInsets.Bottom;
            }

            var newSize = new CGSize(0, height);
            if (newSize != PreferredContentSize)
            {
                PreferredContentSize = newSize;
                PresentationController.ContainerViewWillLayoutSubviews();
            }

            ScrollView.ScrollEnabled = ScrollViewContent.Bounds.Height > ScrollView.Bounds.Height;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing)
                return;

            contentSizeChangedDisposable?.Dispose();
            contentSizeChangedDisposable = null;

            projectOnboardingDisposable?.Dispose();
            projectOnboardingDisposable = null;
        }

        protected override void KeyboardWillShow(object sender, UIKeyboardEventArgs e)
        {
            // this bit of code depends on the knowledge of the component tree:
            // - description label is inside of a container view which is placed in a stack view
            // - we want to know the vertical location of the container view in the whole view
            // - we actually want to know the Y coordinate of the bottom of the container and make
            //   sure we don't overaly it with the keyboard
            var container = DescriptionTextView.Superview;
            var absoluteLocation = View.ConvertPointFromView(container.Frame.Location, container.Superview);
            var minimumVisibleContentHeight = View.Frame.Height - absoluteLocation.Y - container.Frame.Height;

            var coveredByKeyboard = e.FrameEnd.Height - minimumVisibleContentHeight;

            if (coveredByKeyboard < 0)
            {
                return;
            }

            var safeAreaOffset = UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
                ? Math.Max(UIApplication.SharedApplication.KeyWindow.SafeAreaInsets.Top, UIApplication.SharedApplication.StatusBarFrame.Height)
                : 0;
            var distanceFromTop = Math.Max(safeAreaOffset, View.Frame.Y - coveredByKeyboard);

            View.Frame = new CGRect(View.Frame.X, distanceFromTop, View.Frame.Width, View.Frame.Height);
            UIView.Animate(Animation.Timings.EnterTiming, View.LayoutIfNeeded);
        }

        protected override void KeyboardWillHide(object sender, UIKeyboardEventArgs e)
        {
            var distanceFromTop = UIScreen.MainScreen.Bounds.Height - View.Frame.Height;

            Console.WriteLine($"Resetting distance: {distanceFromTop}");
            View.Frame = new CGRect(View.Frame.X, distanceFromTop, View.Frame.Width, View.Frame.Height);
            UIView.Animate(Animation.Timings.EnterTiming, View.LayoutIfNeeded);
        }
    }
}
