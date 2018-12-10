using System;
using Android.Runtime;
using Android.Support.Constraints;
using Android.Support.V7.Widget;
using Android.Views;
using MvvmCross.Platforms.Android.Binding.BindingContext;
using MvvmCross.Droid.Support.V7.RecyclerView;
using Toggl.Foundation.MvvmCross.ViewModels.Reports;
using Toggl.Giskard.TemplateSelectors;
using Toggl.Giskard.Views;
using Android.Widget;
using Toggl.Giskard.ViewHelpers;

namespace Toggl.Giskard.Adapters
{
    public sealed class ReportsRecyclerAdapter : MvxRecyclerAdapter
    {
        private const int workspaceNameCellIndex = 0;
        private const int summaryCardCellIndex = 1;
        private const int headerItemsCount = 2;

        private BarChartData? currentBarChartData;

        public ReportsViewModel ViewModel { get; set; }

        public ReportsRecyclerAdapter()
        {
        }

        public ReportsRecyclerAdapter(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            if (viewType != ReportsTemplateSelector.Item)
                return base.OnCreateViewHolder(parent, viewType);

            var itemBindingContext = new MvxAndroidBindingContext(parent.Context, BindingContext.LayoutInflaterHolder);
            var inflatedView = InflateViewForHolder(parent, viewType, itemBindingContext);
            var viewHolder = new ReportsRecyclerViewHolder(inflatedView, itemBindingContext)
            {
                Click = ItemClick,
                LongClick = ItemLongClick
            };

            return viewHolder;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            base.OnBindViewHolder(holder, position);

            if (holder is ReportsRecyclerViewHolder reportsViewHolder)
            {
                reportsViewHolder.IsLastItem = position == ItemCount - 1;
                reportsViewHolder.RecalculateSize();
            }

            if (position == summaryCardCellIndex && currentBarChartData.HasValue)
            {
                var barChartView = holder.ItemView.FindViewById<BarChartView>(Resource.Id.BarChartView);
                var barChartTopLegendGroup = holder.ItemView.FindViewById<Group>(Resource.Id.WorkspaceBillableGroup);
                var barChartData = currentBarChartData.GetValueOrDefault();
                barChartView.BarChartData = barChartData;
                barChartTopLegendGroup.Visibility = barChartData.WorkspaceIsBillable ? ViewStates.Visible : ViewStates.Gone;
            }

            if (position == workspaceNameCellIndex)
            {
                var workspaceNameTextView = holder.ItemView as TextView;
                workspaceNameTextView.Text = ViewModel.WorkspaceName;
            }
        }

        public override int ItemCount
            => headerItemsCount + base.ItemCount;

        public override object GetItem(int viewPosition)
        {
            if (viewPosition == workspaceNameCellIndex)
                return ViewModel.WorkspaceName;

            if (viewPosition == summaryCardCellIndex)
                return ViewModel;

            return base.GetItem(viewPosition - headerItemsCount);
        }

        public void UpdateBarChart(BarChartData barChartData)
        {
            currentBarChartData = barChartData;
            NotifyItemChanged(summaryCardCellIndex);
        }
    }
}
