﻿using System;
using CoreGraphics;
using Foundation;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Daneel.Extensions;
using Toggl.Foundation.UI.ViewModels.Reports;
using UIKit;
using System.Reactive.Disposables;
using Toggl.Daneel.Extensions.Reactive;
using System.Reactive.Linq;
using Toggl.Multivac.Extensions;
using System.Linq;
using Toggl.Multivac;
using System.Collections.Generic;
using System.Globalization;
using Toggl.Foundation.Conversions;
using System.Reactive.Subjects;
using System.Reactive;
using Toggl.Daneel.Cells;
using Toggl.Foundation;
using Toggl.Foundation.Extensions;
using Color = Toggl.Foundation.UI.Helper.Color;

namespace Toggl.Daneel.Views.Reports
{
    public partial class ReportsHeaderView : BaseTableHeaderFooterView<ReportsViewModel>
    {
        private const int fontSize = 24;
        private const float barChartSpacingProportion = 0.3f;

        private static readonly UIColor normalColor = Color.Reports.PercentageActivated.ToNativeColor();
        private static readonly UIColor disabledColor = Color.Reports.Disabled.ToNativeColor();

        public static readonly string Identifier = nameof(ReportsHeaderView);
        public static readonly NSString Key = new NSString(nameof(ReportsHeaderView));
        public static readonly UINib Nib;

        private readonly UIStringAttributes normalAttributes = new UIStringAttributes
        {
            Font = UIFont.SystemFontOfSize(fontSize, UIFontWeight.Medium),
            ForegroundColor = normalColor
        };

        private readonly UIStringAttributes disabledAttributes = new UIStringAttributes
        {
            Font = UIFont.SystemFontOfSize(fontSize, UIFontWeight.Medium),
            ForegroundColor = disabledColor
        };

        private readonly CompositeDisposable disposeBag = new CompositeDisposable();
        private readonly ISubject<Unit> updateLayout = new BehaviorSubject<Unit>(Unit.Default);

        static ReportsHeaderView()
        {
            Nib = UINib.FromName(nameof(ReportsHeaderView), NSBundle.MainBundle);
        }

        public ReportsHeaderView(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            TotalTitleLabel.Text = Resources.Total.ToUpper();
            BillableTitleLabel.Text = Resources.Billable.ToUpper();
            ClockedHoursTitleLabel.Text = Resources.ClockedHours.ToUpper();
            BillableLegendLabel.Text = Resources.Billable.ToUpper();
            NonBillableLegendLabel.Text = Resources.NonBillable.ToUpper();

            var templateImage = TotalDurationGraph.Image.ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
            TotalDurationGraph.Image = templateImage;

            prepareViews();
        }

