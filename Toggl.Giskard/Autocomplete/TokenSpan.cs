using Android.App;
using Android.Graphics;
using Android.Text.Style;
using Java.Lang;
using MvvmCross;
using MvvmCross.Droid;
using MvvmCross.Platforms.Android;
using Toggl.Giskard.Extensions;

namespace Toggl.Giskard.Autocomplete
{
    public abstract class TokenSpan : ReplacementSpan
    {
        private static readonly int margin;
        private static readonly int padding;
        private static readonly int tokenHeight;
        private static readonly int cornerRadius;
        private static readonly float halfPadding;

        private readonly Color textColor;
        private readonly Color backgroundColor;
        private readonly bool shouldStrokeOnly;

        static TokenSpan()
        {
            var context = Application.Context;

            tokenHeight = 24.DpToPixels(context);
            padding = margin = cornerRadius = 6.DpToPixels(context);
            halfPadding = padding / 2.0f;
        }

        protected TokenSpan(Color backgroundColor, Color textColor, bool shouldOnlyStroke)
        {
            this.textColor = textColor;
            this.backgroundColor = backgroundColor;
            this.shouldStrokeOnly = shouldOnlyStroke;
        }

        public override void Draw(Canvas canvas, ICharSequence text, int start, int end, float x, int top, int y, int bottom, Paint paint)
        {
            var bounds = new Rect();
            paint.GetTextBounds(text.ToString(), start, end, bounds);

            const int strokeWidth = 3;
            var previousStrokeWidth = paint.StrokeWidth;

            var rect = new RectF(margin + x, top + strokeWidth, margin + x + padding + bounds.Width() + padding, top + tokenHeight);
            paint.StrokeWidth = strokeWidth;
            paint.Color = backgroundColor;
            paint.SetStyle(shouldStrokeOnly ? Paint.Style.Stroke : paint.GetStyle());
            canvas.DrawRoundRect(rect, cornerRadius, cornerRadius, paint);

            paint.Color = textColor;
            paint.StrokeWidth = previousStrokeWidth;
            paint.SetStyle(Paint.Style.FillAndStroke);
            canvas.DrawText(text, start, end, x + padding / 2.0f +  margin - strokeWidth, (bottom / 2.0f) + (bounds.Height() / 2.0f) - halfPadding, paint);
        }

        public override int GetSize(Paint paint, ICharSequence text, int start, int end, Paint.FontMetricsInt fm)
            => (int)(margin + padding + paint.MeasureText(text.SubSequence(start, end)) + margin);
    }
}
