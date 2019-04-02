using System;
using System.Reactive.Subjects;
using Android.Runtime;
using System.Linq;
using Android.Animation;
using Android.Support.Constraints;
using Android.Text;
using Android.Views;
using Android.Widget;
using Toggl.Foundation.MvvmCross.Themes;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.ViewHelpers;
using Toggl.Multivac;
using static Toggl.Giskard.Resource.Id;

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
        private View timeEntriesLogCellContinueImage;
        private View errorImageView;
        private View errorNeedsSync;
        private View timeEntriesLogCellContinueButton;
        private View mainLogBackgroundContinue;
        private View mainLogBackgroundDelete;
        private View billableIcon;
        private View hasTagsIcon;
        private View whitePadding;
        private View fadeView;
        private View separator;
        private SpannableFactory spannableFactory = new CopylessSpannableFactory();

        private ObjectAnimator animator;

        public bool IsAnimating => animator?.IsRunning ?? false;

        public bool CanSync => Item.TimeEntryViewModel.CanSync;

        public View MainLogContentView { get; private set; }

        public Subject<TimeEntryViewModel> ContinueButtonTappedSubject { get; set; }

        protected override void InitializeViews()
        {
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
            whitePadding = ItemView.FindViewById(TimeEntriesLogCellDurationWhiteArea);
            fadeView = ItemView.FindViewById(TimeEntriesLogCellDurationGradient);
            separator = ItemView.FindViewById(Separator);
            MainLogContentView = ItemView.FindViewById(Resource.Id.MainLogContentView);
            
            timeEntriesLogCellContinueButton.Click += onContinueClick;
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

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing || timeEntriesLogCellContinueButton == null) return;
            timeEntriesLogCellContinueButton.Click -= onContinueClick;
        }

        private void onContinueClick(object sender, EventArgs e)
        {
            ContinueButtonTappedSubject?.OnNext(Item.TimeEntryViewModel);
        }

        private ConstraintLayout.LayoutParams getWhitePaddingWidthDependentOnIcons()
        {
            var whitePaddingWidth =
                72
                + (Item.TimeEntryViewModel.IsBillable ? 22 : 0)
                + (Item.TimeEntryViewModel.HasTags ? 22 : 0);

            var layoutParameters = (ConstraintLayout.LayoutParams) whitePadding.LayoutParameters;
            layoutParameters.Width = whitePaddingWidth.DpToPixels(ItemView.Context);
            return layoutParameters;
        }

        protected override void UpdateTheme(ITheme theme)
        {
            separator.SetBackgroundColor(theme.Separator.ToNativeColor());
            timeEntriesLogCellDescription.SetTextColor(theme.Text.ToNativeColor());
            MainLogContentView.SetBackgroundColor(theme.CellBackground.ToNativeColor());
;
            fadeView.Background = theme.CellBackground.ToTransparentGradient();
        }

        protected override void UpdateView()
        {
            StopAnimating();

            timeEntriesLogCellDescription.Text = Item.TimeEntryViewModel.Description;
            timeEntriesLogCellDescription.Visibility = Item.DescriptionVisibility;
            addDescriptionLabel.Visibility = Item.AddDescriptionLabelVisibility;

            timeEntriesLogCellProjectLabel.TextFormatted = Item.ProjectTaskClientText;
            timeEntriesLogCellProjectLabel.Visibility = Item.ProjectTaskClientVisibility;

            timeEntriesLogCellDuration.Text = Item.DurationText;

            timeEntriesLogCellContinueImage.Visibility = Item.ContinueImageVisibility;
            errorImageView.Visibility = Item.ErrorImageViewVisibility;
            errorNeedsSync.Visibility = Item.ErrorNeedsSyncVisibility;
            timeEntriesLogCellContinueButton.Visibility = Item.ContinueButtonVisibility;
            billableIcon.Visibility = Item.BillableIconVisibility;
            hasTagsIcon.Visibility = Item.HasTagsIconVisibility;

            whitePadding.LayoutParameters = getWhitePaddingWidthDependentOnIcons();
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
    }
}
