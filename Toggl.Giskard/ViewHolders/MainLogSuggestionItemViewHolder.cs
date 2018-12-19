using System;
using System.Reactive.Subjects;
using Android.Graphics;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Toggl.Foundation.Suggestions;
using Toggl.Giskard.Extensions;

namespace Toggl.Giskard.ViewHolders
{
    public sealed class MainLogSuggestionItemViewHolder : BaseRecyclerViewHolder<Suggestion>
    {
        private TextView timeEntriesLogCellDescriptionLabel;
        private TextView timeEntriesLogCellProjectLabel;
        private TextView timeEntriesLogCellClientLabel;
        private ImageView timeEntriesLogCellContinueImage;

        private readonly ISubject<Suggestion> continueButtonTappedSubject = new Subject<Suggestion>();

        public MainLogSuggestionItemViewHolder(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public MainLogSuggestionItemViewHolder(View itemView) : base(itemView)
        {
        }

        protected override void InitializeViews()
        {
            timeEntriesLogCellDescriptionLabel = ItemView.FindViewById<TextView>(Resource.Id.TimeEntriesLogCellDescriptionLabel);
            timeEntriesLogCellProjectLabel = ItemView.FindViewById<TextView>(Resource.Id.TimeEntriesLogCellProjectLabel);
            timeEntriesLogCellClientLabel = ItemView.FindViewById<TextView>(Resource.Id.TimeEntriesLogCellClientLabel);
            timeEntriesLogCellContinueImage = ItemView.FindViewById<ImageView>(Resource.Id.TimeEntriesLogCellContinueImage);
        }

        protected override void UpdateView()
        {
            timeEntriesLogCellDescriptionLabel.Text = Item.Description;
            timeEntriesLogCellProjectLabel.Text = Item.ProjectName;
            timeEntriesLogCellProjectLabel.SetTextColor(Color.ParseColor(Item.ProjectColor));
            timeEntriesLogCellProjectLabel.Visibility = Item.HasProject.ToVisibility();
            timeEntriesLogCellClientLabel.Text = Item.ClientName;
            timeEntriesLogCellClientLabel.Visibility = Item.HasProject.ToVisibility();
        }
    }
}
