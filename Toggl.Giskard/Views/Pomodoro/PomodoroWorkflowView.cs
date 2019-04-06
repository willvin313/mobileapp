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
using System.Reactive.Subjects;
using System.Reactive.Linq;

namespace Toggl.Giskard.Views.Pomodoro
{
    [Register("toggl.giskard.views.PomodoroWorkflowView")]
    public class PomodoroWorkflowView : View
    {
        private enum SelectionMode
        {
            None = 0,
            Manual = 1,
            UserInteraction = 2
        }

        private int labelFontSize = 14;
        private int verticalOffset = 0;
        private int requiredTotalPadding = 8;
        private const double verticalNonSelectedFactor = 0.8;

        private SelectionMode selectionMode;

        private IReadOnlyList<PomodoroWorkflowItem> items = new List<PomodoroWorkflowItem>();

        private List<double> segmentEndCoordinates;
        private List<int> segmentsWidths;
        private List<Paint> segmentsPaints;
        private int viewWidth;
        private bool isViewWidthKnown;

        private readonly Paint workPaint = new Paint() { Color = new Color(38, 156, 222) };
        private readonly Paint restPaint = new Paint() { Color = new Color(83, 103, 108) };
        private readonly Paint workflowPaint = new Paint() { Color = new Color(141, 76, 175) };
        private Paint labelPaint;

        private int selectedIndex;

        public PomodoroWorkflowItem SelectedWorkflowItem => items[SelectedWorkflowItemIndex];

        public int SelectedWorkflowItemIndex
        {
            get => selectedIndex;
            set
            {
                if (selectedIndex == value)
                    return;

                selectedIndex = value;

                var eventArgs = new SelectedWorkflowItemChangedEventArgs(items[selectedIndex], selectedIndex);
                SelectedWorkflowItemIndexChanged?.Invoke(this, eventArgs);
                Invalidate();
            }
        }

        public event EventHandler<SelectedWorkflowItemChangedEventArgs> SelectedWorkflowItemIndexChanged;

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
            initializeAttributeSet(context, attrs);
            init(context);
        }

        public PomodoroWorkflowView(Context context, IAttributeSet attrs, int defStyleAttrs) : base(context, attrs, defStyleAttrs)
        {
            initializeAttributeSet(context, attrs, 0, defStyleAttrs);
            init(context);
        }

        public PomodoroWorkflowView(Context context, IAttributeSet attrs, int defStyleAttrs, int defStyleRes) : base(context, attrs, defStyleAttrs, defStyleRes)
        {
            initializeAttributeSet(context, attrs, defStyleAttrs, defStyleRes);
            init(context);
        }

        private void initializeAttributeSet(Context context, IAttributeSet attrs, int defStyleAttrs = 0, int defStyleRes = 0)
        {
            var customsAttrs =
                context.ObtainStyledAttributes(attrs, Resource.Styleable.PomodoroWorkflowView, defStyleAttrs, defStyleRes);

            try
            {
                selectionMode = (SelectionMode)customsAttrs.GetInteger(Resource.Styleable.PomodoroWorkflowView_selectionMode, 0);
            }
            finally
            {
                customsAttrs.Recycle();
            }
        }

        #endregion

        private void init(Context context)
        {
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

            Update(items);

            SelectedWorkflowItemIndex = 0;
        }

        protected override void OnDraw(Canvas canvas)
        {
            canvas.DrawColor(Color.White);

            if (!isViewWidthKnown)
                return;

            if (items.Count == 0)
            {
                canvas.DrawColor(Color.Gray);
                return;
            }

            for (int i = 0, offsetX = 0; i < items.Count; i++)
            {
                var text = items[i].Minutes.ToString();
                var textBounds = labelPaint.GetTextBounds(text);

                var width = segmentsWidths[i];
                var height = selectionMode >= SelectionMode.Manual && SelectedWorkflowItemIndex != i
                    ? (int)(canvas.Height * verticalNonSelectedFactor)
                    : canvas.Height;

                var xStart = offsetX;
                var xEnd = i == items.LastIndex()
                    ? viewWidth
                    : xStart + width;

                var midX = (xEnd + xStart) / 2;
                var midY = height / 2;

                canvas.DrawRect(xStart, 0, xEnd, height, segmentsPaints[i]);

                if (textBounds.Width() + requiredTotalPadding <= width)
                {
                    canvas.DrawText(text, midX, midY - textBounds.CenterY(), labelPaint);
                }

                offsetX = xEnd;
            }
        }

        public void Update(IReadOnlyList<PomodoroWorkflowItem> items)
        {
            this.items = items;

            if (!isViewWidthKnown || items.Count == 0)
                return;

            var totalDuration = (double)items.Sum(item => item.Minutes);

            segmentsWidths = items
                .Select(item => item.Minutes / totalDuration)
                .Select(normalizedWidth => (int)(viewWidth * normalizedWidth))
                .ToList();

            segmentEndCoordinates = new List<double>(segmentsWidths.Count);

            var offset = 0.0;
            foreach (var width in segmentsWidths)
            {
                offset += width;
                segmentEndCoordinates.Add(offset);
            }

            // Fix precision errors so that values are really from 0 to width.
            if (segmentEndCoordinates.Count > 0)
                segmentEndCoordinates[segmentEndCoordinates.LastIndex()] = viewWidth;

            segmentsPaints = items
                 .Select(item => item.Type)
                 .Select(paintForItemType)
                 .ToList();

            Invalidate();
        }

        private int? findTouchedItemIndex(double x)
        {
            if (items.Count == 0)
                return null;

            for (int index = 0; index < segmentEndCoordinates.Count; index++)
            {
                if (x < segmentEndCoordinates[index])
                    return index;
            }

            throw new InvalidOperationException("The algorithm is incorrect. This exception should never happen.");
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            if (selectionMode < SelectionMode.UserInteraction)
                return base.OnTouchEvent(e);

            if (e.Action == MotionEventActions.Move || e.Action == MotionEventActions.Down)
            {
                var x = e.GetX();

                var index = findTouchedItemIndex(x);

                if (!index.HasValue)
                    return base.OnTouchEvent(e);

                SelectedWorkflowItemIndex = index.Value;

                return true;
            }

            return base.OnTouchEvent(e);
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

        protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
        {
            base.OnLayout(changed, left, top, right, bottom);

            viewWidth = Width;
            isViewWidthKnown = true;

            Update(items);
        }
    }
}
