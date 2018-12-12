using System;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Android.Content.Res;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Suggestions;
using Toggl.Giskard.Adapters;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.Extensions.Reactive;
using Toggl.Giskard.ViewHelpers;
using Toggl.Multivac.Extensions;
using Toggl.Foundation.MvvmCross.Extensions;
using System.Reactive;
using static Toggl.Multivac.Extensions.CommonFunctions;
using Android.Support.Constraints;

namespace Toggl.Giskard.ViewHolders
{
    public class MainLogUserFeedbackViewHolder : RecyclerView.ViewHolder
    {
        private RatingViewModel ratingViewModel;

        private ImageView thumbsUpButton;
        private ImageView thumbsDownButton;
        private TextView yesText;
        private TextView noText;
        private TextView impressionTitle;
        private ImageView impressionThumbsImage;
        private TextView impressionDescription;
        private Button rateButton;
        private TextView laterButton;

        private Group questionGroup;
        private Group impressionGroup;

        private readonly CompositeDisposable disposeBag = new CompositeDisposable();

        public MainLogUserFeedbackViewHolder(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public MainLogUserFeedbackViewHolder(View itemView, RatingViewModel ratingViewModel) : base(itemView)
        {
            this.ratingViewModel = ratingViewModel;
            initializeViews(itemView);
            bindViews(itemView);
        }

        private void bindViews(View itemView)
        {
            ratingViewModel.CallToActionTitle
                .Subscribe(impressionTitle.Rx().TextObserver())
                .DisposedBy(disposeBag);

            ratingViewModel.CallToActionButtonTitle
                .Subscribe(rateButton.Rx().TextObserver())
                .DisposedBy(disposeBag);

            ratingViewModel.CallToActionDescription
                .Subscribe(impressionDescription.Rx().TextObserver())
                .DisposedBy(disposeBag);

            ratingViewModel.Impression
                .Select(impression => impression.HasValue)
                .Subscribe(impressionGroup.Rx().IsVisible())
                .DisposedBy(disposeBag);

            ratingViewModel.Impression
                .Select(impression => impression.HasValue)
                .Select(Invert)
                .Subscribe(questionGroup.Rx().IsVisible())
                .DisposedBy(disposeBag);

            ratingViewModel.Impression
               .Select(impression => impression ?? false)
               .Select(drawableFromImpression)
               .Subscribe(impressionThumbsImage.Rx().Image(itemView.Context))
               .DisposedBy(disposeBag);

            thumbsUpButton.Rx().Tap()
                .Subscribe(() => ratingViewModel.RegisterImpression(true))
                .DisposedBy(disposeBag);

            yesText.Rx().Tap()
                .Subscribe(() => ratingViewModel.RegisterImpression(true))
                .DisposedBy(disposeBag);

            thumbsDownButton.Rx().Tap()
                .Subscribe(() => ratingViewModel.RegisterImpression(false))
                .DisposedBy(disposeBag);

            noText.Rx().Tap()
                .Subscribe(() => ratingViewModel.RegisterImpression(false))
                .DisposedBy(disposeBag);

            rateButton.Rx().Tap()
                .Subscribe(ratingViewModel.PerformMainAction.Inputs)
                .DisposedBy(disposeBag);

            laterButton.Rx().Tap()
                .Subscribe(ratingViewModel.Dismiss)
                .DisposedBy(disposeBag);
        }

        private void initializeViews(View view)
        {
            thumbsUpButton = view.FindViewById<ImageView>(Resource.Id.UserFeedbackThumbsUp);
            thumbsDownButton = view.FindViewById<ImageView>(Resource.Id.UserFeedbackThumbsDown);
            yesText = view.FindViewById<TextView>(Resource.Id.UserFeedbackThumbsUpText);
            noText = view.FindViewById<TextView>(Resource.Id.UserFeedbackThumbsDownText);
            impressionTitle = view.FindViewById<TextView>(Resource.Id.UserFeedbackImpressionTitle);
            impressionThumbsImage = view.FindViewById<ImageView>(Resource.Id.UserFeedbackImpressionThumbsImage);
            impressionDescription = view.FindViewById<TextView>(Resource.Id.UserFeedbackDescription);
            rateButton = view.FindViewById<Button>(Resource.Id.UserFeedbackRateButton);
            laterButton = view.FindViewById<TextView>(Resource.Id.UserFeedbackLaterButton);

            questionGroup = view.FindViewById<Group>(Resource.Id.QuestionView);
            impressionGroup = view.FindViewById<Group>(Resource.Id.ImpressionView);
        }

        private int drawableFromImpression(bool impression)
            => impression ? Resource.Drawable.ic_thumbs_up : Resource.Drawable.ic_thumbs_down;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            disposeBag.Dispose();
        }
    }
}
