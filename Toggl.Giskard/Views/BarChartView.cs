using System;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Toggl.Foundation.UI.ViewModels.Reports;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.ViewHelpers;

namespace Toggl.Giskard.Views
{
    [Register("toggl.giskard.views.BarChartView")]
    public class BarChartView : View
    {
        private const float barDrawingYTranslationAdjustmentInPixels = 1f;
        private const float defaultBarSpacingRatio = 0.2f;
        private const float minHeightForBarsWithPercentages = 1f;

        private static readonly Color nonBillableColor = Color.ParseColor("#1e328fff");
        private static readonly Color billableColor = Color.ParseColor("#328fff");
        private static readonly Color horizontalLineColor = Color.ParseColor("#e5e5e5");
        private static readonly Color hoursTextColor = Color.ParseColor("#cecece");
        private static readonly Color primaryTextColor = Color.ParseColor("#757575");
        private static readonly Color emptyBarColor = Color.ParseColor("#252525");

        private readonly Paint billablePaint = new Paint();
        private readonly Paint nonBillablePaint = new Paint();
        private readonly Paint othersPaint = new Paint();

        private float maxWidth;
        private float barsRightMargin;
        private float barsLeftMargin;
        private float barsBottomMargin;
        private float barsTopMargin;
        private float textSize;
        private float textLeftMargin;
        private float textBottomMargin;
        private float bottomLabelMarginTop;
        private string hourSymbol;

        private int barsCount;
        private bool willDrawBarChart;
        private float barsWidth;
        private float barsHeight;
        private float actualBarWidth;
        private int spaces;
        private float totalWidth;
        private float remainingWidth;
        private float spacing;
        private float requiredWidth;
        private float middleHorizontalLineY;
        private float barsBottom;
        private float hoursLabelsX;
        private float hoursBottomMargin;
        private bool willDrawDayLabels;
        private float startEndDatesY;
        private float barsStartingLeft;
        private BarViewModel[] bars;
        private BarChartDayLabel[] horizontalLabels;
        private string maxHours;
        private string halfHours;
        private string zeroHours;
        private string startDate;
        private string endDate;
        private float dayLabelsY;

        private BarChartData? barChartData;

        public BarChartData? BarChartData
        {
            get => barChartData;
            set
            {
                barChartData = value;
                updateBarChartDrawingData();
                PostInvalidate();
            }
        }

        public BarChartView(Context context) : base(context)
        {
            initialize(context);
        }

        public BarChartView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            initialize(context);
        }

        protected BarChartView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        private void initialize(Context context)
        {
            maxWidth = 48.DpToPixels(context);
            barsRightMargin = 40.DpToPixels(context);
            barsLeftMargin = 12.DpToPixels(context);
            barsBottomMargin = 40.DpToPixels(context);
            barsTopMargin = 18.DpToPixels(context);
            textSize = 12.SpToPixels(context);
            textLeftMargin = 12.DpToPixels(context);
            textBottomMargin = 4.DpToPixels(context);
            bottomLabelMarginTop = 12.DpToPixels(context);
            hourSymbol = context.GetString(Resource.String.HourSymbol);

            othersPaint.TextSize = textSize;
            billablePaint.Color = billableColor;
            nonBillablePaint.Color = nonBillableColor;
            nonBillablePaint.SetStyle(Paint.Style.FillAndStroke);
            billablePaint.SetStyle(Paint.Style.FillAndStroke);
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
            updateBarChartDrawingData();
            PostInvalidate();
        }

        private void updateBarChartDrawingData()
        {
            if (!barChartData.HasValue)
            {
                willDrawBarChart = false;
                return;
            }

            var barChartDataValue = barChartData.Value;

            barsCount = barChartDataValue.Bars.Length;
            willDrawBarChart = barsCount > 0;

            if (!willDrawBarChart) return;

            bars = barChartDataValue.Bars;
            horizontalLabels = barChartDataValue.HorizontalLabels;
            maxHours = $"{barChartDataValue.MaximumHoursPerBar} {hourSymbol}";
            halfHours = $"{barChartDataValue.MaximumHoursPerBar / 2} {hourSymbol}";
            zeroHours = $"0 {hourSymbol}";
            startDate = barChartDataValue.StartDate ?? string.Empty;
            endDate = barChartDataValue.EndDate ?? string.Empty;

            barsWidth = MeasuredWidth - barsLeftMargin - barsRightMargin;
            barsHeight = MeasuredHeight - barsTopMargin - barsBottomMargin;

            var idealBarWidth = Math.Min(barsWidth / barsCount, maxWidth);
            spaces = barsCount - 1;
            totalWidth = idealBarWidth * barsCount;
            remainingWidth = barsWidth - totalWidth;
            spacing = Math.Max(remainingWidth / barsCount, idealBarWidth * defaultBarSpacingRatio);
            requiredWidth = totalWidth + spaces * spacing;
            actualBarWidth = requiredWidth > barsWidth ? idealBarWidth * (1 - defaultBarSpacingRatio) : idealBarWidth;
            middleHorizontalLineY = barsHeight / 2f + barsTopMargin;
            barsBottom = MeasuredHeight - barsBottomMargin;

            hoursLabelsX = MeasuredWidth - barsRightMargin + textLeftMargin;
            hoursBottomMargin = textBottomMargin * 2f;

            willDrawDayLabels = horizontalLabels.Length > 0;
            startEndDatesY = barsBottom + bottomLabelMarginTop * 2f;
            dayLabelsY = barsBottom + bottomLabelMarginTop * 1.25f;
            barsStartingLeft = barsLeftMargin + (barsWidth - (actualBarWidth * barsCount + spaces * spacing)) / 2f;
        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);
            if (!willDrawBarChart) return;

