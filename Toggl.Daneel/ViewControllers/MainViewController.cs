﻿using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using CoreGraphics;
using Foundation;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Daneel.ExtensionKit;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Daneel.Presentation;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Daneel.Suggestions;
using Toggl.Daneel.Views;
using Toggl.Daneel.ViewSources;
using Toggl.Foundation;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.UI.Collections;
using Toggl.Foundation.UI.Extensions;
using Toggl.Foundation.UI.Helper;
using Toggl.Foundation.UI.Onboarding.MainView;
using Toggl.Foundation.UI.ViewModels;
using Toggl.Foundation.UI.ViewModels.TimeEntriesLog;
using Toggl.Foundation.UI.ViewModels.TimeEntriesLog.Identity;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Extensions;
using Toggl.PrimeRadiant.Onboarding;
using Toggl.PrimeRadiant.Settings;
using UIKit;
using static Toggl.Foundation.UI.Helper.Animation;

namespace Toggl.Daneel.ViewControllers
{
    using MainLogSection = AnimatableSectionModel<DaySummaryViewModel, LogItemViewModel, IMainLogKey>;

    [TabPresentation]
    public partial class MainViewController : ReactiveViewController<MainViewModel>, IScrollableToTop
    {
        private const float showCardDelay = 0.1f;

        private const float spiderHingeCornerRadius = 0.8f;
        private const float spiderHingeWidth = 16;
        private const float spiderHingeHeight = 2;
        private const float welcomeViewTopDistance = 239;
        private const float welcomeViewSideMargin = 16;

        private const float tooltipOffset = 7;

        private readonly UIView spiderContainerView = new UIView();
        private readonly SpiderOnARopeView spiderBroView = new SpiderOnARopeView();
        private readonly UIButton settingsButton = new UIButton(new CGRect(0, 0, 40, 50));
        private readonly UIButton syncFailuresButton = new UIButton(new CGRect(0, 0, 30, 40));
        private readonly UIImageView titleImage = new UIImageView(UIImage.FromBundle("togglLogo"));
        private readonly TimeEntriesEmptyLogView emptyStateView = TimeEntriesEmptyLogView.Create();

        private TimeEntriesLogViewCell firstTimeEntryCell;

        private bool viewInitialized;
        private CancellationTokenSource cardAnimationCancellation;

        private DismissableOnboardingStep tapToEditStep;
        private DismissableOnboardingStep swipeLeftStep;
        private DismissableOnboardingStep swipeRightStep;

        private UIGestureRecognizer swipeLeftGestureRecognizer;

        private CompositeDisposable disposeBag = new CompositeDisposable();

        private IDisposable swipeLeftAnimationDisposable;
        private IDisposable swipeRightAnimationDisposable;

        private readonly UIView tableHeader = new UIView();
        private readonly UIView suggestionsContaier = new UIView { TranslatesAutoresizingMaskIntoConstraints = false };
        private readonly UIView ratingViewContainer = new UIView { TranslatesAutoresizingMaskIntoConstraints = false };
        private readonly SuggestionsView suggestionsView = new SuggestionsView { TranslatesAutoresizingMaskIntoConstraints = false };

        private TimeEntriesLogViewSource tableViewSource;

        private SnackBar snackBar;
        private RatingView ratingView;

