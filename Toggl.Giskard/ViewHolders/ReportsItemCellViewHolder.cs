using System;
using Android.Graphics;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Toggl.Foundation.MvvmCross.Themes;
using Toggl.Foundation.MvvmCross.Transformations;
using Toggl.Foundation.Reports;
using Toggl.Giskard.Extensions;

namespace Toggl.Giskard.ViewHolders
{
    public sealed class ReportsItemCellViewHolder : BaseRecyclerViewHolder<ChartSegment>
    {
        private readonly int lastContainerHeight;
        private readonly int normalContainerHeight;

        private View fadeView;
        private View separator;
        private CardView cardView;
        private TextView projectName;
        private TextView clientName;
        private TextView duration;
        private TextView percentage;

        public bool IsLastItem { get; set; }

        public ReportsItemCellViewHolder(View itemView, int lastContainerHeight, int normalContainerHeight) : base(itemView)
        {
            this.lastContainerHeight = lastContainerHeight;
            this.normalContainerHeight = normalContainerHeight;
        }

        public ReportsItemCellViewHolder(IntPtr handle, JniHandleOwnership ownership) : base(handle, ownership)
        {
        }

        public void RecalculateSize()
        {
            var height = IsLastItem ? lastContainerHeight : normalContainerHeight;
            var layoutParameters = ItemView.LayoutParameters;
            layoutParameters.Height = height;
            ItemView.LayoutParameters = layoutParameters;
        }

        protected override void UpdateTheme(ITheme theme)
        {
            var cardColor = theme.Card.ToNativeColor();
            separator.SetBackgroundColor(cardColor);
            duration.SetTextColor(theme.Text.ToNativeColor());
            cardView.SetCardBackgroundColor(cardColor.ToArgb());
            fadeView.Background = theme.Card.ToTransparentGradient();
        }

        protected override void InitializeViews()
        {
            projectName = ItemView.FindViewById<TextView>(Resource.Id.ReportsFragmentItemProjectName);
            clientName = ItemView.FindViewById<TextView>(Resource.Id.ReportsFragmentItemClientName);
            duration = ItemView.FindViewById<TextView>(Resource.Id.ReportsFragmentItemDuration);
            percentage = ItemView.FindViewById<TextView>(Resource.Id.ReportsFragmentItemPercentage);
            cardView = ItemView.FindViewById<CardView>(Resource.Id.CardView);
            separator = ItemView.FindViewById(Resource.Id.Separator);
            fadeView = ItemView.FindViewById(Resource.Id.FadeView);
        }

        protected override void UpdateView()
        {
            projectName.Text = Item.ProjectName;
            projectName.SetTextColor(Color.ParseColor(Item.Color));

            duration.Text = DurationAndFormatToString.Convert(Item.TrackedTime, Item.DurationFormat);

            percentage.Text = $"{Item.Percentage:0.00}%";
        }
    }
}
