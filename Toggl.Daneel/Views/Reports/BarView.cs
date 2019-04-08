using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Foundation.UI.Helper;
using Toggl.Foundation.UI.ViewModels.Reports;
using UIKit;
using System;

namespace Toggl.Daneel.Views.Reports
{
    internal sealed class BarView : UIView
    {
        private readonly nfloat minimumHeight = 1;

        private readonly BarViewModel bar;

        private readonly UIView billableView;

        private readonly UIView nonBillableView;

        private readonly UIView minimumLevelView;

        private bool shouldSetupConstraints = true;

        public BarView(BarViewModel bar)
        {
            this.bar = bar;

            nonBillableView = new BarSegmentView(Color.Reports.BarChart.NonBillable.ToNativeColor());
            billableView = new BarSegmentView(Color.Reports.BarChart.Billable.ToNativeColor());
            minimumLevelView = new BarSegmentView(
                bar.BillablePercent > bar.NonBillablePercent
                    ? Color.Reports.BarChart.Billable.ToNativeColor()
                    : Color.Reports.BarChart.NonBillable.ToNativeColor());

            AddSubview(minimumLevelView);
            AddSubview(nonBillableView);
            AddSubview(billableView);
        }

        public override void UpdateConstraints()
        {
            base.UpdateConstraints();

            if (!shouldSetupConstraints) return;

            minimumLevelView.BottomAnchor.ConstraintEqualTo(BottomAnchor).Active = true;
            minimumLevelView.LeftAnchor.ConstraintEqualTo(LeftAnchor).Active = true;
            minimumLevelView.RightAnchor.ConstraintEqualTo(RightAnchor).Active = true;
            minimumLevelView.HeightAnchor.ConstraintEqualTo(minimumHeight).Active = true;

            billableView.BottomAnchor.ConstraintEqualTo(BottomAnchor).Active = true;
            billableView.LeftAnchor.ConstraintEqualTo(LeftAnchor).Active = true;
            billableView.RightAnchor.ConstraintEqualTo(RightAnchor).Active = true;

            nonBillableView.BottomAnchor.ConstraintEqualTo(billableView.TopAnchor).Active = true;
            nonBillableView.LeftAnchor.ConstraintEqualTo(LeftAnchor).Active = true;
            nonBillableView.RightAnchor.ConstraintEqualTo(RightAnchor).Active = true;

            billableView.HeightAnchor.ConstraintEqualTo(HeightAnchor, (nfloat)bar.BillablePercent).Active = true;
            nonBillableView.HeightAnchor.ConstraintEqualTo(HeightAnchor, (nfloat)bar.NonBillablePercent).Active = true;

            shouldSetupConstraints = false;
        }
    }
}