            othersPaint.Color = horizontalLineColor;
            canvas.DrawLine(0f, barsTopMargin, Width, barsTopMargin, othersPaint);
            canvas.DrawLine(0f, middleHorizontalLineY, Width, middleHorizontalLineY, othersPaint);
            canvas.DrawLine(0f, barsBottom, Width, barsBottom, othersPaint);

            othersPaint.Color = hoursTextColor;
            canvas.DrawText(zeroHours, hoursLabelsX, barsBottom - hoursBottomMargin, othersPaint);
            canvas.DrawText(halfHours, hoursLabelsX, middleHorizontalLineY - hoursBottomMargin, othersPaint);
            canvas.DrawText(maxHours, hoursLabelsX, barsTopMargin - hoursBottomMargin, othersPaint);

            othersPaint.Color = primaryTextColor;

            if (!willDrawDayLabels)
            {
                othersPaint.TextAlign = Paint.Align.Left;
                canvas.DrawText(startDate, barsLeftMargin, startEndDatesY, othersPaint);
                othersPaint.TextAlign = Paint.Align.Right;
                canvas.DrawText(endDate, Width - barsRightMargin, startEndDatesY, othersPaint);
            }

            var left = barsStartingLeft;

            othersPaint.TextAlign = Paint.Align.Center;

            for (var barIndex = 0; barIndex < bars.Length; barIndex++)
            {
                var bar = bars[barIndex];
                var barRight = left + actualBarWidth;
                var barHasBillablePercentage = bar.BillablePercent > 0f;
                var barHasNonBillablePercentage = bar.NonBillablePercent > 0f;
                if (!barHasBillablePercentage && !barHasNonBillablePercentage)
                {
                    othersPaint.Color = emptyBarColor;
                    canvas.DrawLine(left, barsBottom, barRight, barsBottom, othersPaint);
                }
                else
                {
                    var billableBarHeight = (float) (barsHeight * bar.BillablePercent);
                    var billableTop = calculateBillableTop(billableBarHeight, barHasBillablePercentage);
                    canvas.DrawRect(left, billableTop, barRight, barsBottom + barDrawingYTranslationAdjustmentInPixels, billablePaint);

                    var nonBillableBarHeight = (float) (barsHeight * bar.NonBillablePercent);
                    var nonBillableTop = calculateNonBillableTop(billableTop, nonBillableBarHeight, barHasNonBillablePercentage);
                    canvas.DrawRect(left, nonBillableTop, barRight, billableTop, nonBillablePaint);
                }

                if (willDrawDayLabels)
                {
                    var middleOfTheBar = left + (barRight - left) / 2f;
                    canvas.DrawText(horizontalLabels[barIndex].DayOfWeek, middleOfTheBar, dayLabelsY, othersPaint);
                    canvas.DrawText(horizontalLabels[barIndex].Date, middleOfTheBar, dayLabelsY + textSize, othersPaint);
                }

                left += actualBarWidth + spacing;
            }
        }

        private float calculateBillableTop(float billableBarHeight, bool barHasBillablePercentage)
        {
            var billableTop = barsBottom - billableBarHeight + barDrawingYTranslationAdjustmentInPixels;
            var barHasAtLeast1PixelInHeight = billableBarHeight >= minHeightForBarsWithPercentages;

            return barHasBillablePercentage && !barHasAtLeast1PixelInHeight
                ? billableTop - minHeightForBarsWithPercentages
                : billableTop;
        }

        private float calculateNonBillableTop(float billableTop, float nonBillableBarHeight, bool barHasNonBillablePercentage)
        {
            //Non billable-top doesn't need the extra Y translation because the billableTop accounts for it.
            var nonBillableTop = billableTop - nonBillableBarHeight;
            var barHasAtLeast1PixelInHeight = nonBillableBarHeight >= minHeightForBarsWithPercentages;

            return barHasNonBillablePercentage && !barHasAtLeast1PixelInHeight
                ? nonBillableTop - minHeightForBarsWithPercentages
                : nonBillableTop;
        }
    }
}
