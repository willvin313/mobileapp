using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CoreGraphics;
using Foundation;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.UI.Helper;
using Toggl.Foundation.UI.ViewModels;
using Toggl.Multivac.Extensions;
using UIKit;
using Toggl.Foundation;

namespace Toggl.Daneel.ViewControllers
{
    [ModalCardPresentation]
    public sealed partial class EditDurationViewController
        : KeyboardAwareViewController<EditDurationViewModel>,
          IDismissableViewController,
          IUIGestureRecognizerDelegate
    {
        private const int additionalVerticalContentSize = 100;
        private const int stackViewSpacing = 26;

        private CompositeDisposable disposeBag = new CompositeDisposable();
        private CGRect frameBeforeShowingKeyboard;

        public EditDurationViewController() : base(nameof(EditDurationViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            StartLabel.Text = Resources.Start;
            EndLabel.Text = Resources.End;
            TitleLabel.Text = Resources.StartAndStopTime;
            SetEndButton.SetTitle(Resources.Stop, UIControlState.Normal);
            SaveButton.SetTitle(Resources.Save, UIControlState.Normal);

            prepareViews();

            // Actions
            SaveButton.Rx()
                .BindAction(ViewModel.Save)
                .DisposedBy(disposeBag);

            CloseButton.Rx()
                .BindAction(ViewModel.Close)
                .DisposedBy(disposeBag);

            // Start and stop date/time
            ViewModel.StartTimeString
                .Subscribe(StartTimeLabel.Rx().Text())
                .DisposedBy(disposeBag);

            ViewModel.StartDateString
                .Subscribe(StartDateLabel.Rx().Text())
                .DisposedBy(disposeBag);

            ViewModel.StopTimeString
                .Subscribe(EndTimeLabel.Rx().Text())
                .DisposedBy(disposeBag);

            ViewModel.StopDateString
                .Subscribe(EndDateLabel.Rx().Text())
                .DisposedBy(disposeBag);

            // Editing start and end time
            StartView.Rx()
                .BindAction(ViewModel.EditStartTime)
                .DisposedBy(disposeBag);

            EndView.Rx()
                .BindAction(ViewModel.EditStopTime)
                .DisposedBy(disposeBag);

            SetEndButton.Rx()
                .BindAction(ViewModel.StopTimeEntry)
                .DisposedBy(disposeBag);

            // Visibility
            ViewModel.IsRunning
                .Subscribe(running =>
                {
                    SetEndButton.Hidden = !running;
                    EndTimeLabel.Hidden = running;
                    EndDateLabel.Hidden = running;
                })
                .DisposedBy(disposeBag);

            // Stard and end colors
            ViewModel.IsEditingStartTime
                .Select(editingStartTime => editingStartTime
                    ? Color.EditDuration.EditedTime.ToNativeColor()
                    : Color.EditDuration.NotEditedTime.ToNativeColor()
                )
                .Subscribe(color =>
                {
                    StartTimeLabel.TextColor = color;
                    StartDateLabel.TextColor = color;
                })
                .DisposedBy(disposeBag);

            ViewModel.IsEditingStopTime
                .Select(editingStartTime => editingStartTime
                    ? Color.EditDuration.EditedTime.ToNativeColor()
                    : Color.EditDuration.NotEditedTime.ToNativeColor()
                )
                .Subscribe(color =>
                {
                    EndTimeLabel.TextColor = color;
                    EndDateLabel.TextColor = color;
                })
                .DisposedBy(disposeBag);

            // Date picker
            ViewModel.IsEditingTime
                .Subscribe(DatePickerContainer.Rx().AnimatedIsVisible())
                .DisposedBy(disposeBag);

            DatePicker.Rx().Date()
                .Subscribe(ViewModel.ChangeActiveTime.Inputs)
                .DisposedBy(disposeBag);

            var startTime = ViewModel.IsEditingStartTime
                    .Where(CommonFunctions.Identity)
                    .SelectMany(_ => ViewModel.StartTime);

            var stopTime = ViewModel.IsEditingStopTime
                    .Where(CommonFunctions.Identity)
                    .SelectMany(_ => ViewModel.StopTime);

            Observable.Merge(startTime, stopTime)
                .Subscribe(v => DatePicker.SetDate(v.ToNSDate(), false))
                .DisposedBy(disposeBag);

            ViewModel.IsEditingStartTime
                .Where(CommonFunctions.Identity)
                .SelectMany(_ => ViewModel.StartTime)
                .Subscribe(v => DatePicker.SetDate(v.ToNSDate(), false))
                .DisposedBy(disposeBag);

            ViewModel.MinimumDateTime
                .Subscribe(v => DatePicker.MinimumDate = v.ToNSDate())
                .DisposedBy(disposeBag);

            ViewModel.MaximumDateTime
                .Subscribe(v => DatePicker.MaximumDate = v.ToNSDate())
                .DisposedBy(disposeBag);

            ViewModel.TimeFormat
                .Subscribe(v => DatePicker.Locale = v.IsTwentyFourHoursFormat ? new NSLocale("en_GB") : new NSLocale("en_US"))
                .DisposedBy(disposeBag);

            // DurationInput

            ViewModel.IsEditingTime
                .Invert()
                .Subscribe(DurationInput.Rx().Enabled())
                .DisposedBy(disposeBag);

            ViewModel.Duration
                .Subscribe(v => DurationInput.Duration = v)
                .DisposedBy(disposeBag);

            ViewModel.DurationString
                .Subscribe(v => DurationInput.FormattedDuration = v)
                .DisposedBy(disposeBag);

            DurationInput.Rx().Duration()
                .Subscribe(ViewModel.ChangeDuration.Inputs)
                .DisposedBy(disposeBag);

            // The wheel

            ViewModel.IsEditingTime
                .Invert()
                .Subscribe(v => WheelView.UserInteractionEnabled = v)
                .DisposedBy(disposeBag);

            ViewModel.MinimumStartTime
                .Subscribe(v => WheelView.MinimumStartTime = v)
                .DisposedBy(disposeBag);

            ViewModel.MaximumStartTime
                .Subscribe(v => WheelView.MaximumStartTime = v)
                .DisposedBy(disposeBag);

            ViewModel.MinimumStopTime
                .Subscribe(v => WheelView.MinimumEndTime= v)
                .DisposedBy(disposeBag);

            ViewModel.MaximumStopTime
                .Subscribe(v => WheelView.MaximumEndTime = v)
                .DisposedBy(disposeBag);

            ViewModel.StartTime
                .Subscribe(v => WheelView.StartTime = v)
                .DisposedBy(disposeBag);

            ViewModel.StopTime
                .Subscribe(v => WheelView.EndTime = v)
                .DisposedBy(disposeBag);

            ViewModel.IsRunning
                .Subscribe(v => WheelView.IsRunning = v)
                .DisposedBy(disposeBag);

            WheelView.Rx().StartTime()
                .Subscribe(ViewModel.ChangeStartTime.Inputs)
                .DisposedBy(disposeBag);

            WheelView.Rx().EndTime()
                .Subscribe(ViewModel.ChangeStopTime.Inputs)
                .DisposedBy(disposeBag);

            // Interaction observables for analytics

            var editingStart = Observable.Merge(
                StartView.Rx().Tap().SelectValue(true),
                EndView.Rx().Tap().SelectValue(false)
            );

            var dateComponentChanged = DatePicker.Rx().DateComponent()
                .WithLatestFrom(editingStart,
                    (_, isStart) => isStart ? EditTimeSource.BarrelStartDate : EditTimeSource.BarrelStopDate
                 );

            var timeComponentChanged = DatePicker.Rx().TimeComponent()
                .WithLatestFrom(editingStart,
                    (_, isStart) => isStart ? EditTimeSource.BarrelStartTime : EditTimeSource.BarrelStopTime
                 );

            var durationInputChanged = DurationInput.Rx().Duration()
                .SelectValue(EditTimeSource.NumpadDuration);

            Observable.Merge(
                    dateComponentChanged,
                    timeComponentChanged,
                    WheelView.TimeEdited,
                    durationInputChanged
                )
                .Distinct()
                .Subscribe(ViewModel.TimeEditedWithSource)
                .DisposedBy(disposeBag);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            disposeBag?.Dispose();
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);

            if (ViewModel.IsDurationInitiallyFocused)
            {
                DurationInput.BecomeFirstResponder();
            }
        }

        public async Task<bool> Dismiss()
        {
            ViewModel.Close.Execute();
            return true;
        }

        protected override void KeyboardWillShow(object sender, UIKeyboardEventArgs e)
        {
            frameBeforeShowingKeyboard = View.Frame;

            var safeAreaOffset = UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
                  ? Math.Max(UIApplication.SharedApplication.KeyWindow.SafeAreaInsets.Top, UIApplication.SharedApplication.StatusBarFrame.Height)
                  : 0;
            var distanceFromTop = Math.Max(safeAreaOffset, View.Frame.Y - e.FrameEnd.Height);

            View.Frame = new CGRect(View.Frame.X, distanceFromTop, View.Frame.Width, View.Frame.Height);
            UIView.Animate(Animation.Timings.EnterTiming, () => View.LayoutIfNeeded());
        }

        protected override void KeyboardWillHide(object sender, UIKeyboardEventArgs e)
        {
            View.Frame = frameBeforeShowingKeyboard;
            UIView.Animate(Animation.Timings.EnterTiming, () => View.LayoutIfNeeded());
        }

        private void prepareViews()
        {
            var width = UIScreen.MainScreen.Bounds.Width;
            var height = width + additionalVerticalContentSize;

            PreferredContentSize = new CGSize
            {
                Width = width,
                Height = height
            };

            EndTimeLabel.Font = EndTimeLabel.Font.GetMonospacedDigitFont();
            StartTimeLabel.Font = StartTimeLabel.Font.GetMonospacedDigitFont();

            SetEndButton.TintColor = Color.EditDuration.SetButton.ToNativeColor();

            StackView.Spacing = stackViewSpacing;

            var backgroundTap = new UITapGestureRecognizer(onBackgroundTap);
            backgroundTap.Delegate = this;
            View.AddGestureRecognizer(backgroundTap);

            var editTimeTap = new UITapGestureRecognizer(onEditTimeTap);
            StartTimeLabel.AddGestureRecognizer(editTimeTap);
            EndTimeLabel.AddGestureRecognizer(editTimeTap);
        }

        private void onEditTimeTap(UITapGestureRecognizer recognizer)
        {
            if (DurationInput.IsEditing)
                DurationInput.ResignFirstResponder();
        }

        private void onBackgroundTap(UITapGestureRecognizer recognizer)
        {
            if (DurationInput.IsEditing)
                DurationInput.ResignFirstResponder();
                
            ViewModel.StopEditingTime.Execute();
        }

        [Export("gestureRecognizer:shouldReceiveTouch:")]
        public bool ShouldReceiveTouch(UIGestureRecognizer recognizer, UITouch touch)
        {
            if (touch.View.IsDescendantOfView(StartView) || touch.View.IsDescendantOfView(EndView))
                return false;
            return true;
        }
    }
}