        public MainViewController()
            : base(nameof(MainViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            SwipeRightBubbleLabel.Text = Resources.SwipeRightToContinue;
            SwipeLeftBubbleLabel.Text = Resources.SwipeLeftToDelete;
            WelcomeBackLabel.Text = Resources.LogEmptyStateTitle;
            WelcomeBackDescriptionLabel.Text = Resources.LogEmptyStateText;
            CreatedFirstTimeEntryLabel.Text = Resources.YouHaveCreatedYourFirstTimeEntry;
            TapToEditItLabel.Text = Resources.TapToEditIt;
            StartTimerBubbleLabel.Text = Resources.TapToStartTimer;
            TapToStopTimerLabel.Text = Resources.TapToStopTimer;
            FeedbackSentSuccessTitleLabel.Text = Resources.DoneWithExclamationMark.ToUpper();
            FeedbackSentDescriptionLabel.Text = Resources.ThankYouForTheFeedback;

            prepareViews();
            prepareOnboarding();
            setupTableViewHeader();

            tableViewSource = new TimeEntriesLogViewSource();

            TimeEntriesLogTableView.Source = tableViewSource;

            ViewModel.TimeEntries
                .Subscribe(TimeEntriesLogTableView.Rx().AnimateSections<MainLogSection, DaySummaryViewModel, LogItemViewModel, IMainLogKey>(tableViewSource))
                .DisposedBy(disposeBag);

            ViewModel.ShouldReloadTimeEntryLog
                .WithLatestFrom(ViewModel.TimeEntries, (_, timeEntries) => timeEntries)
                .Subscribe(TimeEntriesLogTableView.Rx().ReloadSections(tableViewSource))
                .DisposedBy(disposeBag);

            tableViewSource.ToggleGroupExpansion
                .Subscribe(ViewModel.TimeEntriesViewModel.ToggleGroupExpansion.Inputs)
                .DisposedBy(disposeBag);

            tableViewSource.FirstCell
                .Subscribe(f =>
                {
                    onFirstTimeEntryChanged(f);
                    firstTimeEntryCell = f;
                })
                .DisposedBy(DisposeBag);

            tableViewSource.Rx().Scrolled()
                .Subscribe(onTableScroll)
                .DisposedBy(DisposeBag);

            tableViewSource.ContinueTap
                .Select(item => timeEntryContinuation(item, false))
                .Subscribe(ViewModel.ContinueTimeEntry.Inputs)
                .DisposedBy(DisposeBag);

            tableViewSource.SwipeToContinue
                .Select(item => timeEntryContinuation(item, true))
                .Subscribe(ViewModel.ContinueTimeEntry.Inputs)
                .DisposedBy(DisposeBag);

            tableViewSource.SwipeToDelete
                .Select(logItem => logItem.RepresentedTimeEntriesIds)
                .Subscribe(ViewModel.TimeEntriesViewModel.DelayDeleteTimeEntries.Inputs)
                .DisposedBy(DisposeBag);

            tableViewSource.Rx().ModelSelected()
                .Select(editEventInfo)
                .Subscribe(ViewModel.SelectTimeEntry.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.TimeEntriesViewModel.TimeEntriesPendingDeletion
                .Subscribe(toggleUndoDeletion)
                .DisposedBy(DisposeBag);

            tableViewSource.SwipeToContinue
                .Subscribe(_ => swipeRightStep.Dismiss())
                .DisposedBy(disposeBag);

            tableViewSource.SwipeToDelete
                .Subscribe(_ => swipeLeftStep.Dismiss())
                .DisposedBy(disposeBag);

            // Refresh Control
            var refreshControl = new RefreshControl(
                ViewModel.SyncProgressState,
                tableViewSource.Rx().Scrolled(),
                tableViewSource.IsDragging);
            refreshControl.Refresh
                .Subscribe(ViewModel.Refresh.Inputs)
                .DisposedBy(DisposeBag);
            TimeEntriesLogTableView.CustomRefreshControl = refreshControl;

            //Actions
            settingsButton.Rx().BindAction(ViewModel.OpenSettings).DisposedBy(DisposeBag);
            syncFailuresButton.Rx().BindAction(ViewModel.OpenSyncFailures).DisposedBy(DisposeBag);
            StopTimeEntryButton.Rx().BindAction(ViewModel.StopTimeEntry, _ => TimeEntryStopOrigin.Manual).DisposedBy(DisposeBag);

            StartTimeEntryButton.Rx().BindAction(ViewModel.StartTimeEntry, _ => true).DisposedBy(DisposeBag);
            StartTimeEntryButton.Rx().BindAction(ViewModel.StartTimeEntry, _ => false, ButtonEventType.LongPress).DisposedBy(DisposeBag);

            CurrentTimeEntryCard.Rx().Tap()
                .WithLatestFrom(ViewModel.CurrentRunningTimeEntry, (_, te) => te)
                .Where(te => te != null)
                .Select(te => (new[] { te.Id }, EditTimeEntryOrigin.RunningTimeEntryCard))
                .Subscribe(ViewModel.SelectTimeEntry.Inputs)
                .DisposedBy(DisposeBag);

            //Visibility
            var shouldWelcomeBack = ViewModel.ShouldShowWelcomeBack;

            ViewModel.ShouldShowEmptyState
                .Subscribe(visible => emptyStateView.Hidden = !visible)
                .DisposedBy(DisposeBag);

            shouldWelcomeBack
                .Subscribe(WelcomeBackView.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            shouldWelcomeBack
                .Subscribe(spiderContainerView.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            shouldWelcomeBack
                .Subscribe(visible =>
                {
                    if (visible)
                        spiderBroView.Show();
                    else
                        spiderBroView.Hide();
                })
                .DisposedBy(DisposeBag);

            //Text
            ViewModel.CurrentRunningTimeEntry
                .Select(te => te?.Description)
                .Subscribe(CurrentTimeEntryDescriptionLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            ViewModel.ElapsedTime
                .Subscribe(CurrentTimeEntryElapsedTimeLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            var capHeight = CurrentTimeEntryProjectTaskClientLabel.Font.CapHeight;
            var clientColor = Color.Main.CurrentTimeEntryClientColor.ToNativeColor();
            ViewModel.CurrentRunningTimeEntry
                .Select(te => te?.ToFormattedTimeEntryString(capHeight, clientColor, shouldColorProject: true))
                .Subscribe(CurrentTimeEntryProjectTaskClientLabel.Rx().AttributedText())
                .DisposedBy(DisposeBag);

            //The start button
            var trackModeImage = UIImage.FromBundle("playIcon");
            var manualModeImage = UIImage.FromBundle("manualIcon");
            ViewModel.IsInManualMode
                .Select(isInManualMode => isInManualMode ? manualModeImage : trackModeImage)
                .Subscribe(image => StartTimeEntryButton.SetImage(image, UIControlState.Normal))
                .DisposedBy(DisposeBag);

            //The sync failures button
            ViewModel.NumberOfSyncFailures
                .Select(numberOfSyncFailures => numberOfSyncFailures > 0)
                .Subscribe(syncFailuresButton.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.RatingViewModel.IsFeedbackSuccessViewShowing
                .Subscribe(SendFeedbackSuccessView.Rx().AnimatedIsVisible())
                .DisposedBy(DisposeBag);

            SendFeedbackSuccessView.Rx().Tap()
                .Subscribe(ViewModel.RatingViewModel.CloseFeedbackSuccessView)
                .DisposedBy(DisposeBag);

            ViewModel.ShouldShowRatingView
                .Subscribe(showHideRatingView)
                .DisposedBy(disposeBag);

            // Suggestion View
            suggestionsView.SuggestionTapped
                .Subscribe(ViewModel.SuggestionsViewModel.StartTimeEntry.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.SuggestionsViewModel.IsEmpty.Invert()
                .Subscribe(suggestionsView.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.SuggestionsViewModel.Suggestions
                .Subscribe(suggestionsView.OnSuggestions)
                .DisposedBy(DisposeBag);

            View.SetNeedsLayout();
            View.LayoutIfNeeded();

            NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.DidBecomeActiveNotification, onApplicationDidBecomeActive);
        }

        private void setupTableViewHeader()
        {
            TimeEntriesLogTableView.TableHeaderView = tableHeader;

            tableHeader.TranslatesAutoresizingMaskIntoConstraints = false;
            tableHeader.WidthAnchor.ConstraintEqualTo(TimeEntriesLogTableView.WidthAnchor).Active = true;

            tableHeader.AddSubview(suggestionsContaier);
            tableHeader.AddSubview(ratingViewContainer);

            suggestionsContaier.ConstrainToViewSides(tableHeader);
            ratingViewContainer.ConstrainToViewSides(tableHeader);

            suggestionsContaier.TopAnchor.ConstraintEqualTo(tableHeader.TopAnchor, TimeEntriesLogViewSource.SpaceBetweenSections).Active = true;
            suggestionsContaier.BottomAnchor.ConstraintEqualTo(ratingViewContainer.TopAnchor, TimeEntriesLogViewSource.SpaceBetweenSections).Active = true;
            ratingViewContainer.BottomAnchor.ConstraintEqualTo(tableHeader.BottomAnchor).Active = true;

            suggestionsContaier.AddSubview(suggestionsView);
            suggestionsView.ConstrainInView(suggestionsContaier);
        }

        private (long[], EditTimeEntryOrigin) editEventInfo(LogItemViewModel item)
        {
            var origin = item.IsTimeEntryGroupHeader
                ? EditTimeEntryOrigin.GroupHeader
                : item.BelongsToGroup
                    ? EditTimeEntryOrigin.GroupTimeEntry
                    : EditTimeEntryOrigin.SingleTimeEntry;

            return (item.RepresentedTimeEntriesIds, origin);
        }

        private (long, ContinueTimeEntryMode) timeEntryContinuation(LogItemViewModel itemViewModel, bool isSwipe)
        {
            var continueMode = default(ContinueTimeEntryMode);

            if (isSwipe)
            {
                continueMode = itemViewModel.IsTimeEntryGroupHeader
                    ? ContinueTimeEntryMode.TimeEntriesGroupSwipe
                    : ContinueTimeEntryMode.SingleTimeEntrySwipe;
            }
            else
            {
                continueMode = itemViewModel.IsTimeEntryGroupHeader
                    ? ContinueTimeEntryMode.TimeEntriesGroupContinueButton
                    : ContinueTimeEntryMode.SingleTimeEntryContinueButton;
            }

            return (itemViewModel.RepresentedTimeEntriesIds.First(), continueMode);
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            NavigationItem.TitleView = titleImage;
            NavigationItem.RightBarButtonItems = new[]
            {
                new UIBarButtonItem(settingsButton)
            };

#if DEBUG
            NavigationItem.LeftBarButtonItems = new[]
            {
                new UIBarButtonItem(syncFailuresButton)
            };
#endif
        }

        private void trackSiriEvents()
        {
            var events = SharedStorage.instance.PopTrackableEvents();

            events?
                .Select(e => e?.ToTrackableEvent())
                .Where(e => e != null)
                .Do(ViewModel.Track);
        }

        private void onApplicationDidBecomeActive(NSNotification notification)
        {
            if (SharedStorage.instance.GetNeedsSync())
            {
                SharedStorage.instance.SetNeedsSync(false);
                ViewModel.Refresh.Execute();
            }
            trackSiriEvents();
        }

        private void toggleUndoDeletion(int? numberOfTimeEntriesPendingDeletion)
        {
            if (snackBar != null)
            {
                snackBar.Hide();
                snackBar = null;
            }

            if (!numberOfTimeEntriesPendingDeletion.HasValue)
                return;

            var undoText = numberOfTimeEntriesPendingDeletion > 1
                ? String.Format(Resources.MultipleEntriesDeleted, numberOfTimeEntriesPendingDeletion)
                : Resources.EntryDeleted;

            snackBar = SnackBar.Factory.CreateUndoSnackBar(
                onUndo: () => ViewModel.TimeEntriesViewModel.CancelDeleteTimeEntry.Execute(Unit.Default),
                text: undoText);

            snackBar.SnackBottomAnchor = StartTimeEntryButton.TopAnchor;
            snackBar.Show(superView: View);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            spiderBroView.Dispose();
            ViewModel.NavigationService.AfterNavigate -= onNavigate;

            disposeBag?.Dispose();
            disposeBag = null;
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();

            TimeEntriesLogTableView.ContentInset = new UIEdgeInsets(-TimeEntriesLogViewSource.SpaceBetweenSections, 0,
                StartTimeEntryButton.Frame.Height, 0);
            TimeEntriesLogTableView.BringSubviewToFront(TimeEntriesLogTableView.TableHeaderView);

            if (TimeEntriesLogTableView.TableHeaderView != null)
            {
                var header = TimeEntriesLogTableView.TableHeaderView;
                var size = header.SystemLayoutSizeFittingSize(UIView.UILayoutFittingCompressedSize);
                if (header.Frame.Size.Height != size.Height)
                {
                    var headerRect = new CGRect
                    {
                        X = header.Frame.X,
                        Y = header.Frame.Y,
                        Width = header.Frame.Width,
                        Height = size.Height
                    };
                    header.Frame = headerRect;
                }
                TimeEntriesLogTableView.TableHeaderView = header;
                TimeEntriesLogTableView.SetNeedsLayout();
            }

            if (viewInitialized) return;

            viewInitialized = true;

            ViewModel.IsTimeEntryRunning
                .Where(visible => visible)
                .Subscribe(_ => showTimeEntryCard())
                .DisposedBy(disposeBag);

            ViewModel.IsTimeEntryRunning
                .Where(visible => !visible)
                .Subscribe(_ => hideTimeEntryCard())
                .DisposedBy(disposeBag);
        }

        public void ScrollToTop()
        {
            TimeEntriesLogTableView.SetContentOffset(CGPoint.Empty, true);
        }

        private void showHideRatingView(bool shouldShow)
        {
            if (shouldShow)
            {
                showRatingView();
                return;
            }

            hideRatingView();
        }

        private void showRatingView()
        {
            ratingView = RatingView.Create();
            ratingView.TranslatesAutoresizingMaskIntoConstraints = false;
            ratingView.DataContext = ViewModel.RatingViewModel;
            ratingViewContainer.AddSubview(ratingView);
            ratingView.ConstrainInView(ratingViewContainer);
            View.SetNeedsLayout();
        }

        private void hideRatingView()
        {
            if (ratingView == null) return;

            ratingView.RemoveFromSuperview();
            ratingView.Dispose();
            ratingView = null;

            View.SetNeedsLayout();
        }

        private void prepareViews()
        {
            //Prevent bounces in UIScrollView
            AutomaticallyAdjustsScrollViewInsets = false;

            //Card border
            CurrentTimeEntryCard.Layer.CornerRadius = 8;
            CurrentTimeEntryCard.Layer.ShadowColor = UIColor.Black.CGColor;
            CurrentTimeEntryCard.Layer.ShadowOffset = new CGSize(0, -2);
            CurrentTimeEntryCard.Layer.ShadowOpacity = 0.1f;
            CurrentTimeEntryElapsedTimeLabel.Font = CurrentTimeEntryElapsedTimeLabel.Font.GetMonospacedDigitFont();

            // Card animations
            StopTimeEntryButton.Hidden = true;
            CurrentTimeEntryCard.Hidden = true;

            //Hide play button for later animating it
            StartTimeEntryButton.Transform = CGAffineTransform.MakeScale(0.01f, 0.01f);

            //Prepare Navigation bar images
            settingsButton.SetImage(UIImage.FromBundle("icSettings"), UIControlState.Normal);
            syncFailuresButton.SetImage(UIImage.FromBundle("icWarning"), UIControlState.Normal);

            RunningEntryDescriptionFadeView.FadeLeft = true;
            RunningEntryDescriptionFadeView.FadeRight = true;

            // Send Feedback Success View Setup
            SendFeedbackSuccessView.Hidden = true;

            prepareWelcomeBackViews();
            prepareEmptyStateView();

            View.BackgroundColor = Color.Main.BackgroundColor.ToNativeColor();

            // Open edit view for the currently running time entry by swiping up
            var swipeUpRunningCardGesture = new UISwipeGestureRecognizer(async () =>
            {
                var currentlyRunningTimeEntry = await ViewModel.CurrentRunningTimeEntry.FirstAsync();
                if (currentlyRunningTimeEntry == null)
                    return;

                var selectTimeEntryData = (new[] { currentlyRunningTimeEntry.Id }, EditTimeEntryOrigin.RunningTimeEntryCard);
                await ViewModel.SelectTimeEntry.ExecuteWithCompletion(selectTimeEntryData);
            });
            swipeUpRunningCardGesture.Direction = UISwipeGestureRecognizerDirection.Up;
            CurrentTimeEntryCard.AddGestureRecognizer(swipeUpRunningCardGesture);
        }

        private void showTimeEntryCard()
        {
            StopTimeEntryButton.Hidden = false;
            CurrentTimeEntryCard.Hidden = false;

            cardAnimationCancellation?.Cancel();
            cardAnimationCancellation = new CancellationTokenSource();

            TimeEntriesLogTableViewBottomToTopCurrentEntryConstraint.Active = true;

            AnimationExtensions.Animate(Timings.EnterTiming, showCardDelay, Curves.EaseOut,
                () => StartTimeEntryButton.Transform = CGAffineTransform.MakeScale(0.01f, 0.01f),
                () =>
                {
                    AnimationExtensions.Animate(Timings.LeaveTimingFaster, Curves.EaseIn,
                        () => StopTimeEntryButton.Transform = CGAffineTransform.MakeScale(1.0f, 1.0f),
                        cancellationToken: cardAnimationCancellation.Token);

                    AnimationExtensions.Animate(Timings.LeaveTiming, Curves.CardOutCurve,
                        () => CurrentTimeEntryCard.Transform = CGAffineTransform.MakeTranslation(0, 0),
                        cancellationToken: cardAnimationCancellation.Token);
                },
                cancellationToken: cardAnimationCancellation.Token);
        }

        private void hideTimeEntryCard()
        {
            cardAnimationCancellation?.Cancel();
            cardAnimationCancellation = new CancellationTokenSource();

            TimeEntriesLogTableViewBottomToTopCurrentEntryConstraint.Active = false;

            AnimationExtensions.Animate(Timings.LeaveTimingFaster, Curves.EaseIn,
                () => StopTimeEntryButton.Transform = CGAffineTransform.MakeScale(0.01f, 0.01f),
                () => StopTimeEntryButton.Hidden = true,
                cancellationToken: cardAnimationCancellation.Token);

            AnimationExtensions.Animate(Timings.LeaveTiming, Curves.CardOutCurve,
                () => CurrentTimeEntryCard.Transform = CGAffineTransform.MakeTranslation(0, CurrentTimeEntryCard.Frame.Height),
                () =>
                {
                    CurrentTimeEntryCard.Hidden = true;

                    AnimationExtensions.Animate(Timings.EnterTiming, Curves.EaseOut,
                        () => StartTimeEntryButton.Transform = CGAffineTransform.MakeScale(1f, 1f),
                        cancellationToken: cardAnimationCancellation.Token);
                },
                cancellationToken: cardAnimationCancellation.Token);
        }

        //Spider is added in code, because IB doesn't allow adding subviews
        //to a UITableView and the spider needs to be a subview of the table
        //view so it reacts to pulling down to refresh
        private void prepareWelcomeBackViews()
        {
            // Welcome back view must be placed inside of the time entries
            // log table view below the spider so that it does not overlay
            // the spider at any time.
            WelcomeBackView.RemoveFromSuperview();
            TimeEntriesLogTableView.AddSubview(WelcomeBackView);
            NSLayoutConstraint.ActivateConstraints(new[]
            {
                WelcomeBackView.CenterXAnchor.ConstraintEqualTo(TimeEntriesLogTableView.CenterXAnchor),
                WelcomeBackView.TopAnchor.ConstraintEqualTo(TimeEntriesLogTableView.TopAnchor, welcomeViewTopDistance),
                WelcomeBackView.LeadingAnchor.ConstraintEqualTo(TimeEntriesLogTableView.LeadingAnchor, welcomeViewSideMargin),
                WelcomeBackView.TrailingAnchor.ConstraintEqualTo(TimeEntriesLogTableView.TrailingAnchor, welcomeViewSideMargin)
            });

            var spiderHinge = new UIView();

            spiderHinge.Layer.CornerRadius = spiderHingeCornerRadius;
            spiderHinge.TranslatesAutoresizingMaskIntoConstraints = false;
            spiderHinge.BackgroundColor = Color.Main.SpiderHinge.ToNativeColor();
            spiderContainerView.TranslatesAutoresizingMaskIntoConstraints = false;
            spiderBroView.TranslatesAutoresizingMaskIntoConstraints = false;
            spiderContainerView.BackgroundColor = UIColor.Clear;

            spiderContainerView.AddSubview(spiderHinge);
            spiderContainerView.AddSubview(spiderBroView);
            TimeEntriesLogTableView.AddSubview(spiderContainerView);

            //Container constraints
            spiderContainerView.WidthAnchor.ConstraintEqualTo(TimeEntriesLogTableView.WidthAnchor).Active = true;
            spiderContainerView.HeightAnchor.ConstraintEqualTo(TimeEntriesLogTableView.HeightAnchor).Active = true;
            spiderContainerView.CenterYAnchor.ConstraintEqualTo(TimeEntriesLogTableView.CenterYAnchor).Active = true;
            spiderContainerView.CenterXAnchor.ConstraintEqualTo(TimeEntriesLogTableView.CenterXAnchor).Active = true;

            //Hinge constraints
            spiderHinge.WidthAnchor.ConstraintEqualTo(spiderHingeWidth).Active = true;
            spiderHinge.HeightAnchor.ConstraintEqualTo(spiderHingeHeight).Active = true;
            spiderHinge.TopAnchor.ConstraintEqualTo(spiderContainerView.TopAnchor).Active = true;
            spiderHinge.CenterXAnchor.ConstraintEqualTo(spiderContainerView.CenterXAnchor).Active = true;

            //Spider constraints
            spiderBroView.TopAnchor.ConstraintEqualTo(spiderContainerView.TopAnchor).Active = true;
            spiderBroView.WidthAnchor.ConstraintEqualTo(spiderContainerView.WidthAnchor).Active = true;
            spiderBroView.BottomAnchor.ConstraintEqualTo(spiderContainerView.BottomAnchor).Active = true;
            spiderBroView.CenterXAnchor.ConstraintEqualTo(spiderContainerView.CenterXAnchor).Active = true;
        }

        private void prepareEmptyStateView()
        {
            emptyStateView.BackgroundColor = UIColor.Clear;
            emptyStateView.TranslatesAutoresizingMaskIntoConstraints = false;

            TimeEntriesLogTableView.AddSubview(emptyStateView);

            emptyStateView.WidthAnchor.ConstraintEqualTo(TimeEntriesLogTableView.WidthAnchor).Active = true;
            emptyStateView.HeightAnchor.ConstraintEqualTo(TimeEntriesLogTableView.HeightAnchor).Active = true;
            emptyStateView.CenterYAnchor.ConstraintEqualTo(TimeEntriesLogTableView.CenterYAnchor).Active = true;
            emptyStateView.TopAnchor.ConstraintEqualTo(TimeEntriesLogTableView.TopAnchor).Active = true;
        }

        private void prepareOnboarding()
        {
            var storage = ViewModel.OnboardingStorage;

            var timelineIsEmpty = ViewModel.LogEmpty;

            new StartTimeEntryOnboardingStep(storage)
                .ManageDismissableTooltip(StartTimeEntryOnboardingBubbleView, storage)
                .DisposedBy(disposeBag);

            new StopTimeEntryOnboardingStep(storage, ViewModel.IsTimeEntryRunning)
                .ManageDismissableTooltip(StopTimeEntryOnboardingBubbleView, storage)
                .DisposedBy(disposeBag);

            tapToEditStep = new EditTimeEntryOnboardingStep(storage, timelineIsEmpty)
                .ToDismissable(nameof(EditTimeEntryOnboardingStep), storage);

            tapToEditStep.DismissByTapping(TapToEditBubbleView);
            tapToEditStep.ManageVisibilityOf(TapToEditBubbleView).DisposedBy(disposeBag);

            prepareSwipeGesturesOnboarding(storage, tapToEditStep.ShouldBeVisible);

            ViewModel.NavigationService.AfterNavigate += onNavigate;
        }

        private void prepareSwipeGesturesOnboarding(IOnboardingStorage storage, IObservable<bool> tapToEditStepIsVisible)
        {
            var timeEntriesCount = ViewModel.TimeEntriesCount;

            var swipeRightCanBeShown =
                UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
                    ? tapToEditStepIsVisible.Select(isVisible => !isVisible)
                    : Observable.Return(false);

            swipeRightStep = new SwipeRightOnboardingStep(swipeRightCanBeShown, timeEntriesCount)
                .ToDismissable(nameof(SwipeRightOnboardingStep), storage);

            var swipeLeftCanBeShown = Observable.CombineLatest(
                tapToEditStepIsVisible,
                swipeRightStep.ShouldBeVisible,
                (tapToEditIsVisible, swipeRightIsVisble) => !tapToEditIsVisible && !swipeRightIsVisble);
            swipeLeftStep = new SwipeLeftOnboardingStep(swipeLeftCanBeShown, timeEntriesCount)
                .ToDismissable(nameof(SwipeLeftOnboardingStep), storage);

            swipeLeftStep.DismissByTapping(SwipeLeftBubbleView);
            swipeLeftStep.ManageVisibilityOf(SwipeLeftBubbleView).DisposedBy(disposeBag);
            swipeLeftAnimationDisposable = swipeLeftStep.ManageSwipeActionAnimationOf(firstTimeEntryCell, Direction.Left);

            swipeRightStep.DismissByTapping(SwipeRightBubbleView);
            swipeRightStep.ManageVisibilityOf(SwipeRightBubbleView).DisposedBy(disposeBag);
            swipeRightAnimationDisposable = swipeRightStep.ManageSwipeActionAnimationOf(firstTimeEntryCell, Direction.Right);

            updateSwipeDismissGestures(firstTimeEntryCell);
        }

        private void onTableScroll(CGPoint offset)
        {
            updateTooltipPositions();
        }

        private void onFirstTimeEntryChanged(TimeEntriesLogViewCell nextFirstTimeEntry)
        {
            updateSwipeDismissGestures(nextFirstTimeEntry);
            firstTimeEntryCell = nextFirstTimeEntry;
            updateTooltipPositions();
        }

        private void onNavigate(object sender, EventArgs e)
        {
            bool isHidden = false;
            InvokeOnMainThread(() => isHidden = TapToEditBubbleView.Hidden);

            if (isHidden == false)
            {
                tapToEditStep.Dismiss();
                ViewModel.NavigationService.AfterNavigate -= onNavigate;
            }
        }

        private void updateTooltipPositions()
        {
            if (TapToEditBubbleView.Hidden && SwipeLeftBubbleView.Hidden && SwipeRightBubbleView.Hidden) return;
            if (firstTimeEntryCell == null) return;

            var position = TimeEntriesLogTableView.ConvertRectToView(
                firstTimeEntryCell.Frame, TimeEntriesLogTableView.Superview);

            TapToEditBubbleViewTopConstraint.Constant = position.Bottom + tooltipOffset;
            SwipeLeftTopConstraint.Constant = position.Y - SwipeLeftBubbleView.Frame.Height - tooltipOffset;
            SwipeRightTopConstraint.Constant = position.Y - SwipeRightBubbleView.Frame.Height - tooltipOffset;
        }

        private void updateSwipeDismissGestures(TimeEntriesLogViewCell nextFirstTimeEntry)
        {
            if (swipeLeftGestureRecognizer != null)
            {
                firstTimeEntryCell?.RemoveGestureRecognizer(swipeLeftGestureRecognizer);
            }

            swipeLeftAnimationDisposable?.Dispose();
            swipeRightAnimationDisposable?.Dispose();

            if (nextFirstTimeEntry == null) return;

            swipeLeftAnimationDisposable = swipeLeftStep.ManageSwipeActionAnimationOf(nextFirstTimeEntry, Direction.Left);
            swipeRightAnimationDisposable = swipeRightStep.ManageSwipeActionAnimationOf(nextFirstTimeEntry, Direction.Right);

            swipeLeftGestureRecognizer = swipeLeftStep.DismissBySwiping(nextFirstTimeEntry, Direction.Left);
        }
    }
}
