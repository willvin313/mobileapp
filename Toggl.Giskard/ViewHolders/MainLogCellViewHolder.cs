using System;
using System.Reactive.Subjects;
using Android.Runtime;
using System.Linq;
using Android.Animation;
using Android.Support.Constraints;
using Android.Text;
using Android.Views;
using Android.Widget;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.ViewModels.TimeEntriesLog;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.ViewHelpers;
using static Toggl.Giskard.Resource.Id;
using MvvmCross.Plugin.Color.Platforms.Android;
using Android.Graphics.Drawables;
using GroupingColor = Toggl.Foundation.MvvmCross.Helper.Color.TimeEntriesLog.Grouping;
using Android.Graphics;
using System.Reactive;
using Toggl.Giskard.Extensions.Reactive;
using System.Reactive.Disposables;
using Android.Support.V4.Content;
using Toggl.Multivac.Extensions;
using System.Reactive.Linq;
using Toggl.Foundation.Analytics;

namespace Toggl.Giskard.ViewHolders
{
    public class MainLogCellViewHolder : BaseRecyclerViewHolder<TimeEntryViewData>
    {
        public enum AnimationSide
        {
            Left,
            Right
        }

        public MainLogCellViewHolder(View itemView) : base(itemView)
        {
        }

        public MainLogCellViewHolder(IntPtr handle, JniHandleOwnership ownership) : base(handle, ownership)
        {
        }

        private static readonly int animationDuration = 1000;

        private TextView timeEntriesLogCellDescription;
        private TextView addDescriptionLabel;
        private TextView timeEntriesLogCellProjectLabel;
        private TextView timeEntriesLogCellDuration;
        private View groupItemBackground;
        private View timeEntriesLogCellContinueImage;
        private View errorImageView;
        private View errorNeedsSync;
        private View timeEntriesLogCellContinueButton;
        private View mainLogBackgroundContinue;
        private View mainLogBackgroundDelete;
        private View billableIcon;
        private View hasTagsIcon;
        private View durationPadding;
        private View durationFadeGradient;
        private TextView groupCountTextView;
        private View groupExpansionButton;
        
        private ObjectAnimator animator;

        public bool IsAnimating => animator?.IsRunning ?? false;

        public bool CanSync => Item.ViewModel.CanContinue;

        public View MainLogContentView { get; private set; }
        public Subject<(LogItemViewModel, ContinueTimeEntryMode)> ContinueButtonTappedSubject { get; set; }
        public Subject<GroupId> ToggleGroupExpansionSubject { get; set; }

        private GroupId groupId;

        private Color whiteColor;
        private Color grayColor;

        protected override void InitializeViews()
        {
            whiteColor = new Color(ItemView.Context.GetColor(Resource.Color.mainLogCellPaddingWhite));
            grayColor = new Color(ItemView.Context.GetColor(Resource.Color.mainLogCellPaddingLightGray));

            groupItemBackground = ItemView.FindViewById<View>(MainLogGroupBackground);
            groupCountTextView = ItemView.FindViewById<TextView>(TimeEntriesLogCellGroupCount);

            timeEntriesLogCellDescription = ItemView.FindViewById<TextView>(TimeEntriesLogCellDescription);
            addDescriptionLabel = ItemView.FindViewById<TextView>(AddDescriptionLabel);
            timeEntriesLogCellProjectLabel = ItemView.FindViewById<TextView>(TimeEntriesLogCellProjectLabel);
            timeEntriesLogCellDuration = ItemView.FindViewById<TextView>(TimeEntriesLogCellDuration);
            timeEntriesLogCellContinueImage = ItemView.FindViewById(TimeEntriesLogCellContinueImage);
            errorImageView = ItemView.FindViewById(ErrorImageView);
            errorNeedsSync = ItemView.FindViewById(ErrorNeedsSync);
            timeEntriesLogCellContinueButton = ItemView.FindViewById(TimeEntriesLogCellContinueButton);
            mainLogBackgroundContinue = ItemView.FindViewById(MainLogBackgroundContinue);
            mainLogBackgroundDelete = ItemView.FindViewById(MainLogBackgroundDelete);
            billableIcon = ItemView.FindViewById(TimeEntriesLogCellBillable);
            hasTagsIcon = ItemView.FindViewById(TimeEntriesLogCellTags);
            durationPadding = ItemView.FindViewById(TimeEntriesLogCellDurationPaddingArea);
            durationFadeGradient = ItemView.FindViewById(TimeEntriesLogCellDurationGradient);
            MainLogContentView = ItemView.FindViewById(Resource.Id.MainLogContentView);

            groupExpansionButton = ItemView.FindViewById(TimeEntriesLogCellToggleExpansionButton);
            timeEntriesLogCellContinueButton.Click += onContinueClick;
            groupExpansionButton.Click += onExpansionClick;
        }

