using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using static Toggl.Foundation.Models.Pomodoro.PomodoroWorkflowItemType;
using Toggl.Multivac.Extensions;
using Toggl.Giskard.Extensions;
using Toggl.Foundation.Models.Pomodoro;

namespace Toggl.Giskard.Views.Pomodoro
{
    [Register("toggl.giskard.views.PomodoroWorkflowView")]
    public class PomodoroWorkflowView : View
    {
        private int labelFontSize = 14;
        private int verticalOffset = 0;
        private int requiredTotalPadding = 8;

        private IReadOnlyList<PomodoroWorkflowItem> items;

        #region Constructors

        protected PomodoroWorkflowView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public PomodoroWorkflowView(Context context) : base(context)
        {
            init(context);
        }

        public PomodoroWorkflowView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            init(context);
        }

        public PomodoroWorkflowView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
            init(context);
        }

        public PomodoroWorkflowView(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes)
        {
            init(context);
        }

        #endregion

        private readonly Paint workPaint = new Paint() { Color = new Color(38, 156, 222) };
        private readonly Paint restPaint = new Paint() { Color = new Color(83, 103, 108) };
        private readonly Paint workflowPaint = new Paint() { Color = new Color(141, 76, 175) };

        private Paint labelPaint;

        protected override void OnDraw(Canvas canvas)
        {
            canvas.DrawColor(Color.White);

            var totalDuration = (double)items.Sum(item => item.Minutes);

            var segmentsWidths = items
                   .Select(item => item.Minutes / totalDuration)
                   .Select(normalizedWidth => (int)(canvas.Width * normalizedWidth))
                   .ToList();

            var segmentsPaints = items
                 .Select(item => item.Type)
                 .Select(paintForItemType)
                 .ToList();

            var offsetX = 0;

            for (int i = 0; i < items.Count; i++)
            {
                var text = items[i].Minutes.ToString();
                var textBounds = labelPaint.GetTextBounds(text);

                var width = segmentsWidths[i];

                var xStart = offsetX;
                var xEnd = i == items.LastIndex() ? canvas.Width : xStart + width;

                var midX = (xEnd + xStart) / 2;
                var midY = canvas.Height / 2;

                canvas.DrawRect(xStart, 0, xEnd, canvas.Height, segmentsPaints[i]);

                if (textBounds.Width() + requiredTotalPadding <= width)
                {
                    canvas.DrawText(text, midX, midY - textBounds.CenterY(), labelPaint);
                }

                offsetX = xEnd;
            }
        }

        private Paint paintForItemType(PomodoroWorkflowItemType type)
        {
            switch (type)
            {
                case Work: return workPaint;
                case Rest: return restPaint;
                case Workflow: return workflowPaint;
            }

            throw new ArgumentException("Invalid item type");
        }

        private void init(Context context)
        {
            items = new List<PomodoroWorkflowItem>()
            {
                new PomodoroWorkflowItem(Work, 30)
            };

            labelFontSize = labelFontSize.SpToPixels(context);
            verticalOffset = verticalOffset.DpToPixels(context);
            requiredTotalPadding = requiredTotalPadding.DpToPixels(context);

            labelPaint = new Paint()
            {
                Color = new Color(255, 255, 255),
                TextSize = labelFontSize,
                TextAlign = Paint.Align.Center
            };

            labelPaint.SetTypeface(Typeface.Create(Typeface.Default, TypefaceStyle.Bold));
        }

        public void Update(IReadOnlyList<PomodoroWorkflowItem> items)
        {
            this.items = items;
            PostInvalidate();
        }
    }
}