        protected override void UpdateView()
        {
            //Text
            Item.BillablePercentageObservable
                .Select(billableFormattedString)
                .Subscribe(BillablePercentageLabel.Rx().AttributedText())
                .DisposedBy(disposeBag);

            Item.TotalTimeObservable
                .CombineLatest(Item.DurationFormatObservable,
                    (totalTime, durationFormat) => totalTime.ToFormattedString(durationFormat))
                .Subscribe(TotalDurationLabel.Rx().Text())
                .DisposedBy(disposeBag);

            //Loading chart
            Item.IsLoadingObservable
                .Subscribe(LoadingPieChartView.Rx().IsVisibleWithFade())
                .DisposedBy(disposeBag);

            Item.IsLoadingObservable
                .Subscribe(LoadingOverviewView.Rx().IsVisibleWithFade())
                .DisposedBy(disposeBag);

            //Pretty stuff
            Item.GroupedSegmentsObservable
                .Subscribe(groupedSegments => PieChartView.Segments = groupedSegments)
                .DisposedBy(disposeBag);

            Item.BillablePercentageObservable
                .Subscribe(percentage => BillablePercentageView.Percentage = percentage)
                .DisposedBy(disposeBag);

            var totalDurationColorObservable = Item.TotalTimeIsZeroObservable
                .Select(isZero => isZero
                    ? Foundation.UI.Helper.Color.Reports.Disabled.ToNativeColor()
                    : Foundation.UI.Helper.Color.Reports.TotalTimeActivated.ToNativeColor());

            totalDurationColorObservable
                .Subscribe(TotalDurationGraph.Rx().TintColor())
                .DisposedBy(disposeBag);

            totalDurationColorObservable
                .Subscribe(TotalDurationLabel.Rx().TextColor())
                .DisposedBy(disposeBag);

            // Bar chart
            Item.WorkspaceHasBillableFeatureEnabled
                .Subscribe(ColorsLegendContainerView.Rx().IsVisible())
                .DisposedBy(disposeBag);

            Item.StartDate
                .CombineLatest(
                    Item.BarChartViewModel.DateFormat,
                    (startDate, format) => startDate.ToString(format.Short, CultureInfo.InvariantCulture))
                .Subscribe(StartDateLabel.Rx().Text())
                .DisposedBy(disposeBag);

            Item.EndDate
                .CombineLatest(
                    Item.BarChartViewModel.DateFormat,
                    (endDate, format) => endDate.ToString(format.Short, CultureInfo.InvariantCulture))
                .Subscribe(EndDateLabel.Rx().Text())
                .DisposedBy(disposeBag);

            Item.BarChartViewModel.MaximumHoursPerBar
                .Select(hours => $"{hours} h")
                .Subscribe(MaximumHoursLabel.Rx().Text())
                .DisposedBy(disposeBag);

            Item.BarChartViewModel.MaximumHoursPerBar
                .Select(hours => $"{hours / 2} h")
                .Subscribe(HalfHoursLabel.Rx().Text())
                .DisposedBy(disposeBag);

            Item.BarChartViewModel.HorizontalLegend
                .Where(legend => legend == null)
                .Subscribe((DateTimeOffset[] _) =>
                {
                    HorizontalLegendStackView.Subviews.ForEach(subview => subview.RemoveFromSuperview());
                    StartDateLabel.Hidden = false;
                    EndDateLabel.Hidden = false;
                })
                .DisposedBy(disposeBag);

            Item.BarChartViewModel.HorizontalLegend
                .Where(legend => legend != null)
                .CombineLatest(Item.BarChartViewModel.DateFormat, createHorizontalLegendLabels)
                .Do(_ =>
                {
                    StartDateLabel.Hidden = true;
                    EndDateLabel.Hidden = true;
                })
                .Subscribe(HorizontalLegendStackView.Rx().ArrangedViews())
                .DisposedBy(disposeBag);

            Item.BarChartViewModel.Bars
                .Select(createBarViews)
                .Subscribe(BarsStackView.Rx().ArrangedViews())
                .DisposedBy(disposeBag);

            var spacingObservable = Item.BarChartViewModel.Bars
                .CombineLatest(updateLayout, (bars, _) => bars)
                .Select(bars => BarsStackView.Frame.Width / bars.Length * barChartSpacingProportion);

            spacingObservable
                .Subscribe(BarsStackView.Rx().Spacing())
                .DisposedBy(disposeBag);

            spacingObservable
                .Subscribe(HorizontalLegendStackView.Rx().Spacing())
                .DisposedBy(disposeBag);

            Item.IsLoadingObservable
                .Select(CommonFunctions.Invert)
                .Subscribe(BarChartContainerView.Rx().IsVisible())
                .DisposedBy(disposeBag);

            //Visibility
            Item.ShowEmptyStateObservable
                .Subscribe(EmptyStateView.Rx().IsVisible())
                .DisposedBy(disposeBag);

            NSAttributedString billableFormattedString(float? value)
            {
                var isDisabled = value == null;
                var actualValue = isDisabled ? 0 : value.Value;

                var percentage = $"{actualValue.ToString("0.00")}%";

                var attributes = isDisabled ? disabledAttributes : normalAttributes;
                return new NSAttributedString(percentage, attributes);
            }
        }


        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            updateLayout.OnNext(Unit.Default);
        }

        private void prepareViews()
        {
            prepareCard(OverviewCardView);
            prepareCard(BarChartCardView);

            TotalTitleLabel.SetKerning(-0.2);
            TotalDurationLabel.SetKerning(-0.2);
            BillableTitleLabel.SetKerning(-0.2);
            BillablePercentageLabel.SetKerning(-0.2);
            ClockedHoursTitleLabel.SetKerning(-0.2);
            BillableLegendLabel.SetKerning(-0.2);
            NonBillableLegendLabel.SetKerning(-0.2);
        }

        private void prepareCard(UIView view)
        {
            view.Layer.CornerRadius = 8;
            view.Layer.ShadowColor = UIColor.Black.CGColor;
            view.Layer.ShadowRadius = 16;
            view.Layer.ShadowOffset = new CGSize(0, 2);
            view.Layer.ShadowOpacity = 0.1f;
        }

        private IEnumerable<UIView> createBarViews(IEnumerable<BarViewModel> bars)
            => bars.Select<BarViewModel, UIView>(bar =>
            {
                if (bar.NonBillablePercent == 0 && bar.BillablePercent == 0)
                {
                    return new EmptyBarView();
                }

                return new BarView(bar);
            });

        private IEnumerable<UILabel> createHorizontalLegendLabels(IEnumerable<DateTimeOffset> dates, DateFormat format)
            => dates.Select(date =>
                new BarLegendLabel(
                    DateTimeOffsetConversion.ToDayOfWeekInitial(date),
                    date.ToString(format.Short, CultureInfo.InvariantCulture)));
    }
}