        private void onExpansionClick(object sender, EventArgs e)
        {
            ToggleGroupExpansionSubject.OnNext(groupId);
        }

        public void ShowSwipeToContinueBackground()
        {
            StopAnimating();
            mainLogBackgroundContinue.Visibility = ViewStates.Visible;
            mainLogBackgroundDelete.Visibility = ViewStates.Invisible;
        }

        public void ShowSwipeToDeleteBackground()
        {
            StopAnimating();
            mainLogBackgroundContinue.Visibility = ViewStates.Invisible;
            mainLogBackgroundDelete.Visibility = ViewStates.Visible;
        }

        public void HideSwipeBackgrounds()
        {
            StopAnimating();
            mainLogBackgroundContinue.Visibility = ViewStates.Invisible;
            mainLogBackgroundDelete.Visibility = ViewStates.Invisible;
        }

        private void onContinueClick(object sender, EventArgs e)
        {
            var continueMode = Item.ViewModel.IsTimeEntryGroupHeader
                ? ContinueTimeEntryMode.TimeEntriesGroupContinueButton
                : ContinueTimeEntryMode.SingleTimeEntryContinueButton;

            ContinueButtonTappedSubject?.OnNext((Item.ViewModel, ContinueTimeEntryMode.SingleTimeEntryContinueButton));
        }

        private ConstraintLayout.LayoutParams getDurationPaddingWidthDependentOnIcons()
        {
            var whitePaddingWidth =
                72
                + (Item.ViewModel.IsBillable ? 22 : 0)
                + (Item.ViewModel.HasTags ? 22 : 0);

            var layoutParameters = (ConstraintLayout.LayoutParams)durationPadding.LayoutParameters;
            layoutParameters.Width = whitePaddingWidth.DpToPixels(ItemView.Context);
            return layoutParameters;
        }

        protected override void UpdateView()
        {
            StopAnimating();

            groupId = Item.ViewModel.GroupId;

            timeEntriesLogCellDescription.Text = Item.ViewModel.Description;
            timeEntriesLogCellDescription.Visibility = Item.DescriptionVisibility;
            addDescriptionLabel.Visibility = Item.AddDescriptionLabelVisibility;

            timeEntriesLogCellProjectLabel.TextFormatted = Item.ProjectTaskClientText;
            timeEntriesLogCellProjectLabel.Visibility = Item.ProjectTaskClientVisibility;

            timeEntriesLogCellDuration.Text = Item.ViewModel.Duration;

            timeEntriesLogCellContinueImage.Visibility = Item.ContinueImageVisibility;
            errorImageView.Visibility = Item.ErrorImageViewVisibility;
            errorNeedsSync.Visibility = Item.ErrorNeedsSyncVisibility;
            timeEntriesLogCellContinueButton.Visibility = Item.ContinueButtonVisibility;
            billableIcon.Visibility = Item.BillableIconVisibility;
            hasTagsIcon.Visibility = Item.HasTagsIconVisibility;

            durationPadding.LayoutParameters = getDurationPaddingWidthDependentOnIcons();

            switch (Item.ViewModel.VisualizationIntent)
            {
                case LogItemVisualizationIntent.SingleItem:
                    presentAsSingleTimeEntry();
                    break;

                case LogItemVisualizationIntent.GroupItem:
                    presentAsTimeEntryInAGroup();
                    break;

                case LogItemVisualizationIntent.CollapsedGroupHeader:
                    presentAsCollapsedGroupHeader(Item.ViewModel.RepresentedTimeEntriesIds.Length);
                    break;

                case LogItemVisualizationIntent.ExpandedGroupHeader:
                    presentAsExpandedGroupHeader(Item.ViewModel.RepresentedTimeEntriesIds.Length);
                    break;

                default:
                    throw new ArgumentOutOfRangeException($"Cannot visualize {Item.ViewModel.VisualizationIntent} in the time entries log table.");
            }
        }

        public void StartAnimating(AnimationSide side)
        {
            if (animator != null && animator.IsRunning)
                return;

            mainLogBackgroundContinue.Visibility = side == AnimationSide.Right ? ViewStates.Visible : ViewStates.Invisible;
            mainLogBackgroundDelete.Visibility = side == AnimationSide.Left ? ViewStates.Visible : ViewStates.Invisible;

            var offsetsInDp = getAnimationOffsetsForSide(side);
            var offsetsInPx = offsetsInDp.Select(offset => (float)offset.DpToPixels(ItemView.Context)).ToArray();

            animator = ObjectAnimator.OfFloat(MainLogContentView, "translationX", offsetsInPx);
            animator.SetDuration(animationDuration);
            animator.RepeatMode = ValueAnimatorRepeatMode.Reverse;
            animator.RepeatCount = ValueAnimator.Infinite;
            animator.Start();
        }

        public void StopAnimating()
        {
            if (animator != null)
            {
                animator.Cancel();
                animator = null;
            }

            MainLogContentView.TranslationX = 0;
            mainLogBackgroundContinue.Visibility = ViewStates.Invisible;
            mainLogBackgroundDelete.Visibility = ViewStates.Invisible;
        }

        private float[] getAnimationOffsetsForSide(AnimationSide side)
        {
            switch (side)
            {
                case AnimationSide.Right:
                    return new[] { 50, 0, 3.5f, 0 };
                case AnimationSide.Left:
                    return new[] { -50, 0, -3.5f, 0 };
                default:
                    throw new ArgumentException("Unexpected side");
            }
        }

        private void presentAsCollapsedGroupHeader(int timeEntriesCount)
        {
            groupExpansionButton.Enabled = true;
            groupCountTextView.Context.GetColor(Resource.Color.defaultEditText);
            groupCountTextView.SetBackgroundResource(Resource.Drawable.GrayBorderRoundedRectangle);
            groupCountTextView.Enabled = true;
            groupCountTextView.Text = timeEntriesCount.ToString();
            groupCountTextView.Visibility = ViewStates.Visible;
            groupCountTextView.SetTextColor(GroupingColor.Collapsed.Text.ToNativeColor());
            groupItemBackground.Visibility = ViewStates.Gone;
            durationPadding.SetBackgroundColor(whiteColor);
            durationFadeGradient.SetBackgroundResource(Resource.Drawable.TransparentToWhiteGradient);
        }

        private void presentAsExpandedGroupHeader(int timeEntriesCount)
        {
            groupExpansionButton.Enabled = true;
            groupCountTextView.Context.GetColor(Resource.Color.buttonBlue);
            groupCountTextView.SetBackgroundResource(Resource.Drawable.LightBlueRoundedRectangle);
            groupCountTextView.Enabled = true;
            groupCountTextView.Text = timeEntriesCount.ToString();
            groupCountTextView.Visibility = ViewStates.Visible;
            groupItemBackground.Visibility = ViewStates.Gone;
            durationPadding.SetBackgroundColor(whiteColor);
            durationFadeGradient.SetBackgroundResource(Resource.Drawable.TransparentToWhiteGradient);
        }

        private void presentAsSingleTimeEntry()
        {
            groupExpansionButton.Enabled = false;
            groupCountTextView.Visibility = ViewStates.Gone;
            groupItemBackground.Visibility = ViewStates.Gone;
            durationPadding.SetBackgroundColor(whiteColor);
            durationFadeGradient.SetBackgroundResource(Resource.Drawable.TransparentToWhiteGradient);
        }

        private void presentAsTimeEntryInAGroup()
        {
            groupExpansionButton.Enabled = false;
            groupCountTextView.Visibility = ViewStates.Invisible;
            groupItemBackground.Visibility = ViewStates.Visible;
            durationPadding.SetBackgroundColor(grayColor);
            durationFadeGradient.SetBackgroundResource(Resource.Drawable.TransparentToLightGrayGradient);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing)
                return;

            timeEntriesLogCellContinueButton.Click -= onContinueClick;
            groupExpansionButton.Click -= onExpansionClick;
        }
    }
}
